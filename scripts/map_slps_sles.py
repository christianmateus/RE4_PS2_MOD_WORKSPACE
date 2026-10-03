#!/usr/bin/env python3
"""Build a conservative SLPS-debug to SLES symbol map for RE4 PS2 REL imports."""

from __future__ import annotations

import argparse
import bisect
import hashlib
import json
import struct
import sys
from collections import Counter, defaultdict
from dataclasses import asdict, dataclass
from pathlib import Path

from rel_inspect import inspect


BOOTSTRAP_NAMES = {
    "InitModule__FP10MODULE_DAT", "setEmModule__FP10MODULE_DATUi",
    "EmReadSearch__FUcPvUi", "SearchEmModule__FUc", "pullEmModule__Fv",
    "EmSetFromList__Fv", "DLL_Link__FP14OSModuleHeaderPv",
    "EmReadModule", "EmFileTbl", "EmInitFunc",
}


@dataclass(frozen=True)
class ElfSymbol:
    name: str
    address: int
    size: int
    kind: int
    section: str


def u16(data: bytes, offset: int) -> int:
    return struct.unpack_from("<H", data, offset)[0]


def u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def checked(data: bytes, offset: int, size: int, label: str) -> None:
    if offset < 0 or size < 0 or offset > len(data) or size > len(data) - offset:
        raise ValueError(f"{label}: faixa fora do ELF")


def read_elf(path: Path) -> tuple[bytes, dict[str, tuple[int, bytes]], dict[str, ElfSymbol]]:
    data = path.read_bytes()
    checked(data, 0, 52, "cabeçalho")
    if data[:6] != b"\x7fELF\x01\x01" or u16(data, 18) != 8:
        raise ValueError(f"{path}: esperado ELF32 MIPS little-endian")
    section_offset, section_size, section_count, name_index = u32(data, 0x20), u16(data, 0x2E), u16(data, 0x30), u16(data, 0x32)
    if section_size < 40 or name_index >= section_count:
        raise ValueError(f"{path}: tabela de seções inválida")
    checked(data, section_offset, section_size * section_count, "seções")
    sections = [struct.unpack_from("<IIIIIIIIII", data, section_offset + i * section_size) for i in range(section_count)]
    name_section = sections[name_index]
    checked(data, name_section[4], name_section[5], "nomes de seções")
    section_names = data[name_section[4]:name_section[4] + name_section[5]]

    def zstring(blob: bytes, offset: int) -> str:
        if offset >= len(blob):
            return ""
        end = blob.find(b"\0", offset)
        return blob[offset:end].decode("ascii", "replace") if end >= 0 else ""

    names = [zstring(section_names, section[0]) for section in sections]
    loaded: dict[str, tuple[int, bytes]] = {}
    for section, name in zip(sections, names):
        if name in {".text", ".data", ".sndata", ".rodata", ".sdata"}:
            checked(data, section[4], section[5], name)
            loaded[name] = (section[3], data[section[4]:section[4] + section[5]])
    symbols: dict[str, ElfSymbol] = {}
    for section in sections:
        if section[1] != 2:  # SHT_SYMTAB
            continue
        offset, size, link, entry_size = section[4], section[5], section[6], section[9]
        if link >= len(sections) or entry_size < 16 or size % entry_size:
            raise ValueError(f"{path}: tabela de símbolos inválida")
        strings_section = sections[link]
        checked(data, offset, size, "símbolos")
        checked(data, strings_section[4], strings_section[5], "strings")
        strings = data[strings_section[4]:strings_section[4] + strings_section[5]]
        for pos in range(offset, offset + size, entry_size):
            name_offset, address, symbol_size, info, _, section_index = struct.unpack_from("<IIIBBH", data, pos)
            name = zstring(strings, name_offset)
            if name and address and name not in symbols:
                section_name = names[section_index] if section_index < len(names) else ""
                symbols[name] = ElfSymbol(name, address, symbol_size, info & 15, section_name)
    return data, loaded, symbols


