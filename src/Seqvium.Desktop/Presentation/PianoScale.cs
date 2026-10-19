// SPDX-License-Identifier: Apache-2.0

namespace Seqvium.Desktop.Presentation;

internal enum PianoScaleKind { Off, Major, NaturalMinor }

/// <summary>Session-only pitch-class guide, independent of source tuning and musical intent.</summary>
internal readonly record struct PianoScale(PianoScaleKind Kind = PianoScaleKind.Off, int Tonic = 0)
{
    internal static readonly string[] Tonics = ["C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B"];

    internal bool Contains(int pitch)
    {
        int interval = ((pitch - Tonic) % 12 + 12) % 12;
        return Kind switch
        {
            PianoScaleKind.Major => interval is 0 or 2 or 4 or 5 or 7 or 9 or 11,
            PianoScaleKind.NaturalMinor => interval is 0 or 2 or 3 or 5 or 7 or 8 or 10,
            _ => false
        };
    }

    internal bool IsTonic(int pitch) => Contains(pitch) && pitch % 12 == Tonic;
}
