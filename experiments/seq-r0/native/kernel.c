// SPDX-License-Identifier: Apache-2.0
#include "kernel.h"
#include <string.h>
void clear_voices(Engine *e) {
    memset(e->steps, 0, sizeof(e->steps));
    memset(e->releases, 0, sizeof(e->releases));
    memset(e->amplitudes, 0, sizeof(e->amplitudes));
}
void release_voices(Engine *e) {
    for (int v = 0; v < VOICES; ++v) if (e->steps[v]) e->releases[v] = 32;
}
void render(Engine *e, float *out, int32_t frames, int32_t channels) {
    for (int f = 0; f < frames; ++f) {
        if (e->playing && e->events) {
            while (e->event_index < e->event_count && e->events[e->event_index].frame == e->position) {
                Event ev = e->events[e->event_index++];
                if (e->trace_count < e->trace_capacity)
                    e->trace[e->trace_count++] = (Trace){e->clock, ev.kind, ev.voice};
                if (ev.kind == 1) {
                    e->phases[ev.voice] = 0; e->steps[ev.voice] = (int)ev.value;
                    e->amplitudes[ev.voice] = 0.02f; e->releases[ev.voice] = 0;
                } else if (ev.kind == 2) {
                    if (e->steps[ev.voice]) e->releases[ev.voice] = 32;
                } else if (ev.kind == 3) e->gain = ev.value;
            }
        }
        float sum = 0;
        for (int v = 0; v < VOICES; ++v) if (e->steps[v]) {
            float envelope = e->releases[v] ? e->releases[v] / 32.0f : 1.0f;
            sum += (e->table[e->phases[v]] * e->amplitudes[v]) * envelope;
            e->phases[v] = (e->phases[v] + e->steps[v]) & 255;
            if (e->releases[v] && --e->releases[v] == 0) e->steps[v] = 0;
        }
        sum *= e->gain;
        for (int c = 0; c < channels; ++c) {
            out[f * channels + c] = sum;
            if (e->capture_count < e->capture_capacity) e->capture[e->capture_count++] = sum;
        }
        ++e->clock;
        if (e->playing && ++e->position == e->loop_frames) { e->position = 0; e->event_index = 0; }
    }
}
