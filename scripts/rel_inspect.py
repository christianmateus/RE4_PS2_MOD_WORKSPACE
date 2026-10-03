#!/usr/bin/env python3
"""Read-only inspector for Resident Evil 4 PS2 ProDG SNR2 REL modules."""

from __future__ import annotations

import argparse
import json
import struct
import sys
from collections import Counter
from dataclasses import asdict, dataclass
from pathlib import Path


HEADER_SIZE = 0x3C
RELOCATION_SIZE = 12
SYMBOL_SIZE = 12
RELOCATION_NAMES = {0: "NONE", 1: "R_MIPS_16", 2: "R_MIPS_32", 3: "R_MIPS_REL32", 4: "R_MIPS_26", 5: "R_MIPS_HI16", 6: "R_MIPS_LO16"}


@dataclass(frozen=True)
class Symbol:
    index: int
    name: str
    name_offset: int
    address: int
    metadata: int
    kind: int
    flags: int
    debug_address: int | None = None
    sles_address: int | None = None
    sles_confidence: str | None = None

    @property
    def is_import(self) -> bool:
        return self.address == 0 and self.index != 0


@dataclass(frozen=True)
class Relocation:
    index: int
    offset: int
    type: int
    symbol_index: int
    symbol_name: str
    addend: int
    extra_hex: str
    sles_address: int | None = None
    sles_confidence: str | None = None


def u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def checked_range(data: bytes, offset: int, size: int, label: str) -> None:
    if offset < 0 or size < 0 or offset > len(data) or size > len(data) - offset:
        raise ValueError(f"{label}: faixa 0x{offset:X}+0x{size:X} fora do arquivo (0x{len(data):X})")


def read_string(data: bytes, offset: int, label: str) -> str:
    checked_range(data, offset, 1, label)
    end = data.find(b"\0", offset)
    if end < 0:
        raise ValueError(f"{label}: string sem terminador em 0x{offset:X}")
    raw = data[offset:end]
    if any(c < 0x20 or c > 0x7E for c in raw):
        raise ValueError(f"{label}: caracteres inválidos em 0x{offset:X}")
    return raw.decode("ascii")


def load_debug_symbols(path: Path) -> dict[str, int]:
    """Read named ELF32 little-endian MIPS symbols; do not infer SLES addresses."""
    data = path.read_bytes()
    checked_range(data, 0, 52, "ELF")
    if data[:6] != b"\x7fELF\x01\x01" or struct.unpack_from("<H", data, 18)[0] != 8:
        raise ValueError(f"{path}: esperado ELF32 MIPS little-endian")
    shoff = u32(data, 0x20)
    shentsize, shnum = struct.unpack_from("<HH", data, 0x2E)
    if shentsize < 40:
        raise ValueError(f"{path}: seção ELF inválida")
    checked_range(data, shoff, shentsize * shnum, "tabela de seções ELF")
    sections = [struct.unpack_from("<IIIIIIIIII", data, shoff + i * shentsize) for i in range(shnum)]
    result: dict[str, int] = {}
    for section in sections:
        if section[1] != 2:  # SHT_SYMTAB
            continue
        offset, size, link, entry_size = section[4], section[5], section[6], section[9]
        if link >= shnum or entry_size < 16 or size % entry_size:
            raise ValueError(f"{path}: tabela de símbolos ELF inválida")
        string_section = sections[link]
        checked_range(data, offset, size, "símbolos ELF")
        checked_range(data, string_section[4], string_section[5], "strings ELF")
        strings = data[string_section[4]:string_section[4] + string_section[5]]
        for pos in range(offset, offset + size, entry_size):
            name_offset, address = struct.unpack_from("<II", data, pos)
            if name_offset >= len(strings):
                continue
            end = strings.find(b"\0", name_offset)
            if end <= name_offset:
                continue
            name = strings[name_offset:end].decode("ascii", "replace")
            if address:
                result.setdefault(name, address)
    return result


def load_sles_map(path: Path) -> dict[str, tuple[int, str]]:
    report = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(report, dict) or not isinstance(report.get("symbols"), list):
        raise ValueError(f"{path}: mapa SLPS → SLES inválido")
    result: dict[str, tuple[int, str]] = {}
    for row in report["symbols"]:
        if not isinstance(row, dict):
            continue
        name, address, confidence = row.get("name"), row.get("sles_address"), row.get("confidence")
        if isinstance(name, str) and isinstance(address, int) and confidence in {"high", "medium"}:
            result[name] = (address, confidence)
    return result