def embedded_snr2_records(data: bytes) -> dict[str, dict[str, int]]:
    """Read the main executable's ProDG export records from its .sndata section."""
    section_offset = u32(data, 0x20)
    section_size, section_count, name_index = struct.unpack_from("<HHH", data, 0x2E)
    if section_size < 40 or name_index >= section_count:
        raise ValueError("tabela de seções ELF inválida")
    checked(data, section_offset, section_size * section_count, "seções ELF")
    sections = [struct.unpack_from("<IIIIIIIIII", data, section_offset + i * section_size)
                for i in range(section_count)]
    name_section = sections[name_index]
    checked(data, name_section[4], name_section[5], "nomes de seções")
    section_names = data[name_section[4]:name_section[4] + name_section[5]]
    sndata = None
    for section in sections:
        start = section[0]
        end = section_names.find(b"\0", start)
        if end > start and section_names[start:end] == b".sndata":
            sndata = section
            break
    if sndata is None:
        raise ValueError("seção .sndata ausente no ELF")
    va, file_offset, size = sndata[3], sndata[4], sndata[5]
    checked(data, file_offset, size, ".sndata")
    header_offset = data.find(b"SNR2", file_offset, file_offset + size)
    if header_offset < 0:
        raise ValueError("cabeçalho SNR2 ausente na .sndata")
    checked(data, header_offset, 0x3C, "cabeçalho SNR2")
    table_va, count = struct.unpack_from("<II", data, header_offset + 0x0C)

    def file_position(address: int, length: int, label: str) -> int:
        position = file_offset + address - va
        if address < va or address - va > size or length > size - (address - va):
            raise ValueError(f"{label}: endereço fora da .sndata")
        checked(data, position, length, label)
        return position

    table_offset = file_position(table_va, count * 12, "tabela SNR2")
    result: dict[str, dict[str, int]] = {}
    for index in range(count):
        name_va, address, metadata, kind, flags = struct.unpack_from("<IIHBB", data, table_offset + index * 12)
        name_offset = file_position(name_va, 1, f"nome SNR2 {index}")
        end = data.find(b"\0", name_offset, file_offset + size)
        if end < 0:
            raise ValueError(f"nome SNR2 {index} sem terminador")
        name = data[name_offset:end].decode("ascii", "strict")
        if name and address:
            if name in result and result[name]["address"] != address:
                raise ValueError(f"símbolo SNR2 duplicado com endereços distintos: {name}")
            result[name] = {"address": address, "metadata": metadata, "kind": kind, "flags": flags}
    return result


def embedded_snr2_symbols(data: bytes) -> dict[str, int]:
    """Compatibility helper: name to address from the executable's SNR2 table."""
    return {name: record["address"] for name, record in embedded_snr2_records(data).items()}


def imported_names(directory: Path) -> tuple[set[str], Counter[str], int, int]:
    paths = sorted(p for p in directory.rglob("*") if p.is_file() and p.suffix.lower() == ".rel")
    if not paths:
        raise ValueError(f"{directory}: nenhum REL encontrado")
    names: set[str] = set()
    usage: Counter[str] = Counter()
    seen_hashes: set[bytes] = set()
    for path in paths:
        digest = hashlib.sha256(path.read_bytes()).digest()
        if digest in seen_hashes:
            continue
        seen_hashes.add(digest)
        module = inspect(path)
        for symbol in module["symbols"]:
            if symbol["is_import"] and symbol["name"]:
                names.add(symbol["name"])
                usage[symbol["name"]] += symbol["relocation_uses"]
    return names, usage, len(paths), len(seen_hashes)


def index_windows(code: bytes, desired: set[bytes], width: int) -> dict[bytes, list[int]]:
    found: dict[bytes, list[int]] = defaultdict(list)
    for offset in range(0, len(code) - width + 1, 4):
        key = code[offset:offset + width]
        if key in desired:
            found[key].append(offset)
    return found


def normalized(word: int) -> int:
    op = word >> 26
    if op in (2, 3):  # J/JAL targets
        return word & 0xFC000000
    if op == 0:  # register instruction, keep the whole encoding
        return word
    # Immediate instructions may carry addresses, branches or build-specific constants.
    return word & 0xFFFF0000


