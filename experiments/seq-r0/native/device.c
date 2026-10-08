// SPDX-License-Identifier: Apache-2.0
#define COBJMACROS
#define INITGUID
#include "probe.h"
#include <mmdeviceapi.h>
#include <audioclient.h>
#include <ks.h>
#include <ksmedia.h>
#include <avrt.h>
#include <string.h>

// Standard IEEE_FLOAT subtype; MinGW's ksmedia named GUID is not materialized by INITGUID in C.
static const GUID float_subtype = {3, 0, 0x0010, {0x80, 0, 0, 0xaa, 0, 0x38, 0x9b, 0x71}};

#define CHECK(call) do { hr = (call); if (FAILED(hr)) goto done; } while (0)
static int64_t ticks(void) { LARGE_INTEGER t; QueryPerformanceCounter(&t); return t.QuadPart; }
DWORD WINAPI device_worker(void *context) {
    Probe *p = context;
    IMMDeviceEnumerator *enumerator = NULL;
    IMMDevice *device = NULL;
    IAudioClient *client = NULL;
    IAudioRenderClient *output = NULL;
    IAudioClock *clock = NULL;
    WAVEFORMATEX *format = NULL;
    HANDLE event = NULL, priority = NULL;
    LPWSTR endpoint = NULL;
    HRESULT hr = S_OK;
    int initialized = 0, started = 0, ready = 0;
    LARGE_INTEGER frequency; QueryPerformanceFrequency(&frequency);
    double scale = 1000000.0 / frequency.QuadPart;
    int64_t first = 0, previous = 0;
    int64_t anchor_qpc = 0;
    uint64_t anchor_clock = 0;
    UINT32 anchor_padding = 0;
    UINT64 clock_frequency = 0, last_device_frame = 0;
    UINT32 capacity = 0;
    REFERENCE_TIME period = 0, minimum = 0;
    CHECK(CoInitializeEx(NULL, COINIT_MULTITHREADED)); initialized = 1;
    CHECK(CoCreateInstance(&CLSID_MMDeviceEnumerator, NULL, CLSCTX_ALL,
        &IID_IMMDeviceEnumerator, (void **)&enumerator));
    CHECK(IMMDeviceEnumerator_GetDefaultAudioEndpoint(enumerator, eRender, eConsole, &device));
    CHECK(IMMDevice_GetId(device, &endpoint));
    wcsncpy(p->endpoint, endpoint, 511);
    CHECK(IMMDevice_Activate(device, &IID_IAudioClient, CLSCTX_ALL, NULL, (void **)&client));
    CHECK(IAudioClient_GetMixFormat(client, &format));
    int is_float = format->wFormatTag == WAVE_FORMAT_IEEE_FLOAT ||
        (format->wFormatTag == WAVE_FORMAT_EXTENSIBLE && format->cbSize >= 22 &&
         IsEqualGUID(&((WAVEFORMATEXTENSIBLE *)format)->SubFormat, &float_subtype));
    if (!is_float || format->wBitsPerSample != 32 || format->nChannels < 1 || format->nChannels > 8) {
        hr = AUDCLNT_E_UNSUPPORTED_FORMAT; goto done;
    }
    CHECK(IAudioClient_GetDevicePeriod(client, &period, &minimum));
    CHECK(IAudioClient_Initialize(client, AUDCLNT_SHAREMODE_SHARED, AUDCLNT_STREAMFLAGS_EVENTCALLBACK,
        0, 0, format, NULL));
    CHECK(IAudioClient_GetBufferSize(client, &capacity));
    CHECK(IAudioClient_GetService(client, &IID_IAudioRenderClient, (void **)&output));
    CHECK(IAudioClient_GetService(client, &IID_IAudioClock, (void **)&clock));
    CHECK(IAudioClock_GetFrequency(clock, &clock_frequency));
    p->stats.clock_frequency = clock_frequency;
    REFERENCE_TIME latency;
    CHECK(IAudioClient_GetStreamLatency(client, &latency));
    p->stats.stream_latency_ms = latency / 10000.0;
    event = CreateEventW(NULL, FALSE, FALSE, NULL);
    if (!event) { hr = HRESULT_FROM_WIN32(GetLastError()); goto done; }
    CHECK(IAudioClient_SetEventHandle(client, event));
    p->stats.rate = format->nSamplesPerSec; p->stats.channels = format->nChannels;
    p->stats.buffer_frames = capacity;
    p->stats.period_frames = (int)((period * format->nSamplesPerSec + 5000000) / 10000000);
    int64_t overhead = 0;
    for (int i = 0; i < 10000; ++i) { int64_t a = ticks(); overhead += ticks() - a; }
    p->stats.qpc_pair_us = overhead * scale / 10000;
    DWORD task = 0; priority = AvSetMmThreadCharacteristicsW(L"Pro Audio", &task);
    p->stats.mmcss = priority != NULL;
    // Prefill uses the same processor but is excluded from steady-state timing records.
    BYTE *buffer;
    CHECK(IAudioRenderClient_GetBuffer(output, capacity, &buffer));
    begin_block(p); p->processor(&p->engine, (float *)buffer, capacity, format->nChannels); end_block(p);
    CHECK(IAudioRenderClient_ReleaseBuffer(output, capacity, 0));
    CHECK(IAudioClient_Start(client)); started = 1;
    first = previous = ticks(); p->stats.hr = 0; atomic_store(&p->initialization_result, 0);
    SetEvent(p->ready); ready = 1;
    HANDLE waits[2] = {p->quit, event};
    while ((ticks() - first) < (int64_t)p->seconds * frequency.QuadPart) {
        DWORD wait = WaitForMultipleObjects(2, waits, FALSE, 2000);
        if (wait == WAIT_OBJECT_0) break;
        if (wait != WAIT_OBJECT_0 + 1) { hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT); goto done; }
        int64_t service_start = ticks();
        // A bounded 30 ms worker stall every 100 services deliberately exceeds the declared period.
        if (p->inject && p->stats.callbacks > 0 && p->stats.callbacks % 100 == 0) {
            int64_t until = service_start + frequency.QuadPart * 30 / 1000;
            while (ticks() < until) { YieldProcessor(); }
        }
        UINT32 padding;
        CHECK(IAudioClient_GetCurrentPadding(client, &padding));
        UINT64 position, qpc;
        CHECK(IAudioClock_GetPosition(clock, &position, &qpc));
        uint64_t device_frame = (uint64_t)((double)position * format->nSamplesPerSec / clock_frequency);
        if (device_frame < last_device_frame) ++p->stats.clock_regressions;
        last_device_frame = device_frame;
        if (!padding) ++p->stats.padding_empty;
        int64_t now = ticks();
        if (!anchor_qpc) { anchor_qpc = now; anchor_clock = p->engine.clock; anchor_padding = padding; }
        int64_t wall_target = (int64_t)anchor_clock + (now - anchor_qpc) * format->nSamplesPerSec / frequency.QuadPart
            + (int64_t)padding - anchor_padding;
        int64_t deficit = wall_target - (int64_t)p->engine.clock;
        if (deficit > p->stats.maximum_wall_deficit) p->stats.maximum_wall_deficit = deficit;
        // On this endpoint IAudioClock pauses through starvation. A QPC anchor supplies elapsed
        // transport time only when padding exhausts. No callback catch-up loop or stale note replay.
        if (!padding && deficit > 0) {
            begin_block(p); skip_frames(p, (uint64_t)deficit); ++p->stats.recoveries;
        }
        // Retain genuinely advancing device-clock recovery as well; do not treat its units as frames.
        if (device_frame > p->engine.clock) { begin_block(p); skip_frames(p, device_frame - p->engine.clock); }
        p->stats.final_wall_deficit = wall_target - (int64_t)p->engine.clock;
        if (p->stats.final_wall_deficit > p->stats.maximum_post_recovery_deficit)
            p->stats.maximum_post_recovery_deficit = p->stats.final_wall_deficit;
        UINT32 frames = capacity - padding;
        if (!frames) continue;
        CHECK(IAudioRenderClient_GetBuffer(output, frames, &buffer));
        int64_t processing_start = ticks();
        begin_block(p); p->processor(&p->engine, (float *)buffer, frames, format->nChannels);
        end_block(p);
        int64_t processing_end = ticks();
        CHECK(IAudioRenderClient_ReleaseBuffer(output, frames, 0));
        int64_t end = ticks();
        int i = p->stats.callbacks++;
        if (i < RECORDS) p->timings[i] = (Timing){
            (processing_end - processing_start) * scale, (end - service_start) * scale,
            (service_start - previous) * scale, (int32_t)frames, (int32_t)padding, p->engine.clock, device_frame};
        else ++p->stats.timing_full;
        previous = service_start;
        p->stats.device_frame = device_frame; p->stats.submitted_frame = p->engine.clock;
    }
done:
    if (first) p->stats.elapsed_seconds = (ticks() - first) / (double)frequency.QuadPart;
    p->stats.hr = hr;
    if (started) {
        HRESULT stop_hr = IAudioClient_Stop(client);
        if (FAILED(stop_hr) && SUCCEEDED(hr)) p->stats.hr = stop_hr;
    }
    clear_voices(&p->engine); p->engine.playing = 0;
    if (priority) AvRevertMmThreadCharacteristics(priority);
    if (clock) IAudioClock_Release(clock);
    if (output) IAudioRenderClient_Release(output);
    if (client) IAudioClient_Release(client);
    if (device) IMMDevice_Release(device);
    if (enumerator) IMMDeviceEnumerator_Release(enumerator);
    if (format) CoTaskMemFree(format);
    if (endpoint) CoTaskMemFree(endpoint);
    if (event) CloseHandle(event);
    if (initialized) CoUninitialize();
    if (!ready) { atomic_store(&p->initialization_result, p->stats.hr); SetEvent(p->ready); }
    return 0;
}