def inspect(path: Path, debug_symbols: dict[str, int] | None = None,
            sles_map: dict[str, tuple[int, str]] | None = None) -> dict:
    data = path.read_bytes()
    checked_range(data, 0, HEADER_SIZE, "cabeçalho SNR2")
    if data[:4] != b"SNR2":
        raise ValueError(f"{path}: assinatura SNR2 ausente")
    fields = struct.unpack_from("<14I", data, 4)
    (reloc_offset, reloc_count, symbol_offset, symbol_count, image_name_offset,
     ctor_offset, dtor_offset, export_offset, export_count, unknown_28,
     declared_size, unknown_30, unknown_34, unknown_38) = fields
    checked_range(data, reloc_offset, reloc_count * RELOCATION_SIZE, "realocações")
    checked_range(data, symbol_offset, symbol_count * SYMBOL_SIZE, "símbolos")
    checked_range(data, export_offset, export_count * 4, "exports")
    if declared_size != len(data):
        raise ValueError(f"{path}: tamanho declarado 0x{declared_size:X} difere do arquivo 0x{len(data):X}")
    image_name = read_string(data, image_name_offset, "nome da imagem")
    symbols: list[Symbol] = []
    for index in range(symbol_count):
        pos = symbol_offset + index * SYMBOL_SIZE
        name_offset, address, metadata, kind, flags = struct.unpack_from("<IIHBB", data, pos)
        name = read_string(data, name_offset, f"símbolo {index}")
        if address and address >= len(data):
            raise ValueError(f"{path}: símbolo {index} ({name}) aponta para fora do REL")
        sles = sles_map.get(name) if sles_map else None
        symbols.append(Symbol(index, name, name_offset, address, metadata, kind, flags,
                              debug_symbols.get(name) if debug_symbols else None,
                              sles[0] if sles else None, sles[1] if sles else None))
    relocations: list[Relocation] = []
    for index in range(reloc_count):
        pos = reloc_offset + index * RELOCATION_SIZE
        offset, info, addend = struct.unpack_from("<IIi", data, pos)
        relocation_type = info & 0xFF
        symbol_index = info >> 8
        if offset >= len(data):
            raise ValueError(f"{path}: realocação {index} aponta para fora do REL")
        if symbol_index >= len(symbols):
            raise ValueError(f"{path}: realocação {index} usa símbolo inexistente {symbol_index}")
        target = symbols[symbol_index]
        relocations.append(Relocation(index, offset, relocation_type, symbol_index,
                                      target.name, addend, data[pos + 7:pos + RELOCATION_SIZE].hex(),
                                      target.sles_address, target.sles_confidence))
    exports = [u32(data, export_offset + index * 4) for index in range(export_count)]
    import_usage = Counter(rel.symbol_index for rel in relocations if symbols[rel.symbol_index].is_import)
    relocation_types = Counter(RELOCATION_NAMES.get(rel.type, f"UNKNOWN_{rel.type}") for rel in relocations)
    return {
        "file": str(path.resolve()),
        "size": len(data),
        "image_name": image_name,
        "header": {
            "relocation_offset": reloc_offset, "relocation_count": reloc_count,
            "symbol_offset": symbol_offset, "symbol_count": symbol_count,
            "image_name_offset": image_name_offset,
            "global_ctors_offset": ctor_offset, "global_dtors_offset": dtor_offset,
            "export_offset": export_offset, "export_count": export_count,
            "unknown_28": unknown_28, "declared_size": declared_size,
            "unknown_30": unknown_30, "unknown_34": unknown_34, "unknown_38": unknown_38,
        },
        "counts": {
            "symbols": len(symbols),
            "defined_symbols": sum(s.address != 0 for s in symbols),
            "imports": sum(s.is_import for s in symbols),
            "relocations": len(relocations),
            "relocation_types": dict(sorted(relocation_types.items())),
            "debug_elf_name_matches": sum(s.is_import and s.debug_address is not None for s in symbols),
            "sles_address_matches": sum(s.is_import and s.sles_address is not None for s in symbols),
        },
        "exports": exports,
        "symbols": [asdict(s) | {"is_import": s.is_import,
                                  "relocation_uses": import_usage.get(s.index, 0)} for s in symbols],
        "relocations": [asdict(r) | {"type_name": RELOCATION_NAMES.get(r.type, f"UNKNOWN_{r.type}")} for r in relocations],
    }


