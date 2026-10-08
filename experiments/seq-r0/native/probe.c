// SPDX-License-Identifier: Apache-2.0
#include "probe.h"
#include <stdlib.h>
#include <string.h>
_Static_assert(ATOMIC_INT_LOCK_FREE == 2, "Control operations must be lock-free");
static _Atomic int live_probes = 0;
API int r0_live_probes(void) { return atomic_load(&live_probes); }
API double r0_empty_calls(Probe *p, int count) {
    if (p->thread || count < 1 || count > 1000000) return -1;
    LARGE_INTEGER frequency, a, b; QueryPerformanceFrequency(&frequency);
    float unused = 0;
    QueryPerformanceCounter(&a);
    for (int i = 0; i < count; ++i) p->processor(&p->engine, &unused, 0, 1);
    QueryPerformanceCounter(&b);
    return (b.QuadPart - a.QuadPart) * 1000000.0 / frequency.QuadPart / count;
}
API int r0_size(int which) {
    return which == 0 ? sizeof(Engine) : which == 1 ? sizeof(Stats) : which == 2 ? sizeof(Timing) : sizeof(Event);
}
API Probe *r0_create(Processor processor) {
    Probe *p = calloc(1, sizeof(Probe));
    if (!p) return NULL;
    atomic_fetch_add(&live_probes, 1);
    atomic_init(&p->pending, -1); p->active = -1;
    atomic_init(&p->requested, 0); atomic_init(&p->desired, 0);
    atomic_init(&p->stop, 0); atomic_init(&p->release, 0); atomic_init(&p->panic, 0);
    atomic_init(&p->gain_version, 0); atomic_init(&p->gain_bits, 0);
    atomic_init(&p->acknowledged_state, 0); atomic_init(&p->acknowledged_generation, 0);
    atomic_init(&p->initialization_result, -1);
    atomic_init(&p->publishes, 0); atomic_init(&p->rejected, 0);
    atomic_init(&p->superseded, 0); atomic_init(&p->retired, 0);
    for (int i = 0; i < SLOTS; ++i) atomic_init(&p->slots[i].state, 0);
    p->processor = processor ? processor : render;
    p->engine.gain = 1; p->engine.loop_frames = 1024;
    p->engine.trace = p->trace; p->engine.trace_capacity = RECORDS;
    p->engine.capture = p->capture; p->engine.capture_capacity = RECORDS * 8;
    return p;
}
API void r0_request(Probe *p, int generation) { atomic_store(&p->requested, generation); }
API int r0_publish(Probe *p, int generation, Event *events, int count, int loop, float *table) {
    // Publication is a single-producer operation. Callback never reads writing/free/retired slots.
    if (!events || !table || count < 1 || count > EVENTS || loop < 1 || loop > 1000000) return -2;
    for (int i = 0; i < count; ++i) {
        Event e = events[i];
        if (e.frame < 0 || e.frame >= loop || (i && e.frame < events[i-1].frame) ||
            e.kind < 1 || e.kind > 3 || e.voice < 0 || e.voice >= VOICES ||
            !(e.value >= 0 && e.value <= 255) || (e.kind == 1 && e.value < 1)) return -2;
    }
    for (int i = 0; i < 256; ++i) if (!(table[i] >= -1 && table[i] <= 1)) return -2;
    if (atomic_load(&p->requested) != generation) { atomic_fetch_add(&p->rejected, 1); return -1; }
    for (int i = 0; i < SLOTS; ++i) {
        int free_state = 0;
        if (!atomic_compare_exchange_strong(&p->slots[i].state, &free_state, 1)) continue;
        Prepared *s = &p->slots[i]; s->generation = generation; s->count = count; s->loop = loop;
        memcpy(s->events, events, count * sizeof(Event)); memcpy(s->table, table, sizeof(s->table));
        if (atomic_load(&p->requested) != generation) {
            atomic_store(&s->state, 4); atomic_fetch_add(&p->rejected, 1); return -1;
        }
        atomic_store(&s->state, 2);
        int old = atomic_exchange(&p->pending, i);
        if (old >= 0) { atomic_store(&p->slots[old].state, 4); atomic_fetch_add(&p->superseded, 1); }
        atomic_fetch_add(&p->publishes, 1); return 0;
    }
    atomic_fetch_add(&p->rejected, 1); return -3;
}
API int r0_reclaim(Probe *p) {
    int n = 0;
    for (int i = 0; i < SLOTS; ++i) {
        int retired = 4;
        if (atomic_compare_exchange_strong(&p->slots[i].state, &retired, 0)) ++n;
    }
    atomic_fetch_add(&p->retired, n); return n;
}
API void r0_control(Probe *p, int kind, float value) {
    switch (kind) {
        case 1: atomic_store(&p->desired, 1); break;
        case 2: atomic_store(&p->desired, 0); atomic_fetch_add(&p->stop, 1); break;
        case 3: atomic_fetch_add(&p->release, 1); break;
        case 4: atomic_store(&p->desired, 0); atomic_fetch_add(&p->panic, 1); break;
        case 5: { int bits; memcpy(&bits, &value, sizeof(bits));
            atomic_store(&p->gain_bits, bits); atomic_fetch_add(&p->gain_version, 1); break; }
    }
}
void begin_block(Probe *p) {
    Engine *e = &p->engine;
    int pending = atomic_exchange(&p->pending, -1);
    if (pending >= 0) {
        Prepared *s = &p->slots[pending];
        if (s->generation != atomic_load(&p->requested)) {
            atomic_store(&s->state, 4); atomic_fetch_add(&p->rejected, 1);
        } else {
            if (p->active >= 0) atomic_store(&p->slots[p->active].state, 4);
            p->active = pending; atomic_store(&s->state, 3);
            e->events = s->events; e->table = s->table; e->event_count = s->count; e->loop_frames = s->loop;
            e->position %= s->loop; e->event_index = 0;
            while (e->event_index < e->event_count && e->events[e->event_index].frame < e->position) ++e->event_index;
        }
    }
    int panic = atomic_load(&p->panic), stop = atomic_load(&p->stop), release = atomic_load(&p->release);
    if (panic != p->seen_panic) { clear_voices(e); e->playing = 0; p->seen_panic = panic; }
    if (stop != p->seen_stop) { release_voices(e); e->playing = 0; p->seen_stop = stop; }
    if (release != p->seen_release) { release_voices(e); p->seen_release = release; }
    if (atomic_load(&p->desired) && !e->playing && e->events) {
        clear_voices(e); e->position = 0; e->event_index = 0; e->playing = 1;
    }
    int gv = atomic_load(&p->gain_version);
    if (gv != p->seen_gain) { int bits = atomic_load(&p->gain_bits); memcpy(&e->gain, &bits, sizeof(bits)); p->seen_gain = gv; }
}
void skip_frames(Probe *p, uint64_t frames) {
    Engine *e = &p->engine;
    clear_voices(e);
    if (e->playing && e->events) {
        uint64_t cycles = frames / e->loop_frames;
        uint64_t remainder = frames % e->loop_frames;
        p->stats.skipped_events += cycles * e->event_count;
        for (int i = 0; i < e->event_count; ++i) {
            int distance = (e->events[i].frame - e->position + e->loop_frames) % e->loop_frames;
            if ((uint64_t)distance < remainder) ++p->stats.skipped_events;
        }
        e->position = (e->position + frames % e->loop_frames) % e->loop_frames;
        e->event_index = 0;
        while (e->event_index < e->event_count && e->events[e->event_index].frame < e->position) ++e->event_index;
    }
    e->clock += frames; p->stats.skipped_frames += frames;
}
void end_block(Probe *p) {
    int voices = 0;
    for (int i = 0; i < VOICES; ++i) if (p->engine.steps[i]) ++voices;
    atomic_store(&p->acknowledged_state, (p->engine.playing << 8) | voices);
    atomic_store(&p->acknowledged_generation, p->active >= 0 ? p->slots[p->active].generation : 0);
}
API void r0_process(Probe *p, float *out, int frames, int channels) {
    begin_block(p); p->processor(&p->engine, out, frames, channels); end_block(p);
}
API void r0_skip(Probe *p, uint64_t frames) { begin_block(p); skip_frames(p, frames); }
API Engine *r0_engine(Probe *p) { return &p->engine; }
API Timing *r0_timings(Probe *p) { return p->timings; }
API const wchar_t *r0_endpoint(Probe *p) { return p->endpoint; }
// Stats are read only after join, or in the single-thread controlled harness.
API void r0_stats(Probe *p, Stats *s) {
    *s = p->stats; s->publishes = atomic_load(&p->publishes); s->rejected = atomic_load(&p->rejected);
    s->superseded = atomic_load(&p->superseded); s->retired = atomic_load(&p->retired);
    s->active_generation = p->active >= 0 ? p->slots[p->active].generation : 0;
    s->live_slots = 0; for (int i = 0; i < SLOTS; ++i) if (atomic_load(&p->slots[i].state)) ++s->live_slots;
    s->voices = 0; for (int i = 0; i < VOICES; ++i) if (p->engine.steps[i]) ++s->voices;
    s->playing = p->engine.playing; s->trace_count = p->engine.trace_count; s->capture_count = p->engine.capture_count;
    s->acknowledged_state = atomic_load(&p->acknowledged_state);
    s->acknowledged_generation = atomic_load(&p->acknowledged_generation);
}
API int r0_open(Probe *p, int seconds, int inject) {
    if (p->thread || seconds < 1 || seconds > 30) return -1;
    if (p->has_opened) return -5;
    p->has_opened = 1;
    p->seconds = seconds; p->inject = inject;
    p->ready = CreateEventW(NULL, TRUE, FALSE, NULL); p->quit = CreateEventW(NULL, TRUE, FALSE, NULL);
    if (!p->ready || !p->quit) return -2;
    p->thread = CreateThread(NULL, 0, device_worker, p, 0, NULL);
    if (!p->thread) return -3;
    if (WaitForSingleObject(p->ready, 10000) != WAIT_OBJECT_0) return -4;
    return atomic_load(&p->initialization_result);
}
API int r0_join(Probe *p, int cancel) {
    if (cancel && p->quit) SetEvent(p->quit);
    if (p->thread) {
        if (WaitForSingleObject(p->thread, (p->seconds + 5) * 1000) != WAIT_OBJECT_0) return -1;
        CloseHandle(p->thread); p->thread = NULL;
    }
    if (p->ready) { CloseHandle(p->ready); p->ready = NULL; }
    if (p->quit) { CloseHandle(p->quit); p->quit = NULL; }
    return 0;
}
API int r0_simulate(Probe *p, int seconds, int frames, int inject) {
    if (p->thread || seconds < 1 || seconds > 30 || frames < 32 || frames > 4096) return -1;
    if (p->has_opened) return -5;
    p->has_opened = 1;
    p->seconds = seconds; p->simulated_frames = frames; p->inject = inject;
    p->ready = CreateEventW(NULL, TRUE, FALSE, NULL); p->quit = CreateEventW(NULL, TRUE, FALSE, NULL);
    if (!p->ready || !p->quit) return -2;
    p->thread = CreateThread(NULL, 0, controlled_worker, p, 0, NULL);
    if (!p->thread) return -3;
    if (WaitForSingleObject(p->ready, 10000) != WAIT_OBJECT_0) return -4;
    return atomic_load(&p->initialization_result);
}
API int r0_destroy(Probe *p) {
    if (!p) return 0;
    if (r0_join(p, 1)) return -1; // Never free state under a still-running callback.
    free(p); atomic_fetch_sub(&live_probes, 1); return 0;
}
