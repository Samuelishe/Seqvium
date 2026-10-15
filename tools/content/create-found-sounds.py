#!/usr/bin/env python3
# SPDX-License-Identifier: Apache-2.0
"""Reproduce the original, data-only Found Sounds package. No external inputs or libraries."""
import math
from pathlib import Path
import random
import struct
import wave

ROOT = Path(__file__).resolve().parents[2] / "src/Seqvium.Desktop/Extensions/seqvium.found-sounds"
RATE = 48000


def write(name, seconds, signal):
    count = round(seconds * RATE)
    values = []
    for i in range(count):
        t = i / RATE
        fade = min(1, i / 96, (count - 1 - i) / 240)
        values.append(round(max(-1, min(1, signal(t) * fade)) * 32767))
    with wave.open(str(ROOT / name), "wb") as output:
        output.setparams((1, 2, RATE, count, "NONE", "not compressed"))
        output.writeframes(struct.pack("<" + "h" * count, *values))


write("Soft Pulse.wav", .32, lambda t: .45 * math.exp(-t * 18) * math.sin(2 * math.pi * (62 * t + 4 * (1 - math.exp(-t * 55)))))
write("Glass Drop.wav", .48, lambda t: .22 * math.exp(-t * 12) * (math.sin(2 * math.pi * 880 * t) + .45 * math.sin(2 * math.pi * 2173 * t)))
rng = random.Random(8675309)
write("Dust Tick.wav", .12, lambda t: .3 * math.exp(-t * 75) * rng.uniform(-1, 1))