def rank_candidates(debug: bytes, retail: bytes, candidates: Counter[int], size: int) -> list[dict]:
    ranked: list[dict] = []
    span = min(size, 256)
    for offset, anchors in candidates.items():
        if offset < 0 or offset + span > len(retail):
            continue
        words = span // 4
        exact = 0
        shape = 0
        for i in range(0, words * 4, 4):
            a = u32(debug, i)
            b = u32(retail, offset + i)
            exact += a == b
            shape += normalized(a) == normalized(b)
        ranked.append({"offset": offset, "anchors": anchors, "exact_words": exact,
                       "shape_words": shape, "compared_words": words})
    ranked.sort(key=lambda r: (r["anchors"], r["exact_words"], r["shape_words"]), reverse=True)
    return ranked


def match_functions(targets: list[ElfSymbol], debug_text: tuple[int, bytes], retail_text: tuple[int, bytes]) -> dict[str, dict]:
    debug_base, debug_code = debug_text
    retail_base, retail_code = retail_text
    requests: dict[str, tuple[bytes, list[tuple[int, bytes]]]] = {}
    desired_12: set[bytes] = set()
    desired_8: set[bytes] = set()
    for symbol in targets:
        start = symbol.address - debug_base
        if symbol.size < 8 or start < 0 or start + symbol.size > len(debug_code):
            continue
        body = debug_code[start:start + symbol.size]
        width = 12 if symbol.size >= 12 else 8
        span = min(symbol.size, 256)
        windows = [(i, body[i:i + width]) for i in range(0, span - width + 1, 4)
                   if any(body[i:i + width])]
        requests[symbol.name] = body, windows
        (desired_12 if width == 12 else desired_8).update(key for _, key in windows)
    index_12 = index_windows(retail_code, desired_12, 12)
    index_8 = index_windows(retail_code, desired_8, 8)
    results: dict[str, dict] = {}
    for symbol in targets:
        request = requests.get(symbol.name)
        if not request:
            continue
        body, windows = request
        index = index_12 if symbol.size >= 12 else index_8
        candidates: Counter[int] = Counter()
        for offset, key in windows:
            for found in index.get(key, ()):
                candidates[found - offset] += 1
        ranked = rank_candidates(body, retail_code, candidates, symbol.size)
        if not ranked:
            continue
        best = ranked[0]
        second = ranked[1] if len(ranked) > 1 else None
        unique = second is None or (best["anchors"] > second["anchors"] and best["exact_words"] >= second["exact_words"])
        exact_ratio = best["exact_words"] / best["compared_words"] if best["compared_words"] else 0
        shape_ratio = best["shape_words"] / best["compared_words"] if best["compared_words"] else 0
        if unique and ((best["anchors"] >= 3 and exact_ratio >= 0.5 and shape_ratio >= 0.75)
                       or (symbol.size <= 32 and exact_ratio == 1 and best["anchors"] >= 1)):
            confidence = "high"
        elif unique and best["anchors"] >= 2 and exact_ratio >= 0.3 and shape_ratio >= 0.6:
            confidence = "medium"
        else:
            confidence = "unresolved"
        results[symbol.name] = {
            "sles_address": retail_base + best["offset"] if confidence != "unresolved" else None,
            "confidence": confidence,
            "evidence": {"anchors": best["anchors"], "exact_words": best["exact_words"],
                         "shape_words": best["shape_words"], "compared_words": best["compared_words"],
                         "runner_up_anchors": second["anchors"] if second else 0},
            "candidate_address": retail_base + best["offset"],
        }
    return results


