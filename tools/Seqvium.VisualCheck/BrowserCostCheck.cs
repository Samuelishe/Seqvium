// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    // One bounded observation per caller-owned source, no physical output or sustained pressure.
    private static async Task BrowserCostAsync(string output, string[] sources)
    {
        Directory.CreateDirectory(output);
        using var deadline = new VerificationDeadline(TimeSpan.FromSeconds(60), output, () => Environment.Exit(124));
        var results = new List<object>();
        foreach (string path in sources)
        {
            RealtimeSampler? sampler = null;
            await using var workflow = new DesktopWorkflow(ProjectDocument.Create(), () =>
            {
                sampler = new(48000, 2, 256);
                return new(new AuditBorrower(), sampler, "Cost-check borrower", () => null);
            }, Path.Combine(output, "media"));
            var source = new BrowserSource(Path.GetFileName(path), path);
            long allocated = GC.GetTotalAllocatedBytes();
            var clock = Stopwatch.StartNew();
            var info = await workflow.InspectBrowserSourceAsync(source, CancellationToken.None);
            double inspectMs = clock.Elapsed.TotalMilliseconds;
            long inspectedBytes = GC.GetTotalAllocatedBytes() - allocated;
            allocated = GC.GetTotalAllocatedBytes(); clock.Restart();
            Require(await workflow.PreviewBrowserSourceAsync(source), "Cost-check preview failed: " + workflow.State.Detail);
            double previewMs = clock.Elapsed.TotalMilliseconds;
            long previewBytes = GC.GetTotalAllocatedBytes() - allocated;
            var packet = new float[512]; sampler!.Process(packet);
            Require(sampler.ReadStatus().Playing && packet.Any(v => v != 0), "Cost-check preview has no rendered PCM.");
            await workflow.StopBrowserPreviewAsync(); sampler.Process(packet);
            using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(25));
            clock.Restart();
            bool cancelled = false;
            try { await workflow.InspectBrowserSourceAsync(source, cancellation.Token); }
            catch (OperationCanceledException) { cancelled = true; }
            using var process = Process.GetCurrentProcess();
            results.Add(new { Source = Path.GetFileName(path), info.Rate, info.Channels, info.Frames,
                InspectMs = inspectMs, PreviewMs = previewMs, InspectAllocatedBytes = inspectedBytes,
                PreviewAllocatedBytes = previewBytes, CancelRequestedAfterMs = 25, Cancelled = cancelled,
                CancelledOperationMs = clock.Elapsed.TotalMilliseconds, process.PeakWorkingSet64 });
            Require(workflow.State.UndoCount == 0 && !workflow.State.IsDirty, "Cost check edited the project.");
        }
        Json(output, "browser-cost", new { PhysicalAudio = false, UiResponsivenessMeasured = false,
            AllocationScope = "process cumulative managed allocations per operation, including owner/poll/JIT work",
            Results = results });
    }
}
