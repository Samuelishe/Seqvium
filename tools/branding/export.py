#!/usr/bin/env python3
# SPDX-License-Identifier: Apache-2.0
"""Deterministic offline exports from hand-authored SVG masters and bundled font outlines."""
import hashlib
import argparse
import io
import json
import struct
from pathlib import Path
import xml.etree.ElementTree as ET

import resvg_py
from PIL import Image, ImageDraw, ImageFont
from fontTools.ttLib import TTFont
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.varLib.instancer import instantiateVariableFont

ROOT = Path(__file__).resolve().parents[2]
BRAND = ROOT / "brand"
ASSETS = ROOT / "src/Seqvium.Desktop/Assets"
SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256, 512, 1024)
INK, PAPER, CYAN, TEAL, MINT, SLATE = '#101617', '#F1F5F3', '#00E5D1', '#14B8A6', '#A7F3D0', '#5B6B70'


def save(path, content):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(content.encode('utf-8') if isinstance(content, str) else content)


def svg(body, width, height):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}">{body}</svg>\n'


def contents(path):
    root = ET.parse(path).getroot()
    source = ''.join(ET.tostring(child, encoding='unicode') for child in root)
    prefix = path.parent.name + '-' + path.stem + '-'
    for node in root.iter():
        if 'id' in node.attrib:
            name = node.attrib['id']
            source = source.replace(f'id="{name}"', f'id="{prefix}{name}"').replace(f'url(#{name})',f'url(#{prefix}{name})')
    return source


def raster(source, width, height=None):
    return resvg_py.svg_to_bytes(svg_string=source, width=width, height=height, skip_system_fonts=True)