def infer_neighbor_functions(targets: list[ElfSymbol], mapped: dict[str, dict],
                             debug_text: tuple[int, bytes], retail_text: tuple[int, bytes]) -> dict[str, dict]:
    """Resolve identical small functions using two nearby, already matched functions."""
    debug_base, debug_code = debug_text
    retail_base, retail_code = retail_text
    known = sorted((s.address, mapped[s.name]["sles_address"]) for s in targets
                   if s.name in mapped and mapped[s.name]["confidence"] == "high"
                   and mapped[s.name]["sles_address"] is not None)
    addresses = [item[0] for item in known]
    result: dict[str, dict] = {}
    for symbol in targets:
        if symbol.name in mapped and mapped[symbol.name]["confidence"] != "unresolved":
            continue
        index = bisect.bisect_left(addresses, symbol.address)
        if index == 0 or index >= len(known):
            continue
        before, after = known[index - 1], known[index]
        if before[0] - before[1] != after[0] - after[1]:
            continue
        candidate = symbol.address - (before[0] - before[1])
        span = min(symbol.size, 128)
        words = span // 4
        debug_start, retail_start = symbol.address - debug_base, candidate - retail_base
        if words < 6 or debug_start < 0 or retail_start < 0 or debug_start + span > len(debug_code) or retail_start + span > len(retail_code):
            continue
        exact = sum(u32(debug_code, debug_start + i) == u32(retail_code, retail_start + i)
                    for i in range(0, words * 4, 4))
        ratio = exact / words
        if ratio < 0.8:
            continue
        confidence = "high" if words >= 8 and ratio >= 0.9 else "medium"
        result[symbol.name] = {
            "sles_address": candidate, "confidence": confidence,
            "evidence": {"neighbor_delta": before[0] - before[1],
                         "before_slps": before[0], "after_slps": after[0],
                         "exact_words": exact, "compared_words": words},
            "candidate_address": candidate,
        }
    return result


def infer_shifted_functions(targets: list[ElfSymbol], mapped: dict[str, dict],
                            debug_text: tuple[int, bytes], retail_text: tuple[int, bytes]) -> dict[str, dict]:
    """Find a changed function entry near raw anchors using many matching body windows."""
    debug_base, debug_code = debug_text
    retail_base, retail_code = retail_text
    known = sorted((s.address, mapped[s.name]["sles_address"]) for s in targets
                   if s.name in mapped and mapped[s.name]["confidence"] == "high"
                   and mapped[s.name]["sles_address"] is not None)
    addresses = [item[0] for item in known]
    result: dict[str, dict] = {}
    for symbol in targets:
        current = mapped.get(symbol.name)
        if symbol.size < 80 or not current or current["confidence"] != "unresolved" or current.get("candidate_address") is None:
            continue
        debug_start = symbol.address - debug_base
        if debug_start < 0 or debug_start + symbol.size > len(debug_code):
            continue
        prologue = u32(debug_code, debug_start)
        if prologue >> 16 != 0x27BD:  # ADDIU sp, sp, -frame
            continue
        candidate = current["candidate_address"]
        index = bisect.bisect_left(addresses, symbol.address)
        next_sles = known[index][1] if index < len(known) else retail_base + len(retail_code)
        debug_body = debug_code[debug_start:debug_start + symbol.size]
        debug_windows = {debug_body[i:i + 12] for i in range(0, len(debug_body) - 11, 4)}
        candidates: list[tuple[int, int]] = []
        lower = max(retail_base, candidate - 0x100)
        upper = min(retail_base + len(retail_code) - 4, candidate + 0x100)
        for address in range((lower + 3) & ~3, upper + 1, 4):
            retail_start = address - retail_base
            if u32(retail_code, retail_start) != prologue:
                continue
            end = min(next_sles, address + symbol.size + 0x100)
            if end <= address + 12:
                continue
            body = retail_code[retail_start:end - retail_base]
            common = len(debug_windows & {body[i:i + 12] for i in range(0, len(body) - 11, 4)})
            candidates.append((common, address))
        candidates.sort(reverse=True)
        if not candidates:
            continue
        count, address = candidates[0]
        runner_up = candidates[1][0] if len(candidates) > 1 else 0
        if count < 50 or count < runner_up * 2 + 1 or count < len(debug_windows) * 0.15:
            continue
        result[symbol.name] = {
            "sles_address": address, "confidence": "high",
            "evidence": {"common_body_windows": count, "debug_body_windows": len(debug_windows),
                         "runner_up_windows": runner_up, "matching_prologue": f"0x{prologue:08X}"},
            "candidate_address": address,
        }
    return result


