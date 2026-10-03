#!/usr/bin/env python3
"""Audit SNR2 REL imports and dry-run MIPS relocations without writing game files."""

from __future__ import annotations

import argparse
import hashlib
import json
import struct
import sys
from collections import Counter, defaultdict
from pathlib import Path

from rel_inspect import inspect


SUPPORTED_TYPES = {2, 4, 5, 6}


def main_export_address(mapped: dict, metadata: int) -> int | None:
    """Model lookup_symbol after snInitDllSystem activates SLES exports."""
    if ((mapped.get("evidence") or {}).get("method") == "embedded_sles_snr2_symbol_table"
            and mapped.get("sles_metadata") == metadata
            and mapped.get("sles_symbol_kind") in {2, 3, 4}
            and isinstance(mapped.get("sles_address"), int)):
        return mapped["sles_address"]
    return None


def loaded_exports(path: Path, base: int) -> dict[tuple[str, int], int]:
    module = inspect(path)
    alignment = module["header"]["unknown_28"]
    if alignment < 1 or alignment & (alignment - 1) or base & (alignment - 1):
        raise ValueError(f"{path}: base 0x{base:X} não satisfaz alinhamento 0x{alignment:X}")
    return {(symbol["name"], symbol["metadata"]): base + symbol["address"]
            for symbol in module["symbols"] if symbol["name"] and symbol["address"]
            and symbol["kind"] in {2, 3, 4}}


def load_map(path: Path) -> dict[str, dict]:
    report = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(report, dict) or not isinstance(report.get("symbols"), list):
        raise ValueError(f"{path}: mapa SLES inválido")
    return {row["name"]: row for row in report["symbols"] if isinstance(row, dict) and isinstance(row.get("name"), str)}


def inventory(root: Path, mapping: dict[str, dict]) -> tuple[list[dict], dict[str, list[str]], dict[str, str]]:
    paths = sorted((p for p in root.rglob("*") if p.is_file() and p.suffix.lower() == ".rel"),
                   key=lambda p: str(p.relative_to(root)).lower())
    if not paths:
        raise ValueError(f"{root}: nenhum REL encontrado")
    digests: dict[str, str] = {}
    aliases: dict[str, list[str]] = defaultdict(list)
    modules: list[dict] = []
    exports: dict[str, list[dict]] = defaultdict(list)
    for path in paths:
        relative = str(path.relative_to(root)).replace("\\", "/")
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if digest in digests:
            aliases[digests[digest]].append(relative)
            continue
        digests[digest] = relative
        module = inspect(path)
        imports = [symbol for symbol in module["symbols"]
                   if symbol["is_import"] and symbol["name"] and symbol["relocation_uses"]]
        for symbol in module["symbols"]:
            if symbol["address"] and symbol["name"]:
                exports[symbol["name"]].append({"file": relative, "metadata": symbol["metadata"]})
        modules.append({
            "file": relative,
            "size": module["size"],
            "relocations": module["counts"]["relocations"],
            "relocation_types": module["counts"]["relocation_types"],
            "imports": imports,
        })

    rows: list[dict] = []
    for module in modules:
        unresolved = []
        direct = 0
        for symbol in module["imports"]:
            name = symbol["name"]
            mapped = mapping.get(name, {})
            if main_export_address(mapped, symbol["metadata"]) is not None:
                direct += 1
                continue
            metadata_matches = (mapped.get("sles_metadata") is None or
                                mapped["sles_metadata"] == symbol["metadata"])
            providers = sorted({entry["file"] for entry in exports.get(name, [])
                                if entry["file"] != module["file"] and
                                entry["metadata"] == symbol["metadata"]})
            unresolved.append({
                "name": name,
                "metadata": symbol["metadata"],
                "relocation_uses": symbol["relocation_uses"],
                "reason": "sles_metadata_mismatch" if not metadata_matches else
                          "other_rel_export" if len(providers) == 1 else
                          "ambiguous_rel_export" if providers else
                          "not_exported_by_sles" if isinstance(mapped.get("sles_address"), int) else
                          "no_address_found",
                "providers": providers,
            })
        unsupported = [name for name in module["relocation_types"]
                       if name not in {"R_MIPS_32", "R_MIPS_26", "R_MIPS_HI16", "R_MIPS_LO16"}]
        rows.append({
            "file": module["file"], "identical_copies": aliases[module["file"]],
            "size": module["size"], "relocations": module["relocations"],
            "relocation_types": module["relocation_types"],
            "used_imports": len(module["imports"]),
            "direct_sles_imports": direct,
            "unresolved_imports": unresolved, "unsupported_types": unsupported,
            "mapping_ready": not unresolved and not unsupported,
        })
    return rows, {name: sorted({entry["file"] for entry in entries})
                  for name, entries in exports.items()}, digests


