// SPDX-License-Identifier: Apache-2.0
#ifndef R0_PROBE_H
#define R0_PROBE_H
#include "kernel.h"
#include <windows.h>
#include <stdatomic.h>
typedef struct {
    _Atomic int state; // free, writing, pending, active, retired
    int generation, count, loop;
    Event events[EVENTS];
    float table[256];
} Prepared;
typedef struct {
    double processing_us, service_us, interval_us;
    int32_t frames, padding;
    uint64_t clock, device_frame;
} Timing;
typedef struct {
    int32_t rate, channels, buffer_frames, period_frames;
    int32_t hr, callbacks, padding_empty, timing_full;
    uint64_t skipped_frames, skipped_events, clock_regressions;
    uint64_t device_frame, submitted_frame;
    double elapsed_seconds, qpc_pair_us;
    int32_t publishes, rejected, superseded, retired, active_generation, live_slots;
    int32_t voices, playing, trace_count, capture_count;
    int32_t mmcss;
    uint64_t clock_frequency;
    double stream_latency_ms;
    int32_t acknowledged_state, acknowledged_generation;
    int64_t maximum_wall_deficit, maximum_post_recovery_deficit, final_wall_deficit;
    int32_t recoveries;
} Stats;
typedef struct {
    Engine engine;
    Prepared slots[SLOTS];
    _Atomic int pending, requested, desired;
    _Atomic int stop, release, panic, gain_version;
    _Atomic int gain_bits;
    _Atomic int acknowledged_state, acknowledged_generation;
    _Atomic int initialization_result;
    int seen_stop, seen_release, seen_panic, seen_gain, active;
    _Atomic int publishes, rejected, superseded, retired;
    Processor processor;
    Stats stats;
    Timing timings[RECORDS];
    Trace trace[RECORDS];
    float capture[RECORDS * 8];
    HANDLE thread, ready, quit;
    int seconds, inject;
    int simulated_frames;
    int has_opened;
    wchar_t endpoint[512];
} Probe;
void begin_block(Probe *);
void end_block(Probe *);
void skip_frames(Probe *, uint64_t);
DWORD WINAPI device_worker(void *);
DWORD WINAPI controlled_worker(void *);
#endif
