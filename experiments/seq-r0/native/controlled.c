// SPDX-License-Identifier: Apache-2.0
#include "probe.h"
#include <avrt.h>
static int64_t ticks(void) { LARGE_INTEGER t; QueryPerformanceCounter(&t); return t.QuadPart; }
DWORD WINAPI controlled_worker(void *context) {
    Probe *p = context;
    LARGE_INTEGER frequency; QueryPerformanceFrequency(&frequency);
    double scale = 1000000.0 / frequency.QuadPart;
    float buffer[4096];
    HANDLE timer = CreateWaitableTimerExW(NULL, NULL, CREATE_WAITABLE_TIMER_HIGH_RESOLUTION, TIMER_ALL_ACCESS);
    if (!timer) {
        p->stats.hr = HRESULT_FROM_WIN32(GetLastError()); atomic_store(&p->initialization_result, p->stats.hr);
        SetEvent(p->ready); return 0;
    }
    DWORD task = 0; HANDLE priority = AvSetMmThreadCharacteristicsW(L"Pro Audio", &task);
    p->stats.mmcss = priority != NULL;
    p->stats.rate = 48000; p->stats.channels = 1;
    p->stats.buffer_frames = p->stats.period_frames = p->simulated_frames;
    int64_t overhead = 0;
    for (int i = 0; i < 10000; ++i) { int64_t a = ticks(); overhead += ticks() - a; }
    p->stats.qpc_pair_us = overhead * scale / 10000;
    int64_t first = ticks(), previous = first;
    atomic_store(&p->initialization_result, 0); SetEvent(p->ready);
    while (ticks() - first < (int64_t)p->seconds * frequency.QuadPart) {
        if (WaitForSingleObject(p->quit, 0) == WAIT_OBJECT_0) break;
        int64_t start = ticks();
        if (p->inject && p->stats.callbacks > 0 && p->stats.callbacks % 100 == 0) {
            int64_t until = start + frequency.QuadPart * 30 / 1000;
            while (ticks() < until) { YieldProcessor(); }
        }
        uint64_t target = (uint64_t)((ticks() - first) * 48000 / frequency.QuadPart);
        begin_block(p);
        // A sub-period timer wake delay is jitter, not a reason to kill voices every block.
        // Keep an absolute sample-grid deadline; only a whole-block deficit triggers recovery.
        if (target > p->engine.clock + (uint64_t)p->simulated_frames) {
            uint64_t aligned = target / p->simulated_frames * p->simulated_frames;
            skip_frames(p, aligned - p->engine.clock); ++p->stats.recoveries;
        }
        int64_t a = ticks();
        p->processor(&p->engine, buffer, p->simulated_frames, 1);
        end_block(p);
        int64_t b = ticks();
        int i = p->stats.callbacks++;
        if (i < RECORDS) p->timings[i] = (Timing){(b-a)*scale, (b-start)*scale, (start-previous)*scale,
            p->simulated_frames, 0, p->engine.clock, target};
        else ++p->stats.timing_full;
        p->stats.device_frame = target; p->stats.submitted_frame = p->engine.clock;
        previous = start;
        int64_t next = first + (int64_t)(p->engine.clock * frequency.QuadPart / 48000);
        int64_t remaining = next - ticks();
        LARGE_INTEGER due; due.QuadPart = remaining > 0 ? -(remaining * 10000000 / frequency.QuadPart) : -1;
        if (!SetWaitableTimer(timer, &due, 0, NULL, NULL, FALSE)) {
            p->stats.hr = HRESULT_FROM_WIN32(GetLastError()); break;
        }
        HANDLE waits[2] = {p->quit, timer};
        DWORD wait = WaitForMultipleObjects(2, waits, FALSE, 2000);
        if (wait == WAIT_OBJECT_0) break;
        if (wait != WAIT_OBJECT_0 + 1) { p->stats.hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT); break; }
    }
    p->stats.elapsed_seconds = (ticks()-first) / (double)frequency.QuadPart;
    clear_voices(&p->engine); p->engine.playing = 0;
    if (priority) AvRevertMmThreadCharacteristics(priority);
    CloseHandle(timer); return 0;
}
