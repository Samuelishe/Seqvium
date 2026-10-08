// SPDX-License-Identifier: Apache-2.0
namespace Seqvium.R0;

internal static unsafe class ManagedProcessor
{
    // Deliberately mirrors the small native processor, not the device adapter or future domain model.
    internal static void Render(Engine* e, float* output, int frames, int channels)
    {
        for (int f = 0; f < frames; ++f)
        {
            if (e->Playing != 0 && e->Events != null)
            {
                while (e->EventIndex < e->EventCount && e->Events[e->EventIndex].Frame == e->Position)
                {
                    var ev = e->Events[e->EventIndex++];
                    if (e->TraceCount < e->TraceCapacity)
                        e->Trace[e->TraceCount++] = new EventTrace { Frame = e->Clock, Kind = ev.Kind, Voice = ev.Voice };
                    if (ev.Kind == 1)
                    {
                        e->Phases[ev.Voice] = 0; e->Steps[ev.Voice] = (int)ev.Value;
                        e->Amplitudes[ev.Voice] = 0.02f; e->Releases[ev.Voice] = 0;
                    }
                    else if (ev.Kind == 2)
                    {
                        if (e->Steps[ev.Voice] != 0) e->Releases[ev.Voice] = 32;
                    }
                    else if (ev.Kind == 3) e->Gain = ev.Value;
                }
            }
            float sum = 0;
            for (int v = 0; v < 8; ++v)
            {
                if (e->Steps[v] == 0) continue;
                float envelope = e->Releases[v] != 0 ? e->Releases[v] / 32.0f : 1.0f;
                sum += (e->Table[e->Phases[v]] * e->Amplitudes[v]) * envelope;
                e->Phases[v] = (e->Phases[v] + e->Steps[v]) & 255;
                if (e->Releases[v] != 0 && --e->Releases[v] == 0) e->Steps[v] = 0;
            }
            sum *= e->Gain;
            for (int c = 0; c < channels; ++c)
            {
                output[f * channels + c] = sum;
                if (e->CaptureCount < e->CaptureCapacity) e->Capture[e->CaptureCount++] = sum;
            }
            ++e->Clock;
            if (e->Playing != 0 && ++e->Position == e->LoopFrames) { e->Position = 0; e->EventIndex = 0; }
        }
    }
}

internal static class Sequence
{
    internal static readonly MusicalEvent[] Events =
    [
        new(0, 1, 0, 3), new(255, 2, 0, 0), new(256, 1, 1, 5), new(257, 1, 2, 7),
        new(511, 2, 1, 0), new(512, 1, 3, 11), new(767, 2, 2, 0), new(768, 3, 0, 0.5f),
        new(1023, 2, 3, 0), new(1023, 1, 4, 13), new(1023, 2, 4, 0)
    ];
    internal static readonly float[] Table = Enumerable.Range(0, 256)
        .Select(i => i < 128 ? i / 64.0f - 1 : 3 - i / 64.0f).ToArray();
}