def outlined(text, size, x, y, color, weight='Black', tracking=0, font=None):
    font = font or TTFont(ASSETS / f'Fonts/Onest/Onest-{weight}.ttf')
    glyphs, cmap = font.getGlyphSet(), font.getBestCmap()
    scale = size/font['head'].unitsPerEm
    cursor, paths = 0, []
    for letter in text:
        name = cmap[ord(letter)]
        pen = SVGPathPen(glyphs)
        glyphs[name].draw(pen)
        paths.append(f'<path transform="translate({cursor:.3f} 0)" d="{pen.getCommands()}"/>')
        cursor += font['hmtx'].metrics[name][0] + tracking
    return f'<g aria-label="{text}" fill="{color}" transform="translate({x} {y}) scale({scale:.6f} {-scale:.6f})">'+''.join(paths)+'</g>'


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--captures',type=Path,help='Explicitly adopt the successful identity run as real publication screenshots')
    args=parser.parse_args()
    frames = {}
    for size in SIZES:
        optical = 'tiny' if size == 16 else 'small' if size <= 40 else None
        master = BRAND / ('masters/seqvium-09'+('-'+optical if optical else '')+'.svg')
        frames[size] = raster(master.read_text(encoding='utf-8'), size, size)
        save(ASSETS / f'Brand/Icons/seqvium-{size}.png', frames[size])
    # Explicit PNG-frame ICO; preserves optical versions instead of re-scaling the large frame.
    icon_sizes = (16, 20, 24, 32, 40, 48, 64, 128, 256)
    offset = 6 + 16*len(icon_sizes)
    entries = []
    for size in icon_sizes:
        frame = frames[size]
        entries.append(struct.pack('<BBBBHHII', size % 256, size % 256, 0, 0, 1, 32, len(frame), offset))
        offset += len(frame)
    save(ASSETS / 'Brand/seqvium.ico', struct.pack('<HHH', 0, 1, len(entries))+b''.join(entries)+b''.join(frames[s] for s in icon_sizes))
    # PNG-backed ICNS for future packaging only; no claim of a macOS bundle/runtime.
    chunks = []
    for kind, size in [(b'icp4',16),(b'icp5',32),(b'icp6',64),(b'ic07',128),(b'ic08',256),(b'ic09',512),(b'ic10',1024)]:
        chunks.append(kind+struct.pack('>I',8+len(frames[size]))+frames[size])
    data = b''.join(chunks)
    save(BRAND / 'exports/seqvium.icns', b'icns'+struct.pack('>I',8+len(data))+data)
    save(BRAND / 'exports/seqvium.svg', (BRAND/'masters/seqvium-09.svg').read_bytes())
    bare = ET.parse(BRAND/'masters/seqvium-09.svg').getroot()
    for node in list(bare):
        if node.tag.endswith('rect'):
            bare.remove(node)
    symbol = ET.tostring(bare,encoding='unicode')
    save(BRAND/'exports/symbol.svg',symbol)
    save(BRAND/'exports/symbol-1024.png',raster(symbol,1024,1024))
    for mode, color in [('ink',INK),('chalk',PAPER)]:
        wordmark = svg(outlined('Seqvium', 132, 8, 111, color, tracking=-26), 550, 150)
        save(BRAND / f'exports/wordmark-{mode}.svg', wordmark)
        save(BRAND / f'exports/wordmark-{mode}.png', raster(wordmark, 1100, 300))
        lockup = svg(f'<g transform="translate(0 0) scale(.28)">{contents(BRAND/"masters/seqvium-09.svg")}</g>'+outlined('Seqvium',100,164,109,color,tracking=-26),590,150)
        save(BRAND / f'exports/lockup-{mode}.svg', lockup)
        save(BRAND / f'exports/lockup-{mode}.png',raster(lockup,1180,300))
    for alternative in ('01','07'):
        master = BRAND / f'alternatives/{alternative}/master.svg'
        save(master.parent/'icon-512.png',raster(master.read_text(encoding='utf-8'),512,512))

    # Publication artwork contains no pretend UI. All lettering is outlined, no system fonts.
    def publication(width,height,compact):
        title_size = 124 if compact else 104
        left = 70 if compact else 64
        top = 215 if compact else 160
        art = f'<rect width="{width}" height="{height}" fill="{INK}"/>'
        art += f'<path d="M{width-330} -20 H{width+50} V{height+20} H{width-330}Z" fill="#131E1D"/>'
        art += outlined('Seqvium', title_size,left,top,PAPER,tracking=-26)
        art += outlined('chaos into structure.',34 if compact else 30,left,top+68,MINT,'Regular')
        art += outlined('A music workstation for ideas in motion.',18,left,height-95,'#A9BCB8','Regular')
        art += outlined('OPEN SOURCE  /  EARLY ALPHA',13,left,height-57,'#88A29D','SemiBold',90)
        marksize=400 if compact else 310
        art += f'<g transform="translate({width-marksize-46} {(height-marksize)/2}) scale({marksize/512})">{contents(BRAND/"masters/seqvium-09.svg")}</g>'
        return svg(art,width,height)
    for name,width,height,compact in [('readme-header',1400,440,False),('social-preview',1280,640,True)]:
        source=publication(width,height,compact)
        save(BRAND/f'publication/{name}.svg',source)
        save(BRAND/f'publication/{name}.png',raster(source,width,height))

    # Review sheet shows actual size samples as well as enlarged pixels, on two backgrounds.
    sheet=Image.new('RGB',(1200,760),PAPER)
    draw=ImageDraw.Draw(sheet)
    font=lambda size:ImageFont.truetype(str(ASSETS/'Fonts/Onest/Onest-Regular.ttf'),size)
    draw.text((32,20),'SEQVIUM 09 / OPTICAL SIZES',font=font(26),fill=INK)
    for row,bg,fg in [(0,PAPER,INK),(1,INK,PAPER)]:
        y=90+row*280
        draw.rectangle((0,y,1200,y+280),fill=bg)
        for index,size in enumerate([16,20,24,32,48,64,128,256]):
            x=24+index*140
            icon=Image.open(io.BytesIO(frames[size])).convert('RGBA')
            actual=icon if size<=128 else icon.resize((128,128),Image.Resampling.LANCZOS)
            sheet.paste(actual,(x,y+45),actual)
            draw.text((x,y+12),str(size)+' px'+(' / half' if size==256 else ''),font=font(16),fill=fg)
            if size<=32:
                zoom=icon.resize((96,96),Image.Resampling.NEAREST)
                sheet.paste(zoom,(x,y+105),zoom)
    draw.text((32,690),'16: fitted pixels   /   20–40: optical master   /   48–1024: full master',font=font(22),fill=INK)
    sheet.save(BRAND/'publication/icon-sizes.png',optimize=True)
    # A concise board for all three usable directions, with source/outcome distinction in the guide.
    board=svg(f'<rect width="1440" height="660" fill="{PAPER}"/>'+outlined('Seqvium',90,45,112,INK,tracking=-26)+outlined('chaos into structure.',24,48,164,SLATE,'Regular')+''.join(
        f'<g transform="translate({50+i*465} 210) scale(.72)">{contents(path)}</g>'+outlined(label,24,64+i*465,625,INK,'SemiBold')
        for i,(path,label) in enumerate([(BRAND/'masters/seqvium-09.svg','09 / primary'),(BRAND/'alternatives/01/master.svg','01 / layered sequence'),(BRAND/'alternatives/07/master.svg','07 / structured flow')])),1440,660)
    save(BRAND/'publication/directions.svg',board)
    save(BRAND/'publication/directions.png',raster(board,1440,660))
    comparison=f'<rect width="1200" height="660" fill="{PAPER}"/>'
    for y,name,weight in [(35,'Onest',900),(350,'Manrope',800)]:
        face=instantiateVariableFont(TTFont(BRAND/f'fonts/sources/{name}/{name}[wght].ttf'),{'wght':weight})
        comparison += outlined(f'{name} / {weight}',20,40,y+20,SLATE,'Regular')
        comparison += outlined('Seqvium',120,35,y+150,INK,tracking=-26,font=face)
        comparison += outlined('Паттерн / Channel Rack / 128.00 BPM',28,40,y+217,INK,font=face)
    save(BRAND/'publication/typography-comparison.svg',svg(comparison,1200,660))
    save(BRAND/'publication/typography-comparison.png',raster(svg(comparison,1200,660),1200,660))

    if args.captures:
        receipt=json.loads((args.captures/'identity.json').read_text(encoding='utf-8'))
        run=json.loads((args.captures/'.seqvium-run.json').read_text(encoding='utf-8'))
        assert receipt['FontsEmbedded'] and not receipt['PhysicalAudio'] and run['Status']=='success'
        captures=[]
        for theme in ('Dark','Light'):
            source=args.captures/f'workspace-English-{theme}.png'
            target=BRAND/f'publication/workspace-{theme.lower()}.webp'
            screenshot=Image.open(source).convert('RGB')
            screenshot.save(target,lossless=True,method=6)
            captures.append({'path':target.relative_to(ROOT).as_posix(),'source_image':source.name,
                             'source_sha256':hashlib.sha256(source.read_bytes()).hexdigest(),
                             'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),
                             'pixels':screenshot.size,'conversion':'lossless WebP, no crop or compositing',
                             'origin':'Actual Seqvium MainWindow, isolated VisualCheck identity route',
                             'project':'examples/Neon Orchard/Neon Orchard.seqvium','version':receipt['Version'],
                             'language':'English','theme':theme,'native_input':False})
        save(BRAND/'publication/captures.json',json.dumps(captures,indent=2)+'\n')
    outputs = sorted([*ASSETS.glob('Brand/**/*.png'),ASSETS/'Brand/seqvium.ico',*BRAND.glob('exports/*'),
                      *(p for p in BRAND.glob('publication/*') if p.suffix in ('.svg','.png')),*BRAND.glob('alternatives/*/*.png')])
    save(BRAND/'exports/manifest.json',json.dumps({
        'generator':'tools/branding/export.py','ico_frames':list(icon_sizes),
        'outputs':[{'path':p.relative_to(ROOT).as_posix(),'sha256':hashlib.sha256(p.read_bytes()).hexdigest()} for p in outputs if p.name!='manifest.json']},indent=2)+'\n')


if __name__=='__main__':
    main()
