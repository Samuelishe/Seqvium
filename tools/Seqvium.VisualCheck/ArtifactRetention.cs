// SPDX-License-Identifier: Apache-2.0
using System.Diagnostics;
using System.Text.Json;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Seqvium.Tests")]

namespace Seqvium.VisualCheck;

// Developer evidence only. Unknown trees are measured but never adopted by age/name alone.
internal static class ArtifactRetention
{
    internal const string Marker = ".seqvium-run.json";
    internal sealed record Policy(int SuccessDays = 7, int FailureDays = 14, int MaxRuns = 3, long Budget = 1L << 30);
    internal sealed record Manifest(string Owner, string Producer, string Scenario, string RunId,
        DateTimeOffset Created, DateTimeOffset? Completed, string Status, int Pid, long ProcessStart,
        string Kind = "run", string? PreserveReason = null, string? ReviewReason = null);
    internal sealed record Candidate(string Path, long Bytes, string Reason, Manifest Manifest);
    internal sealed record Report(long BeforeBytes, long AfterBytes, List<Candidate> Plan,
        List<Candidate> Deleted, List<string> Diagnostics, bool OverBudget);
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly StringComparison Comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    internal static string RootFor(string path)
    {
        for (var directory = new DirectoryInfo(Path.GetFullPath(path)); directory is not null; directory = directory.Parent)
            if (directory.Name == ".artifacts") return directory.FullName;
        throw new ArgumentException("Verification output must be inside .artifacts.");
    }

