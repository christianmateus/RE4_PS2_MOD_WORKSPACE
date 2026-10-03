"""Read-only comparison and extraction of the CGH SLUS menu wrapper."""
from pathlib import Path
import hashlib
import lzma
import struct

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / '_references' / 'ELFs'
OUT = ROOT / 'artifacts' / 'slus_menu_analysis'
OUT.mkdir(parents=True, exist_ok=True)
original = (SOURCE / 'SLUS_211.34_original').read_bytes()
menu = (SOURCE / 'SLUS_211.34_mod_Menu').read_bytes()
filters = [{'id': lzma.FILTER_LZMA1, 'dict_size': 0x400000,
            'lc': 3, 'lp': 0, 'pb': 2}]
decoder = lzma.LZMADecompressor(format=lzma.FORMAT_RAW, filters=filters)
wrapper = decoder.decompress(menu[0x2EBD:])
assert decoder.eof
game_size, initial_code = struct.unpack_from('<II', wrapper, 0x61010)
# The inner decoder stores the initial range code as a little-endian word,
# followed by the remaining LZMA stream, instead of the standard five bytes.
stream = b'\0' + initial_code.to_bytes(4, 'big') + wrapper[0x61018:]
decoder = lzma.LZMADecompressor(format=lzma.FORMAT_RAW, filters=filters)
game = decoder.decompress(stream, max_length=game_size)
program = struct.unpack_from('<8I', original, 52)
reference = original[program[1]:program[1] + program[4]]
assert len(game) == len(reference) == game_size
assert game == reference
(OUT / 'menu_unpacked.bin').write_bytes(wrapper)
(OUT / 'game_segment.bin').write_bytes(game)
print('File sizes:', len(original), len(menu))
print('Raw differing bytes:', sum(a != b for a, b in zip(original, menu)))
print('Wrapper size:', len(wrapper))
print('Original game segment:', len(game), 'bytes; identical:', game == reference)
print('Game segment SHA-256:', hashlib.sha256(game).hexdigest())
for name, data in [('original', original), ('menu', menu)]:
    print(name, 'entry:', hex(struct.unpack_from('<I', data, 24)[0]),
          'SHA-256:', hashlib.sha256(data).hexdigest())