def infer_call_targets(targets: list[ElfSymbol], mapped: dict[str, dict],
                       debug_text: tuple[int, bytes], retail_text: tuple[int, bytes]) -> dict[str, dict]:
    """Transfer JAL targets through matched call sites with aligned neighboring code."""
    debug_base, debug_code = debug_text
    retail_base, retail_code = retail_text
    unresolved_by_address: dict[int, list[str]] = defaultdict(list)
    for symbol in targets:
        if symbol.name not in mapped or mapped[symbol.name]["confidence"] == "unresolved":
            unresolved_by_address[symbol.address].append(symbol.name)
    evidence: dict[str, dict[int, set[str]]] = defaultdict(lambda: defaultdict(set))
    for caller in targets:
        mapping = mapped.get(caller.name)
        if not mapping or mapping["confidence"] != "high" or mapping["sles_address"] is None:
            continue
        debug_start = caller.address - debug_base
        retail_start = mapping["sles_address"] - retail_base
        span = min(caller.size, 1024)
        if debug_start < 0 or retail_start < 0 or debug_start + span > len(debug_code) or retail_start + span > len(retail_code):
            continue
        for offset in range(4, span - 4, 4):
            debug_word = u32(debug_code, debug_start + offset)
            retail_word = u32(retail_code, retail_start + offset)
            if debug_word >> 26 != 3 or retail_word >> 26 != 3:
                continue
            debug_target = ((caller.address + offset + 4) & 0xF0000000) | ((debug_word & 0x03FFFFFF) << 2)
            if debug_target not in unresolved_by_address:
                continue
            previous_debug = u32(debug_code, debug_start + offset - 4)
            previous_retail = u32(retail_code, retail_start + offset - 4)
            next_debug = u32(debug_code, debug_start + offset + 4)
            next_retail = u32(retail_code, retail_start + offset + 4)
            if previous_debug != previous_retail and next_debug != next_retail:
                continue
            retail_target = ((mapping["sles_address"] + offset + 4) & 0xF0000000) | ((retail_word & 0x03FFFFFF) << 2)
            if not retail_base <= retail_target < retail_base + len(retail_code):
                continue
            for name in unresolved_by_address[debug_target]:
                evidence[name][retail_target].add(caller.name)
    result: dict[str, dict] = {}
    for name, choices in evidence.items():
        ranked = sorted(choices.items(), key=lambda item: len(item[1]), reverse=True)
        address, callers = ranked[0]
        runner_up = len(ranked[1][1]) if len(ranked) > 1 else 0
        count = len(callers)
        if count <= runner_up:
            continue
        result[name] = {
            "sles_address": address, "confidence": "high" if count >= 2 else "medium",
            "evidence": {"matching_callers": count, "runner_up_callers": runner_up,
                         "examples": sorted(callers)[:5]},
            "candidate_address": address,
        }
    return result


def infer_data_addresses(targets: list[ElfSymbol], mapped_functions: dict[str, dict],
                         debug_symbols: dict[str, ElfSymbol], debug_text: tuple[int, bytes],
                         retail_text: tuple[int, bytes]) -> dict[str, dict]:
    """Infer data addresses from aligned LUI plus low-immediate references in matched code."""
    debug_base, debug_code = debug_text
    retail_base, retail_code = retail_text
    evidence: dict[str, dict[int, set[str]]] = {target.name: defaultdict(set) for target in targets}
    for name, mapping in mapped_functions.items():
        if mapping["confidence"] != "high" or mapping["sles_address"] is None:
            continue
        function = debug_symbols[name]
        debug_start = function.address - debug_base
        retail_start = mapping["sles_address"] - retail_base
        span = min(function.size, 2048)
        if debug_start < 0 or retail_start < 0 or debug_start + span > len(debug_code) or retail_start + span > len(retail_code):
            continue
        for offset in range(0, span - 4, 4):
            debug_hi = u32(debug_code, debug_start + offset)
            retail_hi = u32(retail_code, retail_start + offset)
            if (debug_hi >> 26) != 15 or (retail_hi >> 26) != 15 or (debug_hi & 0xFFFF0000) != (retail_hi & 0xFFFF0000):
                continue
            base_register = (debug_hi >> 16) & 31
            for step in range(1, 7):
                if offset + step * 4 >= span:
                    break
                debug_lo = u32(debug_code, debug_start + offset + step * 4)
                retail_lo = u32(retail_code, retail_start + offset + step * 4)
                op = debug_lo >> 26
                if (op == 0 or op != retail_lo >> 26 or
                    (debug_lo & 0xFFFF0000) != (retail_lo & 0xFFFF0000) or
                    (debug_lo >> 21) & 31 != base_register):
                    continue
                debug_low = debug_lo & 0xFFFF
                retail_low = retail_lo & 0xFFFF
                if op != 13:  # ORI is unsigned; loads, stores and ADDIU sign-extend.
                    debug_low = debug_low - 0x10000 if debug_low & 0x8000 else debug_low
                    retail_low = retail_low - 0x10000 if retail_low & 0x8000 else retail_low
                debug_address = ((debug_hi & 0xFFFF) << 16) + debug_low
                retail_address = ((retail_hi & 0xFFFF) << 16) + retail_low
                for target in targets:
                    size = max(target.size, 4)
                    if target.address <= debug_address < target.address + size:
                        candidate = retail_address - (debug_address - target.address)
                        evidence[target.name][candidate].add(name)
    result: dict[str, dict] = {}
    for target in targets:
        candidates = sorted(evidence[target.name].items(), key=lambda item: len(item[1]), reverse=True)
        if not candidates:
            continue
        address, functions = candidates[0]
        runner_up = len(candidates[1][1]) if len(candidates) > 1 else 0
        count = len(functions)
        if count >= 3 and count >= runner_up * 2 + 1:
            confidence = "high"
        elif count >= 2 and count > runner_up:
            confidence = "medium"
        else:
            confidence = "unresolved"
        result[target.name] = {
            "sles_address": address if confidence != "unresolved" else None,
            "confidence": confidence,
            "evidence": {"referencing_functions": count, "runner_up_functions": runner_up,
                         "examples": sorted(functions)[:5]},
            "candidate_address": address,
        }
    return result