    internal static void Validate(string root, string path)
    {
        root = Path.GetFullPath(root); path = Path.GetFullPath(path);
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, Comparison))
            throw new IOException("Target is outside artifact root: " + path);
        for (var entry = new DirectoryInfo(path); entry is not null; entry = entry.Parent)
            if (entry.LinkTarget is not null || (entry.Exists && (entry.Attributes & FileAttributes.ReparsePoint) != 0)) throw new IOException("Reparse ancestor: " + entry.FullName);
    }

    private static long Measure(string path, List<string>? markers = null)
    {
        long bytes = 0;
        foreach (var entry in new DirectoryInfo(path).EnumerateFileSystemInfos().OrderBy(e => e.Name, StringComparer.Ordinal))
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse entry: " + entry.FullName);
            if (entry is DirectoryInfo directory) bytes += Measure(directory.FullName, markers);
            else { bytes += ((FileInfo)entry).Length; if (entry.Name == Marker) markers?.Add(entry.FullName); }
        }
        return bytes;
    }

    internal static void Write(string directory, Manifest manifest)
    {
        string root = RootFor(directory); Validate(root, directory);
        string path = Path.Combine(directory, Marker);
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse marker.");
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            JsonSerializer.Serialize(stream, manifest, JsonOptions);
        File.Move(temporary, path, true);
    }

    internal sealed class Run : IDisposable
    {
        private readonly FileStream _lease;
        private Manifest _manifest;
        internal string Directory { get; }
        internal Run(string parent, string scenario)
        {
            string root = RootFor(parent);
            Validate(root, parent); System.IO.Directory.CreateDirectory(parent);
            Apply(root, true);
            string id = DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + Guid.NewGuid().ToString("N");
            Directory = Path.Combine(parent, id); System.IO.Directory.CreateDirectory(Directory);
            _lease = new FileStream(Path.Combine(Directory, ".active"), FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
            using var process = Process.GetCurrentProcess();
            _manifest = new("Seqvium.Artifacts.v1", "VisualCheck", scenario, id, DateTimeOffset.UtcNow, null,
                "running", process.Id, process.StartTime.ToUniversalTime().Ticks);
            Write(Directory, _manifest);
        }
        internal void Complete(int exit) => Write(Directory, _manifest = _manifest with
            { Status = exit == 0 ? "success" : "failure", Completed = DateTimeOffset.UtcNow });
        public void Dispose() { _lease.Dispose(); Apply(RootFor(Directory), true); }
    }

    private static bool Alive(Manifest manifest)
    {
        if (manifest.Pid <= 0) return false;
        try { using var process = Process.GetProcessById(manifest.Pid); return !process.HasExited && process.StartTime.ToUniversalTime().Ticks == manifest.ProcessStart; }
        catch (ArgumentException) { return false; }
        catch (InvalidOperationException) { return false; }
        catch (System.ComponentModel.Win32Exception) { return true; }
    }

    // One bounded snapshot per invocation: hosted DLLs and command-line output roots count too.
    private static List<string>? ActiveReferences()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
                foreach (string arg in new[] { "-NoProfile", "-NonInteractive", "-Command", "Get-CimInstance Win32_Process -ErrorAction Stop | Select-Object ExecutablePath,CommandLine | ConvertTo-Json -Compress" }) start.ArgumentList.Add(arg);
                using var process = Process.Start(start)!;
                var output = process.StandardOutput.ReadToEndAsync(); var errors = process.StandardError.ReadToEndAsync();
                if (!process.WaitForExit(5000)) { process.Kill(true); process.WaitForExit(1000); return null; }
                if (process.ExitCode != 0) return null;
                using var json = JsonDocument.Parse(output.GetAwaiter().GetResult());
                return json.RootElement.EnumerateArray().SelectMany(p => p.EnumerateObject().Select(v => v.Value.ValueKind == JsonValueKind.String ? v.Value.GetString()! : "")).ToList();
            }
            var paths = new List<string>();
            if (!OperatingSystem.IsLinux()) return null; // Unknown platform: fail closed for deletion.
            foreach (string directory in System.IO.Directory.EnumerateDirectories("/proc"))
            {
                if (!int.TryParse(Path.GetFileName(directory), out _)) continue;
                try
                {
                    paths.Add(File.ReadAllText(Path.Combine(directory, "cmdline")).Replace('\0', ' '));
                    paths.Add(new DirectoryInfo(Path.Combine(directory, "cwd")).LinkTarget ?? "");
                    paths.Add(new FileInfo(Path.Combine(directory, "exe")).LinkTarget ?? "");
                }
                catch (Exception error) when (error is IOException or UnauthorizedAccessException) { }
            }
            return paths;
        }
        catch (Exception error) when (error is IOException or System.ComponentModel.Win32Exception or JsonException or InvalidOperationException) { return null; }
    }

    private static bool InUse(string path, List<string>? references) => references is null || references.Any(r => r.Contains(path, Comparison));

    private static bool LeaseBusy(string path)
    {
        string lease = Path.Combine(path, ".active");
        if (!File.Exists(lease)) return false;
        try { using var stream = new FileStream(lease, FileMode.Open, FileAccess.ReadWrite, FileShare.None); return false; }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return true; }
    }

    internal static Report Apply(string root, bool apply, Policy? policy = null, DateTimeOffset? now = null,
        Action<string>? beforeDelete = null)
    {
        policy ??= new(); var clock = now ?? DateTimeOffset.UtcNow;
        root = Path.GetFullPath(root);
        if (Path.GetFileName(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)) != ".artifacts")
            throw new IOException("Retention root must be an explicitly named .artifacts directory.");
        if (!System.IO.Directory.Exists(root)) return new(0, 0, [], [], [], false);
        Validate(root, Path.Combine(root, "retention-boundary"));
        foreach (string name in new[] { ".retention.lock", "retention.jsonl", "retention.previous.jsonl" })
            if (new FileInfo(Path.Combine(root, name)).LinkTarget is not null) throw new IOException("Reparse retention metadata: " + name);
        using var gate = apply ? new FileStream(Path.Combine(root, ".retention.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None) : null;
        var diagnostics = new List<string>(); var markers = new List<string>(); long before = 0;
        // A reparse/inaccessible subtree is retained; it cannot hide a deletion candidate.
        foreach (var entry in new DirectoryInfo(root).EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) { diagnostics.Add("Protected reparse: " + entry.FullName); continue; }
            if (entry is not DirectoryInfo dir) { before += ((FileInfo)entry).Length; continue; }
            try { var found = new List<string>(); before += Measure(dir.FullName, found); markers.AddRange(found); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { diagnostics.Add("Protected unreadable tree: " + dir.FullName + ": " + error.Message); }
        }
        var runs = new List<Candidate>();
        var activeReferences = markers.Count == 0 ? [] : ActiveReferences();
        if (activeReferences is null) diagnostics.Add("Process ownership snapshot unavailable; all runs protected.");
        foreach (var marker in markers)
        {
            try
            {
                string path = Path.GetDirectoryName(marker)!; Validate(root, path);
                var m = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(marker)) ?? throw new IOException("Empty manifest.");
                if (m.Owner != "Seqvium.Artifacts.v1" || string.IsNullOrWhiteSpace(m.Scenario) || string.IsNullOrWhiteSpace(m.Producer) ||
                    m.RunId != Path.GetFileName(path) || m.Pid < 0 || m.Kind is not ("run" or "disposable") || m.Completed is null ||
                    m.Status is not ("success" or "failure") || m.Completed < m.Created || m.Completed > clock || m.PreserveReason is not null || Alive(m) ||
                    (m.Kind == "disposable" && string.IsNullOrWhiteSpace(m.ReviewReason)) || LeaseBusy(path) || InUse(path, activeReferences))
                { diagnostics.Add("Protected manifest: " + path); continue; }
                if (markers.Any(other => other != marker && (Path.GetDirectoryName(other)!.StartsWith(path + Path.DirectorySeparatorChar, Comparison) ||
                    path.StartsWith(Path.GetDirectoryName(other)! + Path.DirectorySeparatorChar, Comparison))))
                { diagnostics.Add("Protected nested ownership: " + path); continue; }
                runs.Add(new(path, Measure(path), "", m));
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
            { diagnostics.Add("Protected invalid manifest: " + marker + ": " + error.Message); }
        }
        var reserved = new HashSet<string>();
        foreach (var family in runs.Where(r => r.Manifest.Kind == "run").GroupBy(r => (r.Manifest.Producer, r.Manifest.Scenario)))
            foreach (var status in new[] { "success", "failure" })
            {
                var last = family.Where(r => r.Manifest.Status == status).OrderByDescending(r => r.Manifest.Completed).ThenBy(r => r.Path, StringComparer.Ordinal).FirstOrDefault();
                if (last is not null) reserved.Add(last.Path);
            }
        var plan = new Dictionary<string, Candidate>();
        foreach (var family in runs.GroupBy(r => (r.Manifest.Producer, r.Manifest.Scenario)))
        {
            var ordered = family.OrderByDescending(r => r.Manifest.Completed).ThenBy(r => r.Path, StringComparer.Ordinal).ToList();
            // Reservations consume slots; fill the remaining slots with newest runs.
            var keep = ordered.Where(r => reserved.Contains(r.Path)).Select(r => r.Path).ToHashSet();
            foreach (var r in ordered.Where(r => !keep.Contains(r.Path)).Take(Math.Max(0, policy.MaxRuns - keep.Count))) keep.Add(r.Path);
            foreach (var r in ordered.Where(r => !reserved.Contains(r.Path)))
            {
                string? reason = r.Manifest.Kind == "disposable" ? "reviewed legacy disposable" :
                    clock - r.Manifest.Completed > TimeSpan.FromDays(r.Manifest.Status == "success" ? policy.SuccessDays : policy.FailureDays) ? "age" :
                    !keep.Contains(r.Path) ? "count" : null;
                if (reason is not null) plan[r.Path] = r with { Reason = reason };
            }
        }
        long projected = before - plan.Values.Sum(r => r.Bytes);
        foreach (var r in runs.Where(r => !reserved.Contains(r.Path) && !plan.ContainsKey(r.Path)).OrderBy(r => r.Manifest.Completed).ThenBy(r => r.Path, StringComparer.Ordinal))
        {
            if (projected <= policy.Budget) break;
            plan[r.Path] = r with { Reason = "budget" }; projected -= r.Bytes;
        }
        var deleted = new List<Candidate>();
        var orderedPlan = plan.Values.OrderBy(r => r.Manifest.Completed).ThenBy(r => r.Path, StringComparer.Ordinal).ToList();
        foreach (var r in orderedPlan)
        {
            Console.WriteLine($"retention {(apply ? "apply" : "dry-run")}: {r.Path} | {r.Bytes} bytes | {r.Reason}");
            if (!apply) continue;
            try
            {
                Validate(root, r.Path); Measure(r.Path);
                if (JsonSerializer.Deserialize<Manifest>(File.ReadAllText(Path.Combine(r.Path, Marker))) != r.Manifest || Alive(r.Manifest) || InUse(r.Path, activeReferences))
                    throw new IOException("Ownership changed or process is using target.");
                string leasePath = Path.Combine(r.Path, ".active");
                using (var lease = new FileStream(leasePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 1, FileOptions.DeleteOnClose))
                {
                    // Delete files individually without following any link. Leave the marker until last,
                    // so partial failures remain reviewable and retryable on the next invocation.
                    beforeDelete?.Invoke(r.Path);
                    DeleteContents(r.Path, leasePath);
                }
                File.Delete(Path.Combine(r.Path, Marker)); System.IO.Directory.Delete(r.Path);
                deleted.Add(r);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            { diagnostics.Add("Deletion failed: " + r.Path + ": " + error.Message); }
        }
        long after = 0;
        foreach (var entry in new DirectoryInfo(root).EnumerateFileSystemInfos())
            try { if ((entry.Attributes & FileAttributes.ReparsePoint) == 0) after += entry is DirectoryInfo d ? Measure(d.FullName) : ((FileInfo)entry).Length; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { diagnostics.Add("Unmeasured retained tree: " + entry.FullName); }
        bool over = (apply ? after : projected) > policy.Budget;
        if (over) diagnostics.Add("Soft budget exceeded by protected/unknown evidence; retained for review.");
        var report = new Report(before, after, orderedPlan, deleted, diagnostics, over);
        foreach (var diagnostic in diagnostics) Console.Error.WriteLine("retention: " + diagnostic);
        if (apply)
        {
            string log = Path.Combine(root, "retention.jsonl");
            if (File.Exists(log) && new FileInfo(log).Length > 256 * 1024) File.Move(log, Path.Combine(root, "retention.previous.jsonl"), true);
            File.AppendAllText(log, JsonSerializer.Serialize(new { Time = clock, report }) + Environment.NewLine);
        }
        return report;
    }

    private static void DeleteContents(string directory, string lease)
    {
        foreach (var entry in new DirectoryInfo(directory).EnumerateFileSystemInfos())
        {
            if ((entry.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("Reparse appeared during deletion.");
            if (entry.FullName == lease || entry.Name == Marker) continue;
            if (entry is DirectoryInfo child) { DeleteContents(child.FullName, lease); System.IO.Directory.Delete(child.FullName); }
            else
            {
                entry.Attributes &= ~FileAttributes.ReadOnly;
                using (var probe = new FileStream(entry.FullName, FileMode.Open, FileAccess.Read, FileShare.None)) { }
                File.Delete(entry.FullName);
            }
        }
    }
}
