// SPDX-License-Identifier: Apache-2.0
#ifndef R0_KERNEL_H
#define R0_KERNEL_H
#include <stdint.h>
#define API __declspec(dllexport)
#define VOICES 8
#define EVENTS 16
#define SLOTS 4
#define RECORDS 65536
typedef struct { int32_t frame, kind, voice; float value; } Event;
typedef struct { uint64_t frame; int32_t kind, voice; } Trace;
// Only one execution thread may mutate Engine. Pointers borrow the active immutable slot.
typedef struct {
    uint64_t clock;
    int32_t position, event_index, playing;
    float gain;
    Event *events;
    float *table;
    int32_t event_count, loop_frames;
    int32_t phases[VOICES], steps[VOICES], releases[VOICES];
    float amplitudes[VOICES];
    Trace *trace;
    int32_t trace_count, trace_capacity;
    float *capture;
    int32_t capture_count, capture_capacity;
} Engine;
typedef void (*Processor)(Engine *, float *, int32_t, int32_t);
void render(Engine *, float *, int32_t, int32_t);
void release_voices(Engine *);
void clear_voices(Engine *);
#endif