def match_initialized_data(targets: list[ElfSymbol], debug_sections: dict[str, tuple[int, bytes]],
                           retail_sections: dict[str, tuple[int, bytes]]) -> dict[str, dict]:
    """Match distinctive initialized data directly, within the same ELF section."""
    result: dict[str, dict] = {}
    for target in targets:
        if target.size < 8 or target.section not in debug_sections or target.section not in retail_sections:
            continue
        debug_base, debug_data = debug_sections[target.section]
        retail_base, retail_data = retail_sections[target.section]
        start = target.address - debug_base
        length = min(target.size, 256)
        if start < 0 or start + length > len(debug_data):
            continue
        signature = debug_data[start:start + length]
        if not any(signature):
            continue
        first = retail_data.find(signature)
        if first < 0 or retail_data.find(signature, first + 1) >= 0:
            continue
        result[target.name] = {
            "sles_address": retail_base + first,
            "confidence": "high" if length >= 16 else "medium",
            "evidence": {"identical_data_bytes": length, "section": target.section,
                         "unique_within_section": True},
            "candidate_address": retail_base + first,
        }
    return result


def main() -> int:
    parser = argparse.ArgumentParser(description="Mapeia nomes importados por RELs do SLPS de debug para o SLES.")
    parser.add_argument("--rel-dir", type=Path, default=Path("_references/REL"))
    parser.add_argument("--debug-elf", type=Path, default=Path("_references/ELFs/SLPS_000.00"))
    parser.add_argument("--sles-elf", type=Path, default=Path("_references/ELFs/SLES_537.02"))
    parser.add_argument("--json", type=Path, default=Path("tmp/slps-sles-map.json"))
    parser.add_argument("--text", type=Path, default=Path("tmp/slps-sles-map.txt"))
    args = parser.parse_args()
    try:
        names, usage, rel_count, unique_rel_count = imported_names(args.rel_dir)
        names |= BOOTSTRAP_NAMES
        _, debug_sections, debug_symbols = read_elf(args.debug_elf)
        retail_data, retail_sections, _ = read_elf(args.sles_elf)
        retail_snr2 = embedded_snr2_records(retail_data)
        if ".text" not in debug_sections or ".text" not in retail_sections:
            raise ValueError("seção .text ausente")
        function_targets = [s for name, s in debug_symbols.items() if name in names and s.kind == 2 and s.section == ".text"]
        matched = match_functions(function_targets, debug_sections[".text"], retail_sections[".text"])
        matched.update(infer_neighbor_functions(function_targets, matched, debug_sections[".text"], retail_sections[".text"]))
        matched.update(infer_shifted_functions(function_targets, matched, debug_sections[".text"], retail_sections[".text"]))
        matched.update(infer_call_targets(function_targets, matched, debug_sections[".text"], retail_sections[".text"]))
        data_targets = [s for name, s in debug_symbols.items() if name in names and s.kind != 2 and s.section in {".data", ".sndata", ".bss", ".sdata"}]
        data_matches = match_initialized_data(data_targets, debug_sections, retail_sections)
        xref_targets = [s for s in data_targets if s.name not in data_matches and (s.section == ".bss" or s.size <= 4)]
        data_matches.update(infer_data_addresses(xref_targets, matched, debug_symbols,
                                            debug_sections[".text"], retail_sections[".text"]))
        matched.update(data_matches)
        rows = []
        disagreements = 0
        for name in sorted(names):
            symbol = debug_symbols.get(name)
            result = matched.get(name, {})
            direct_record = retail_snr2.get(name)
            if direct_record is not None:
                direct_address = direct_record["address"]
                evidence = {"method": "embedded_sles_snr2_symbol_table"}
                candidate = result.get("sles_address") or result.get("candidate_address")
                if candidate is not None and candidate != direct_address:
                    evidence["heuristic_candidate"] = candidate
                    evidence["heuristic_confidence"] = result.get("confidence", "unresolved")
                    disagreements += 1
                result = {"sles_address": direct_address, "confidence": "high",
                          "evidence": evidence, "candidate_address": direct_address}
            rows.append({"name": name, "relocation_uses": usage.get(name, 0),
                         "slps_address": symbol.address if symbol else None,
                         "slps_size": symbol.size if symbol else None,
                         "slps_kind": symbol.kind if symbol else None,
                         "slps_section": symbol.section if symbol else None,
                         "sles_metadata": direct_record["metadata"] if direct_record else None,
                         "sles_symbol_kind": direct_record["kind"] if direct_record else None,
                         "sles_address": result.get("sles_address"),
                         "confidence": result.get("confidence", "unresolved"),
                         "evidence": result.get("evidence"),
                         "candidate_address": result.get("candidate_address")})
        counts = Counter(row["confidence"] for row in rows)
        report = {"debug_elf": str(args.debug_elf.resolve()), "sles_elf": str(args.sles_elf.resolve()),
                  "rel_directory": str(args.rel_dir.resolve()), "rel_files": rel_count,
                  "unique_rel_files": unique_rel_count, "sles_snr2_symbols": len(retail_snr2),
                  "direct_matches": sum(row["evidence"] is not None and row["evidence"].get("method") == "embedded_sles_snr2_symbol_table" for row in rows),
                  "heuristic_disagreements": disagreements, "counts": dict(counts), "symbols": rows}
        args.json.parent.mkdir(parents=True, exist_ok=True)
        args.text.parent.mkdir(parents=True, exist_ok=True)
        args.json.write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
        lines = ["Mapa de importações REL → SLES", f"RELs: {rel_count} arquivos, {unique_rel_count} conteúdos únicos",
                 f"Tabela SNR2 do SLES: {len(retail_snr2)} símbolos | correspondências diretas: {report['direct_matches']} | divergências heurísticas: {disagreements}",
                 f"Alvos: {len(rows)} | alta: {counts['high']} | média: {counts['medium']} | não resolvidos: {counts['unresolved']}",
                 "A tabela SNR2 interna do SLES tem precedência; diferenças heurísticas ficam registradas no JSON.", ""]
        for row in sorted(rows, key=lambda r: (-r["relocation_uses"], r["name"])):
            slps = f"0x{row['slps_address']:08X}" if row["slps_address"] is not None else "-"
            sles = f"0x{row['sles_address']:08X}" if row["sles_address"] is not None else "-"
            lines.append(f"{row['confidence']:<10} refs={row['relocation_uses']:>5} SLPS={slps} SLES={sles} {row['name']}")
        args.text.write_text("\n".join(lines) + "\n", encoding="utf-8")
        print("\n".join(lines[:6]))
        print(f"JSON: {args.json.resolve()}\nTexto: {args.text.resolve()}")
        return 0
    except (OSError, ValueError, struct.error) as exc:
        print(f"Erro: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
