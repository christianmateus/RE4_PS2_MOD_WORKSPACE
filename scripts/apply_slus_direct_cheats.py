"""Apply verified direct patches to a new ELF copy; reject auxiliary hooks.

Example: python scripts/apply_slus_direct_cheats.py --input path/to/ELF
         --output path/to/NEW_ELF --ids 1,7,9
"""
import argparse
import json
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--input',required=True,type=Path)
    parser.add_argument('--output',required=True,type=Path)
    parser.add_argument('--ids',required=True,help='Comma-separated catalogue IDs')
    args=parser.parse_args()
    if args.input.resolve()==args.output.resolve():parser.error('Output must be a separate copy')
    if args.output.exists():parser.error('Output already exists; choose a new filename')
    ids=[int(x) for x in args.ids.split(',')]
    if len(set(ids))!=len(ids):parser.error('Duplicate IDs')
    if len(set(ids)&{19,20,21})>1:parser.error('Select only one TMP variant')
    catalogue=json.loads((ROOT/'artifacts/slus_menu_analysis/cheats_catalogue.json').read_text(encoding='utf-8'))
    options={c['id']:c for c in catalogue['cheats']}
    raw=bytearray(args.input.read_bytes())
    if raw[:7]!=b'\x7fELF\x01\x01\x01':parser.error('Expected ELF32 little-endian')
    if struct.unpack_from('<H',raw,18)[0]!=8:parser.error('Expected MIPS ELF')
    phoff=struct.unpack_from('<I',raw,28)[0]
    phsize,phnum=struct.unpack_from('<HH',raw,42)
    if phsize<32:parser.error('Invalid program headers')
    segments=[struct.unpack_from('<8I',raw,phoff+i*phsize) for i in range(phnum)]
    planned={}
    for index in ids:
        if index not in options:parser.error(f'Unknown ID {index}')
        option=options[index]
        if option['kind']!='patch_direto':parser.error(f'ID {index} needs runtime/auxiliary code; use the extracted resources with a launcher')
        for patch in option['game_writes']:
            addr=int(patch['ram_address'],16)
            matches=[s for s in segments if s[0]==1 and s[2]<=addr and addr+4<=s[2]+s[4]]
            if len(matches)!=1:parser.error(f'RAM {addr:08X} has no unique file-backed segment')
            s=matches[0];offset=s[1]+addr-s[2]
            actual=struct.unpack_from('<I',raw,offset)[0]
            expected=int(patch['original'],16);value=int(patch['value'],16)
            if actual!=expected:parser.error(f'ID {index}: RAM {addr:08X} expected {expected:08X}, found {actual:08X}; incompatible version or existing mod')
            if offset in planned and planned[offset]!=value:parser.error(f'Conflicting writes at {addr:08X}')
            planned[offset]=value
    for offset,value in planned.items():struct.pack_into('<I',raw,offset,value)
    args.output.parent.mkdir(parents=True,exist_ok=True)
    with args.output.open('xb') as f:f.write(raw)
    print(f'Created {args.output}: {len(planned)} verified 32-bit patches; source preserved')

if __name__=='__main__':main()
