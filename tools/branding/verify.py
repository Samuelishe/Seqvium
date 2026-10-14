#!/usr/bin/env python3
# SPDX-License-Identifier: Apache-2.0
"""Check distributed hashes, static font coverage and optical ICO/ICNS frames without modifying assets."""
import argparse
import hashlib
import io
import json
import struct
from pathlib import Path

from PIL import Image, ImageChops
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--native', type=Path, help='Also compare captured exe-N.png files from check-windows-icons.py')
    args = parser.parse_args()
    hashes = 0
    for name in ('brand/fonts/manifest.json', 'brand/exports/manifest.json', 'brand/publication/captures.json'):
        data = json.loads((ROOT/name).read_text(encoding='utf-8'))
        for item in data['outputs'] if isinstance(data, dict) else data:
            assert hashlib.sha256((ROOT/item['path']).read_bytes()).hexdigest() == item['sha256'], item['path']
            hashes += 1
    for item in json.loads((ROOT/'brand/references/manifest.json').read_text(encoding='utf-8')):
        assert hashlib.sha256((ROOT/item['retained']).read_bytes()).hexdigest() == item['retained_sha256']
    icons = ROOT/'src/Seqvium.Desktop/Assets/Brand'
    data = (icons/'seqvium.ico').read_bytes()
    assert struct.unpack_from('<HHH', data) == (0, 1, 9)
    sizes = []
    for index in range(9):
        width, height, _, _, planes, bits, count, offset = struct.unpack_from('<BBBBHHII', data, 6+16*index)
        size = width or 256
        assert (height or 256) == size and planes == 1 and bits == 32
        frame = data[offset:offset+count]
        assert Image.open(io.BytesIO(frame)).size == (size, size)
        assert frame == (icons/f'Icons/seqvium-{size}.png').read_bytes(), 'ICO lost its optical frame'
        sizes.append(size)
    assert sizes == [16,20,24,32,40,48,64,128,256]
    icns = (ROOT/'brand/exports/seqvium.icns').read_bytes()
    assert icns[:4] == b'icns' and struct.unpack_from('>I', icns, 4)[0] == len(icns)
    offset, chunks = 8, []
    while offset < len(icns):
        count = struct.unpack_from('>I', icns, offset+4)[0]
        assert count > 8
        chunks.append((icns[offset:offset+4].decode(), Image.open(io.BytesIO(icns[offset+8:offset+count])).size))
        offset += count
    assert offset == len(icns) and len(chunks) == 7
    fonts = list((ROOT/'src/Seqvium.Desktop/Assets/Fonts').rglob('*.ttf'))
    for path in fonts:
        font = TTFont(path)
        assert 'fvar' not in font and (path.parent/'OFL.txt').exists(), path
        required = range(0x2669,0x2670) if 'Noto' in path.name else map(ord,
            'SeqviumАБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя0123456789−')
        assert all(code in font.getBestCmap() for code in required), path
    native = []
    if args.native:
        for size in sizes:
            actual = Image.open(args.native/f'exe-{size}.png').convert('RGBA')
            expected = Image.open(icons/f'Icons/seqvium-{size}.png').convert('RGBA')
            difference = ImageChops.difference(actual, expected)
            assert difference.getextrema()[3][1] == 0, 'Native alpha mismatch'
            for a, b in zip(actual.get_flattened_data(), expected.get_flattened_data()):
                if b[3] == 255:
                    assert a == b, 'Native opaque pixel mismatch'
                for channel in range(3):
                    assert abs(round(a[channel]*a[3]/255)-round(b[channel]*b[3]/255)) <= 1, 'Native premultiplied color mismatch'
            native.append({'size':size, 'alpha_and_opaque_exact':True,
                           'maximum_translucent_rgb_rounding':max(v[1] for v in difference.getextrema()[:3])})
    print(json.dumps({'hashes':hashes, 'ico_frames':sizes, 'icns':chunks, 'static_fonts':len(fonts), 'native':native}, indent=2))


if __name__ == '__main__':
    main()