def dry_run(path: Path, mapping: dict[str, dict], base: int,
            providers: list[tuple[str, dict[tuple[str, int], int]]] | None = None) -> dict:
    module = inspect(path)
    source = path.read_bytes()
    image = bytearray(source)
    symbols = module["symbols"]
    header = module["header"]
    alignment = header["unknown_28"]
    if alignment < 1 or alignment & (alignment - 1) or base & (alignment - 1):
        raise ValueError(f"base 0x{base:X} não satisfaz alinhamento SNR2 0x{alignment:X}")
    if base + len(source) > 0x100000000:
        raise ValueError("base + tamanho do REL excede 32 bits")
    providers = providers or []

    def resolve(symbol: dict) -> tuple[int | None, str]:
        if not symbol["is_import"]:
            return base + symbol["address"], "REL"
        mapped = mapping.get(symbol["name"], {})
        address = main_export_address(mapped, symbol["metadata"])
        if address is not None:
            return address, "SLES"
        key = (symbol["name"], symbol["metadata"])
        for provider_name, exports in providers:
            if key in exports:
                return exports[key], provider_name
        return None, "unresolved"
    header_fields = {
        "relocation_offset": 0x04, "symbol_offset": 0x0C, "image_name_offset": 0x14,
        "global_ctors_offset": 0x18, "global_dtors_offset": 0x1C,
        "export_offset": 0x20, "unknown_34": 0x34, "unknown_38": 0x38,
    }
    rebased_header: dict[str, int] = {}
    for name, offset in header_fields.items():
        value = header[name]
        if value:
            value += base
            struct.pack_into("<I", image, offset, value)
        rebased_header[name] = value
    struct.pack_into("<I", image, 0x30, 0)  # linked-list pointer is cleared by relocate_dll
    for symbol in symbols:
        record = header["symbol_offset"] + symbol["index"] * 12
        struct.pack_into("<I", image, record, base + symbol["name_offset"])
        address, _ = resolve(symbol)
        if address is not None:
            struct.pack_into("<I", image, record + 4, address)
    for relocation in module["relocations"]:
        record = header["relocation_offset"] + relocation["index"] * 12
        struct.pack_into("<I", image, record, base + relocation["offset"])
    export_values = []
    for index, value in enumerate(module["exports"]):
        patched = base + value if value else 0
        export_values.append(patched)
        struct.pack_into("<I", image, header["export_offset"] + index * 4, patched)
    if len(export_values) >= 3:
        struct.pack_into("<I", image, 0x34, export_values[1])
        struct.pack_into("<I", image, 0x38, export_values[2])
        rebased_header["unknown_34"] = export_values[1]
        rebased_header["unknown_38"] = export_values[2]
    counts: Counter[str] = Counter()
    blockers: list[dict] = []
    errors: list[dict] = []
    samples: list[dict] = []
    seen_offsets: set[int] = set()
    for relocation in module["relocations"]:
        offset = relocation["offset"]
        kind = relocation["type"]
        symbol = symbols[relocation["symbol_index"]]
        name = symbol["name"] or f"<local:{symbol['index']}>"
        address, origin = resolve(symbol)
        if address is None:
            blockers.append({"offset": offset, "symbol": name, "metadata": symbol["metadata"],
                             "type": relocation["type_name"]})
            continue
        try:
            if kind not in SUPPORTED_TYPES:
                raise ValueError(f"tipo de realocação {kind} não implementado")
            if offset % 4 or offset + 4 > len(source):
                raise ValueError("offset não alinhado ou fora do arquivo")
            if offset in seen_offsets:
                raise ValueError("duas realocações no mesmo offset")
            seen_offsets.add(offset)
            old = struct.unpack_from("<I", source, offset)[0]
            target = address + relocation["addend"]
            if target < 0 or target > 0xFFFFFFFF:
                raise ValueError("endereço fora de 32 bits")
            if kind == 2:  # R_MIPS_32: S + A
                new = target
            elif kind == 4:  # R_MIPS_26: J/JAL target
                if old >> 26 not in (2, 3):
                    raise ValueError("R_MIPS_26 fora de J/JAL")
                if target & 3:
                    raise ValueError("destino J/JAL não alinhado")
                if ((base + offset + 4) & 0xF0000000) != (target & 0xF0000000):
                    raise ValueError("destino J/JAL fora da região de 256 MB")
                new = (old & 0xFC000000) | ((target >> 2) & 0x03FFFFFF)
            elif kind == 5:  # R_MIPS_HI16: rounded upper half of S + A
                if old >> 26 != 15:
                    raise ValueError("R_MIPS_HI16 fora de LUI")
                expected = ((relocation["addend"] + 0x8000) >> 16) & 0xFFFF
                if old & 0xFFFF != expected:
                    raise ValueError("HI16 original não corresponde ao addend explícito")
                new = (old & 0xFFFF0000) | (((target + 0x8000) >> 16) & 0xFFFF)
            else:  # R_MIPS_LO16
                if old & 0xFFFF != relocation["addend"] & 0xFFFF:
                    raise ValueError("LO16 original não corresponde ao addend explícito")
                new = (old & 0xFFFF0000) | (target & 0xFFFF)
            struct.pack_into("<I", image, offset, new)
            if struct.unpack_from("<I", image, offset)[0] != new:
                raise ValueError("falha ao conferir palavra realocada")
            counts[relocation["type_name"]] += 1
            if len(samples) < 12:
                samples.append({"offset": offset, "symbol": name, "origin": origin,
                                "type": relocation["type_name"], "addend": relocation["addend"],
                                "target": target, "before": old, "after": new})
        except ValueError as exc:
            errors.append({"offset": offset, "symbol": name, "type": relocation["type_name"],
                           "error": str(exc)})
    return {
        "file": str(path.resolve()), "synthetic_base": base,
        "required_alignment": alignment, "rebased_header": rebased_header,
        "rebased_exports": export_values,
        "symbol_records_processed": len(symbols),
        "total_relocations": len(module["relocations"]),
        "patched_in_memory": sum(counts.values()), "patched_types": dict(sorted(counts.items())),
        "blockers": blockers, "validation_errors": errors, "samples": samples,
        "status": "arithmetic_ok" if not blockers and not errors else "incomplete",
        "note": "Teste estático em cópia na memória; não executa o REL nem confirma o carregador do jogo.",
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Audita importações e simula realocações SNR2 em memória.")
    parser.add_argument("--rel-dir", type=Path, default=Path("_references/REL"))
    parser.add_argument("--sles-map", type=Path, default=Path("tmp/slps-sles-map.json"))
    parser.add_argument("--module", type=Path, default=Path("_references/REL/em21.rel"))
    parser.add_argument("--base", type=lambda value: int(value, 0), default=0x00500000,
                        help="base sintética do módulo para o teste (padrão: 0x00500000)")
    parser.add_argument("--loaded", action="append", default=[], metavar="REL@BASE",
                        help="REL já carregado e sua base; pode ser repetido na ordem de carregamento")
    parser.add_argument("--verify-all-ready", action="store_true",
                        help="simular todos os RELs sem importações pendentes na base informada")
    parser.add_argument("--json", type=Path, default=Path("tmp/rel-link-audit.json"))
    parser.add_argument("--text", type=Path, default=Path("tmp/rel-link-audit.txt"))
    args = parser.parse_args()
    try:
        if args.base < 0 or args.base > 0xFFFFFFFF or args.base % 4:
            raise ValueError("--base deve ser endereço de 32 bits alinhado a 4")
        mapping = load_map(args.sles_map)
        rows, _, hashes = inventory(args.rel_dir, mapping)
        ready_verification = None
        if args.verify_all_ready:
            checks = []
            for row in rows:
                if not row["mapping_ready"]:
                    continue
                result = dry_run(args.rel_dir / row["file"], mapping, args.base)
                checks.append({"file": row["file"], "status": result["status"],
                               "patched": result["patched_in_memory"],
                               "total": result["total_relocations"],
                               "blockers": len(result["blockers"]),
                               "validation_errors": len(result["validation_errors"])})
            ready_verification = {"base": args.base, "tested": len(checks),
                                  "passed": sum(check["status"] == "arithmetic_ok" for check in checks),
                                  "relocations": sum(check["total"] for check in checks),
                                  "modules": checks}
        providers: list[tuple[str, dict[tuple[str, int], int]]] = []
        loaded_reports = []
        memory_ranges: list[tuple[int, int, str]] = []
        for specification in args.loaded:
            if "@" not in specification:
                raise ValueError(f"--loaded exige REL@BASE: {specification}")
            path_text, base_text = specification.rsplit("@", 1)
            provider_path = Path(path_text)
            provider_base = int(base_text, 0)
            provider_end = provider_base + provider_path.stat().st_size
            if provider_base < 0 or provider_end > 0x100000000:
                raise ValueError(f"{provider_path}: faixa de memória fora de 32 bits")
            if any(provider_base < end and start < provider_end for start, end, _ in memory_ranges):
                raise ValueError(f"{provider_path}: faixa de memória sobrepõe outro REL carregado")
            provider_report = dry_run(provider_path, mapping, provider_base, providers)
            loaded_reports.append({"file": str(provider_path.resolve()), "base": provider_base,
                                   "status": provider_report["status"],
                                   "patched_in_memory": provider_report["patched_in_memory"],
                                   "total_relocations": provider_report["total_relocations"],
                                   "blockers": provider_report["blockers"],
                                   "validation_errors": provider_report["validation_errors"]})
            if provider_report["status"] == "arithmetic_ok":
                providers.append((str(provider_path.resolve()), loaded_exports(provider_path, provider_base)))
                memory_ranges.append((provider_base, provider_end, str(provider_path)))
        pilot_end = args.base + args.module.stat().st_size
        if any(args.base < end and start < pilot_end for start, end, _ in memory_ranges):
            raise ValueError("faixa de memória do piloto sobrepõe um REL carregado")
        pilot = dry_run(args.module, mapping, args.base, providers)
        counts = Counter("ready" if row["mapping_ready"] else "pending" for row in rows)
        groups: dict[str, dict[str, int]] = defaultdict(lambda: {"total": 0, "ready": 0})
        for row in rows:
            group = row["file"].split("/", 1)[0] if "/" in row["file"] else "inimigos"
            groups[group]["total"] += 1
            groups[group]["ready"] += int(row["mapping_ready"])
        report = {"rel_directory": str(args.rel_dir.resolve()), "map": str(args.sles_map.resolve()),
                  "files": sum(1 for p in args.rel_dir.rglob("*") if p.is_file() and p.suffix.lower() == ".rel"),
                  "unique_files": len(hashes), "counts": dict(counts), "groups": dict(groups),
                  "modules": rows, "ready_verification": ready_verification,
                  "loaded_modules": loaded_reports, "pilot": pilot}
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.text.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
        lines = [f"RELs: {report['files']} arquivos, {report['unique_files']} conteúdos únicos",
                 f"Sem importações pendentes: {counts['ready']} | com pendências: {counts['pending']}", ""]
        lines += [f"{group}: {value['ready']}/{value['total']} sem pendências"
                  for group, value in sorted(groups.items())]
        if ready_verification is not None:
            lines.append(f"Verificação em lote: {ready_verification['passed']}/{ready_verification['tested']} "
                         f"RELs, {ready_verification['relocations']} realocações")
        lines.append("")
        for row in sorted(rows, key=lambda row: (len(row["unresolved_imports"]), row["file"])):
            lines.append(f"{row['file']}: {row['used_imports']} imports usados, "
                         f"{len(row['unresolved_imports'])} pendentes, {row['relocations']} realocações")
            for item in row["unresolved_imports"]:
                provider = f" -> {', '.join(item['providers'])}" if item["providers"] else ""
                lines.append(f"  {item['name']} ({item['relocation_uses']} refs): {item['reason']}{provider}")
        lines.append("")
        for loaded in loaded_reports:
            lines.append(f"Carregado: {loaded['file']} @ 0x{loaded['base']:08X} "
                         f"-> {loaded['status']} ({loaded['patched_in_memory']}/{loaded['total_relocations']})")
        lines.extend([f"Piloto: {args.module}",
                      f"Base sintética: 0x{args.base:08X}",
                      f"Resultado: {pilot['status']} | {pilot['patched_in_memory']}/{pilot['total_relocations']} realocações aplicadas",
                      f"Bloqueios: {len(pilot['blockers'])} | erros de validação: {len(pilot['validation_errors'])}"])
        args.text.write_text("\n".join(lines) + "\n", encoding="utf-8")
        headline = lines[:2]
        if ready_verification is not None:
            headline.append(f"Verificação em lote: {ready_verification['passed']}/{ready_verification['tested']} "
                            f"RELs, {ready_verification['relocations']} realocações")
        print("\n".join(headline + lines[-5:]))
        print(f"JSON: {args.json.resolve()}\nTexto: {args.text.resolve()}")
        all_ready_passed = (ready_verification is None or
                            ready_verification["passed"] == ready_verification["tested"])
        return 0 if pilot["status"] == "arithmetic_ok" and all_ready_passed and all(
            loaded["status"] == "arithmetic_ok" for loaded in loaded_reports) else 2
    except (OSError, ValueError, KeyError, IndexError, struct.error, UnicodeError) as exc:
        print(f"Erro: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