def render_report(modules: list[dict], show_symbols: bool, show_relocations: bool,
                  pattern: str | None, limit: int, root: Path | None = None) -> None:
    for module in modules:
        counts = module["counts"]
        path = Path(module["file"])
        label = path.relative_to(root.resolve()) if root else path.name
        print(f"\n{label}: {module['size']} bytes | {module['image_name']}")
        print(f"  Símbolos: {counts['symbols']} ({counts['defined_symbols']} definidos, "
              f"{counts['imports']} externos) | Realocações: {counts['relocations']}")
        print("  Tipos: " + ", ".join(f"{key}={value}" for key, value in counts["relocation_types"].items()))
        if counts["debug_elf_name_matches"]:
            print(f"  Nomes externos encontrados no ELF debug: {counts['debug_elf_name_matches']}")
        if counts["sles_address_matches"]:
            print(f"  Dependências com endereço SLES: {counts['sles_address_matches']}")
        if not show_symbols and not show_relocations:
            continue
        if show_symbols:
            print("  Símbolos:")
            rows = [s for s in module["symbols"] if pattern is None or pattern.lower() in s["name"].lower()]
            for symbol in rows[:limit]:
                location = "externo" if symbol["is_import"] else f"0x{symbol['address']:X}"
                debug = f" | SLPS 0x{symbol['debug_address']:X}" if symbol["debug_address"] is not None else ""
                sles = f" | SLES 0x{symbol['sles_address']:X} ({symbol['sles_confidence']})" if symbol["sles_address"] is not None else ""
                print(f"    [{symbol['index']:>3}] {location:>10} tipo={symbol['kind']} "
                      f"refs={symbol['relocation_uses']:>3} {symbol['name']}{debug}{sles}")
            if len(rows) > limit:
                print(f"    ... {len(rows) - limit} restantes; use --limit ou --json")
        if show_relocations:
            print("  Realocações:")
            rows = [r for r in module["relocations"] if pattern is None or pattern.lower() in r["symbol_name"].lower()]
            for relocation in rows[:limit]:
                sles = f" -> SLES 0x{relocation['sles_address']:X} ({relocation['sles_confidence']})" if relocation["sles_address"] is not None else ""
                print(f"    0x{relocation['offset']:06X} {relocation['type_name']:<12} "
                      f"[{relocation['symbol_index']}] {relocation['symbol_name']} "
                      f"addend=0x{relocation['addend'] & 0xFFFFFFFF:08X}{sles}")
            if len(rows) > limit:
                print(f"    ... {len(rows) - limit} restantes; use --limit ou --json")


def main() -> int:
    parser = argparse.ArgumentParser(description="Inspeciona RELs SNR2 do RE4 PS2 sem modificar arquivos.")
    parser.add_argument("path", type=Path, help="arquivo .rel ou pasta que contém arquivos .rel")
    parser.add_argument("--debug-elf", type=Path, help="ELF SLPS de debug para cruzar nomes; endereços NÃO são do SLES")
    parser.add_argument("--sles-map", type=Path, help="mapa JSON gerado por map_slps_sles.py")
    parser.add_argument("--json", type=Path, help="salvar relatório completo em JSON")
    parser.add_argument("--symbols", action="store_true", help="listar símbolos")
    parser.add_argument("--relocations", action="store_true", help="listar realocações")
    parser.add_argument("--filter", help="filtrar símbolos e realocações pelo nome")
    parser.add_argument("--limit", type=int, default=30, help="máximo de linhas de cada lista no terminal (padrão: 30)")
    args = parser.parse_args()
    if args.limit < 1:
        parser.error("--limit deve ser positivo")
    try:
        root = args.path if args.path.is_dir() else None
        paths = sorted((p for p in root.rglob("*") if p.is_file() and p.suffix.lower() == ".rel"), key=lambda p: str(p.relative_to(root)).lower()) if root else [args.path]
        if not paths:
            raise ValueError(f"{args.path}: nenhum .rel encontrado")
        debug_symbols = load_debug_symbols(args.debug_elf) if args.debug_elf else None
        sles_map = load_sles_map(args.sles_map) if args.sles_map else None
        modules = [inspect(path, debug_symbols, sles_map) for path in paths]
        render_report(modules, args.symbols, args.relocations, args.filter, args.limit, root)
        if args.json:
            args.json.write_text(json.dumps(modules if len(modules) != 1 else modules[0], ensure_ascii=False, indent=2), encoding="utf-8")
            print(f"\nJSON salvo em {args.json.resolve()}")
        return 0
    except (OSError, ValueError, struct.error, json.JSONDecodeError) as exc:
        print(f"Erro: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
