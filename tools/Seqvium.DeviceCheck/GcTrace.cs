// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Diagnostics.Tracing;

/// <summary>Opt-in harness-only native runtime events. Delivery occurs on the runtime listener thread,
/// not in PCM processing. Original event timestamps are mapped using bracketed UTC/QPC anchors;
/// receipt timestamps must never be substituted for runtime suspension timestamps.</summary>
internal sealed class GcTrace : EventListener
{
    internal readonly record struct Anchor(long Before, long UtcTicks, long After)
    {
        internal static Anchor Capture()
        {
            long before = Stopwatch.GetTimestamp(), utc = DateTime.UtcNow.Ticks, after = Stopwatch.GetTimestamp();
            return new(before, utc, after);
        }
        internal long Map(long utc) => (Before + After) / 2 +
            (long)((utc - UtcTicks) * (double)Stopwatch.Frequency / TimeSpan.TicksPerSecond);
    }
    internal readonly record struct Observation(int Id, long UtcTicks, long Qpc, long Received,
        long Thread, uint Count, uint Depth, uint Reason, uint Type);
    private readonly Observation[] _events = new Observation[20000];
    private readonly Anchor _anchor = Anchor.Capture();
    private bool _enabled;
    private int _count, _dropped;

    internal GcTrace()
    {
        _enabled = true;
        foreach (var source in EventSource.GetSources()) Enable(source);
    }
    protected override void OnEventSourceCreated(EventSource source) { if (_enabled) Enable(source); }
    private void Enable(EventSource source)
    {
        if (source.Name == "Microsoft-Windows-DotNETRuntime")
            EnableEvents(source, EventLevel.Informational, (EventKeywords)1);
    }
    protected override void OnEventWritten(EventWrittenEventArgs data)
    {
        if (data.EventId is not (1 or 2 or 3 or 7 or 8 or 9)) return;
        int index = _count;
        if (index == _events.Length) { _dropped++; return; }
        uint Field(string name)
        {
            if (data.PayloadNames is null || data.Payload is null) return 0;
            int field = data.PayloadNames.IndexOf(name);
            return field < 0 ? 0 : Convert.ToUInt32(data.Payload[field], System.Globalization.CultureInfo.InvariantCulture);
        }
        long utc = data.TimeStamp.ToUniversalTime().Ticks;
        _events[index] = new(data.EventId, utc, _anchor.Map(utc), Stopwatch.GetTimestamp(), data.OSThreadId,
            Field("Count"), Field("Depth"), Field("Reason"), Field("Type"));
        Volatile.Write(ref _count, index + 1);
    }
    internal object Snapshot(long begin, long end) => new
    {
        InitialAnchor = _anchor, FinalAnchor = Anchor.Capture(), Dropped = _dropped,
        Events = _events.Take(Volatile.Read(ref _count)).Where(item => item.Qpc >= begin && item.Qpc <= end).ToArray()
    };
}
