// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using Seqvium.VisualCheck;
using Xunit;

namespace Seqvium.Tests;

public sealed class ArtifactRetentionTests : IDisposable
{
    private readonly string _parent = Path.Combine(Path.GetTempPath(), "seqvium-retention-" + Guid.NewGuid().ToString("N"));
    private string Root => Path.Combine(_parent, ".artifacts");
    private readonly DateTimeOffset _now = DateTimeOffset.UtcNow;
    public ArtifactRetentionTests() => Directory.CreateDirectory(Root);
    private string Run(string id, int days, string status = "success", string scenario = "matrix", string kind = "run", string? preserve = null)
    {
        string path = Path.Combine(Root, id); Directory.CreateDirectory(path);
        File.WriteAllBytes(Path.Combine(path, "fixture.bin"), new byte[1024]);
        var time = _now.AddDays(-days);
        ArtifactRetention.Write(path, new("Seqvium.Artifacts.v1", "VisualCheck", scenario, id, time, time, status, 0, 0,
            kind, preserve, kind == "disposable" ? "Reviewed generated synthetic fixture" : null));
        return path;
    }
    [Fact]
    public void AgeLimitsReserveLatestSuccessAndFailureAndDryRunChangesNothing()
    {
        string s1 = Run("success-old", 8), s2 = Run("success-last", 1), f1 = Run("failure-old", 15, "failure"), f2 = Run("failure-last", 2, "failure");
        var files = Directory.GetFiles(Root, "*", SearchOption.AllDirectories).ToDictionary(p => p, File.ReadAllBytes);
        var report = ArtifactRetention.Apply(Root, false, now: _now);
        Assert.Equal(new[] { f1, s1 }.Order(), report.Plan.Select(r => r.Path).Order());
        Assert.Empty(report.Deleted);
        Assert.Equal(files.Keys.Order(), Directory.GetFiles(Root, "*", SearchOption.AllDirectories).Order());
        foreach (var file in files) Assert.Equal(file.Value, File.ReadAllBytes(file.Key));
        Assert.True(Directory.Exists(s2) && Directory.Exists(f2));
    }
    [Fact]
    public void ReservationsConsumeCountSlotsAndFamiliesAreIndependent()
    {
        var paths = Enumerable.Range(0, 5).Select(i => Run("s" + i, i)).ToArray();
        string failure = Run("f", 40, "failure"), other = Run("other", 50, scenario: "catalog");
        var report = ArtifactRetention.Apply(Root, false, now: _now);
        Assert.Equal(paths.Skip(2).Order(), report.Plan.Select(r => r.Path).Order());
        Assert.DoesNotContain(report.Plan, r => r.Path == failure || r.Path == other);
    }
    [Fact]
    public void BudgetDeletesOldestEligibleAndReportsProtectedExcess()
    {
        string a = Run("a", 2), b = Run("b", 1), latest = Run("latest", 0);
        var report = ArtifactRetention.Apply(Root, false, new(Budget: 1), _now);
        Assert.Equal(new[] { a, b }, report.Plan.Select(r => r.Path));
        Assert.True(report.OverBudget);
        Assert.DoesNotContain(report.Plan, r => r.Path == latest);
    }
    [Fact]
    public void ReviewedLegacyIsRemovedAndApplyIsIdempotent()
    {
        string disposable = Run("legacy-bin", 0, kind: "disposable"), latest = Run("latest", 0);
        string unknown = Path.Combine(Root, "unknown"); Directory.CreateDirectory(unknown); File.WriteAllText(Path.Combine(unknown, "notes.txt"), "keep");
        var first = ArtifactRetention.Apply(Root, true, now: _now);
        Assert.Single(first.Deleted); Assert.False(Directory.Exists(disposable)); Assert.True(Directory.Exists(latest));
        Assert.True(first.AfterBytes < first.BeforeBytes);
        Assert.Empty(ArtifactRetention.Apply(Root, true, now: _now).Deleted);
        Assert.Equal("keep", File.ReadAllText(Path.Combine(unknown, "notes.txt")));
    }
    [Fact]
    public void ActiveIncompleteAndExceptionalEvidenceRemainProtected()
    {
        string active = Run("active", 10, kind: "disposable");
        var m = System.Text.Json.JsonSerializer.Deserialize<ArtifactRetention.Manifest>(File.ReadAllText(Path.Combine(active, ArtifactRetention.Marker)))!;
        using var process = Process.GetCurrentProcess();
        ArtifactRetention.Write(active, m with { Pid = process.Id, ProcessStart = process.StartTime.ToUniversalTime().Ticks });
        string incomplete = Run("incomplete", 10, kind: "disposable");
        ArtifactRetention.Write(incomplete, m with { Completed = null, Status = "running", RunId = "incomplete" });
        Run("exceptional", 100, kind: "disposable", preserve: "Unique raw evidence");
        string locked = Run("locked-active", 10, kind: "disposable");
        using var lease = new FileStream(Path.Combine(locked, ".active"), FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Assert.Empty(ArtifactRetention.Apply(Root, true, now: _now).Plan);
    }
    [Fact]
    public void LinksAndExternalPathsNeverBecomeCandidates()
    {
        string external = Path.Combine(_parent, "outside"); Directory.CreateDirectory(external); File.WriteAllText(Path.Combine(external, "user.txt"), "user");
        string candidate = Run("linked-tree", 10, kind: "disposable");
        string link = Path.Combine(candidate, "outside"); Directory.CreateSymbolicLink(link, external);
        try
        {
            Assert.Empty(ArtifactRetention.Apply(Root, true, now: _now).Plan);
            Assert.Throws<IOException>(() => ArtifactRetention.Validate(Root, external));
            Assert.Throws<IOException>(() => ArtifactRetention.Apply(external, true));
            Assert.Throws<IOException>(() => ArtifactRetention.Validate(Root, link));
            Assert.Equal("user", File.ReadAllText(Path.Combine(external, "user.txt")));
        }
        finally { Directory.Delete(link); }
    }
    [Fact]
    public void IndividualDenialDoesNotStopOtherCandidatesAndPartialRunCanRetry()
    {
        string denied = Run("denied", 20, kind: "disposable"), good = Run("good", 19, kind: "disposable");
        using (var locked = new FileStream(Path.Combine(denied, "fixture.bin"), FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var report = ArtifactRetention.Apply(Root, true, now: _now);
            Assert.Contains(report.Diagnostics, d => d.StartsWith("Deletion failed:", StringComparison.Ordinal));
            Assert.True(File.Exists(Path.Combine(denied, ArtifactRetention.Marker)));
            Assert.False(Directory.Exists(good));
        }
        Assert.Single(ArtifactRetention.Apply(Root, true, now: _now).Deleted);
        Assert.Empty(ArtifactRetention.Apply(Root, true, now: _now).Deleted);
    }
    [Fact]
    public void InvalidManifestAndNestedOwnershipAreProtected()
    {
        string path = Run("bad", 10, kind: "disposable"); File.WriteAllText(Path.Combine(path, ArtifactRetention.Marker), "bad json");
        string outer = Run("outer", 10, kind: "disposable"), inner = Path.Combine(outer, "inner"); Directory.CreateDirectory(inner);
        var manifest = new ArtifactRetention.Manifest("Seqvium.Artifacts.v1", "VisualCheck", "inner", "inner", _now.AddDays(-10), _now.AddDays(-10), "success", 0, 0, "disposable", ReviewReason: "Synthetic");
        ArtifactRetention.Write(inner, manifest);
        Assert.Empty(ArtifactRetention.Apply(Root, true, now: _now).Plan);
    }
    [Fact]
    public async Task WatchdogPersistsDiagnosticWithoutGuiDispatcher()
    {
        var fired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var deadline = new VerificationDeadline(TimeSpan.FromMilliseconds(20), Root, () => fired.TrySetResult());
        await fired.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Contains("Watchdog timeout", File.ReadAllText(Path.Combine(Root, "failure.txt")));
    }
    [Fact]
    public void PermissionDenialIsReportedAndAnotherDirectoryStillDeletes()
    {
        string denied = Run("permission-denied", 20, kind: "disposable"), other = Run("permitted", 19, kind: "disposable");
        var report = ArtifactRetention.Apply(Root, true, now: _now, beforeDelete: path =>
        {
            if (path == denied) throw new UnauthorizedAccessException("Synthetic ACL denial");
        });
        Assert.True(Directory.Exists(denied)); Assert.False(Directory.Exists(other));
        Assert.Contains(report.Diagnostics, d => d.Contains("Synthetic ACL denial", StringComparison.Ordinal));
    }
    [Fact]
    public void NormalRunAutomaticallyRotatesCompletedOutputsAndMarksItsOwnLifecycle()
    {
        Enumerable.Range(0, 5).ToList().ForEach(i => Run("previous-" + i, i));
        using (var current = new ArtifactRetention.Run(Path.Combine(Root, "new"), "matrix"))
        {
            Assert.False(Directory.Exists(Path.Combine(Root, "previous-4")));
            var marker = Path.Combine(current.Directory, ArtifactRetention.Marker);
            Assert.Contains("running", File.ReadAllText(marker));
            Assert.DoesNotContain(ArtifactRetention.Apply(Root, true).Plan, r => r.Path == current.Directory);
            current.Complete(0);
            Assert.Contains("success", File.ReadAllText(marker));
        }
    }
    public void Dispose() => Directory.Delete(_parent, true);
}
