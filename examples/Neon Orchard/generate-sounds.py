# SPDX-License-Identifier: Apache-2.0
"""Regenerate only the two original demo sounds; requires Python 3, no input audio.

Writes managed 44.1 kHz stereo PCM24 WAVs beside this script. Existing files must
match exactly; a different result refuses rather than overwriting accepted audio.
The project and VSCO samples are never edited by this generator.
"""

import hashlib
import io
import math
from pathlib import Path
import wave

RATE = 44100
ROOT = Path(__file__).resolve().parent / "Neon Orchard.seqvium.media" / "wav"


def bass(t):
    # A1 / MIDI 33: rounded analogue-style harmonic body with a decaying bright attack.
    phase = 2 * math.pi * 55 * t
    brightness = 0.15 + 0.85 * math.exp(-t / 0.14)
    body = math.sin(phase) + brightness * sum(
        math.sin(h * phase) / (h * h) for h in range(2, 13)
    )
    envelope = min(1, t / 0.004) * (0.72 + 0.28 * math.exp(-t / 0.4))
    envelope *= min(1, max(0, (214755 / RATE - t) / 0.08))
    return (0.57 * math.tanh(body) * envelope,) * 2


def effect(t, noise):
    # Independent falling electronic transition: chirp, metallic partials and filtered noise.
    phase = 2 * math.pi * (170 * t + 1150 * 0.23 * (1 - math.exp(-t / 0.23)))
    envelope = min(1, t / 0.008) * math.exp(-t / 0.7)
    envelope *= min(1, max(0, (136863 / RATE - t) / 0.08))
    left = 0.25 * math.sin(phase) + 0.13 * math.sin(phase * 1.419) + 0.12 * noise
    right = 0.25 * math.sin(phase) + 0.13 * math.sin(phase * 1.423) + 0.12 * noise
    return left * envelope, right * envelope


def render(kind, frames):
    pcm = bytearray()
    seed, filtered = 0x4E454F4E, 0.0
    for i in range(frames):
        t = i / RATE
        if kind == "bass":
            channels = bass(t)
        else:
            seed = (1664525 * seed + 1013904223) & 0xFFFFFFFF
            filtered += 0.22 * ((seed / 2147483648 - 1) - filtered)
            channels = effect(t, filtered)
        for sample in channels:
            value = max(-8388608, min(8388607, round(sample * 8388607)))
            pcm.extend(value.to_bytes(3, "little", signed=True))
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as wav:
        wav.setparams((2, 3, RATE, frames, "NONE", "not compressed"))
        wav.writeframes(pcm)
    return buffer.getvalue()


def main():
    ROOT.mkdir(parents=True, exist_ok=True)
    for kind, frames, identity in (
        ("bass", 214755, "663c86c3fb244e84b482b6690a4c9356"),
        ("fx", 136863, "9424a130ce1049a88fe2a9adcd927637"),
    ):
        data = render(kind, frames)
        digest = hashlib.sha256(data).hexdigest()
        path = ROOT / f"{identity}-{digest}.wav"
        previous = list(ROOT.glob(f"{identity}-*.wav"))
        if previous and (previous != [path] or path.read_bytes() != data):
            raise RuntimeError("Generated sound differs; review as an explicit musical change.")
        if not previous:
            with path.open("xb") as output:
                output.write(data)
        print(f"{kind}: {path.name} ({len(data)} bytes; {frames} frames)")


if __name__ == "__main__":
    main()
