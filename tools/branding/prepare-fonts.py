#!/usr/bin/env python3
# SPDX-License-Identifier: Apache-2.0
"""Fetch pinned OFL sources explicitly, or rebuild static application instances offline."""
import argparse
import hashlib
import json
from pathlib import Path
from urllib.request import urlopen

from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont
from fontTools import subset

ROOT = Path(__file__).resolve().parents[2]
SOURCES = {
    "Onest": ("onest", "4c1db3aec83c67dd223dd82a68c039cab30917a9", "Onest[wght].ttf"),
    "Manrope": ("manrope", "b31870aff700ab7a1d74fa0c6887d95beb9e0037", "Manrope[wght].ttf"),
    "JetBrainsMono": ("jetbrainsmono", "6e4b84c976cadb3c49a40fd9a1c203e4f7fcf2da", "JetBrainsMono[wght].ttf"),
    "NotoSansSymbols": ("notosanssymbols", "8b0a1d0f5983c89bc2b93f1b5fb55f9e252744b5", "NotoSansSymbols[wght].ttf"),
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fetch", action="store_true", help="Download the exact pinned public source files")
    args = parser.parse_args()
    records = []
    for family, (folder, revision, filename) in SOURCES.items():
        directory = ROOT / "brand/fonts/sources" / family
        directory.mkdir(parents=True, exist_ok=True)
        base = f"https://raw.githubusercontent.com/google/fonts/{revision}/ofl/{folder}/"
        for member in (filename, "OFL.txt", "METADATA.pb"):
            target = directory / member
            if args.fetch:
                with urlopen(base + member.replace("[", "%5B").replace("]", "%5D"), timeout=45) as response:
                    target.write_bytes(response.read())
            records.append({"path": target.relative_to(ROOT).as_posix(), "source": base + member,
                            "sha256": hashlib.sha256(target.read_bytes()).hexdigest()})
        print(family, (directory / "OFL.txt").read_text(encoding="utf-8").splitlines()[0])
        if family == "Manrope":
            continue  # Comparison source, deliberately not embedded in the application.
        weights = [(400, "Regular"), (600, "SemiBold"), (900, "Black")] if family == "Onest" else [(400, "Regular")]
        destination = ROOT / "src/Seqvium.Desktop/Assets/Fonts" / family
        destination.mkdir(parents=True, exist_ok=True)
        (destination / "OFL.txt").write_bytes((directory / "OFL.txt").read_bytes())
        if family == "NotoSansSymbols":
            font = instantiateVariableFont(TTFont(directory / filename, recalcTimestamp=False), {'wght':400})
            options = subset.Options()
            options.name_IDs = ['*']
            options.name_legacy = True
            options.name_languages = ['*']
            smaller = subset.Subsetter(options=options)
            smaller.populate(unicodes=range(0x2669, 0x2670))
            smaller.subset(font)
            target = destination / 'NotoSansSymbols-Music.ttf'
            font.save(target)
            records.append({'path':target.relative_to(ROOT).as_posix(),
                            'derived_from':(directory/filename).relative_to(ROOT).as_posix(),
                            'subset':'U+2669–U+266F', 'sha256':hashlib.sha256(target.read_bytes()).hexdigest()})
            continue
        for weight, style in weights:
            font = TTFont(directory / filename, recalcTimestamp=False)
            instance = instantiateVariableFont(font, {"wght": weight}, inplace=False)
            # One resolvable embedded family with explicit static style/weight metadata.
            name = "JetBrains Mono" if family == "JetBrainsMono" else family
            for platform, encoding, language in [(3, 1, 0x409), (1, 0, 0)]:
                for name_id, value in [(1, name), (2, style), (4, f"{name} {style}"),
                                       (6, f"{family}-{style}"), (16, name), (17, style)]:
                    instance["name"].setName(value, name_id, platform, encoding, language)
            instance["OS/2"].usWeightClass = weight
            instance["OS/2"].fsSelection &= ~0x61
            instance["OS/2"].fsSelection |= 0x20 if weight >= 600 else 0x40
            instance["head"].macStyle = 1 if weight >= 600 else 0
            target = destination / f"{family}-{style}.ttf"
            instance.save(target)
            records.append({"path": target.relative_to(ROOT).as_posix(),
                            "derived_from": (directory / filename).relative_to(ROOT).as_posix(),
                            "weight": weight, "sha256": hashlib.sha256(target.read_bytes()).hexdigest()})
    (ROOT / "brand/fonts/manifest.json").write_text(json.dumps(records, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
