// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task PianoMusicAsync(MainWindow main, string output, string library, string? deliveryRoot)
    {
        Console.WriteLine(
            "Audible Piano check: native authoring and physical Pattern/Song output; system volume is unchanged.");
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        using var catalog = JsonDocument.Parse(await File.ReadAllBytesAsync(Path.Combine(library, "catalog.json")));

        string Find(int root, string dynamic) => Path.Combine(library, catalog.RootElement.EnumerateArray().First(e =>
                e.GetProperty("category").GetString() == "Piano/Single-Notes" &&
                e.GetProperty("root_midi").ValueKind == JsonValueKind.Number &&
                e.GetProperty("root_midi").GetInt32() == root &&
                e.GetProperty("source_path").GetString()!.Contains(dynamic, StringComparison.OrdinalIgnoreCase))
            .GetProperty("file").GetString()!);

        string[] sources = [Find(57, "dyn1"), Find(45, "dyn3")];
        var hashes = sources.Select(p => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p)))).ToArray();
        main.WindowState = WindowState.Maximized;
        await Task.Delay(250);
        main.Activate();
        workspace.Open(WorkspaceState.BeatId);
        Require(
            await workflow.CreateBeatAsync(workflow.State.Document.LifecycleId, workflow.State.Generation,
                "Jade Nocturne"), "Music creation failed.");
        foreach (string source in sources)
            Require(await workflow.AddBeatWavAsync(BeatSteps.Resolve(workflow.State).Basis!, source),
                "Library import failed: " + workflow.State.Detail);
        Require(
            workflow.State.Snapshot.State.Sounds.Select(s => s.Parameters["rootPitch"])
                .SequenceEqual(new decimal[] { 57, 45 }), "Library roots failed.");
        Require(await workflow.ResizeBeatAsync(BeatSteps.Resolve(workflow.State).Basis!, 4), "Music length failed.");
        var imported = BeatSteps.Resolve(workflow.State).Pattern!;
        Require(
            await workflow.ConfigurePianoSourceAsync(BeatSteps.Resolve(workflow.State).Basis!, imported.Parts[0].Id,
                "Upright · melody", 57), "Melody naming failed.");
        Require(
            await workflow.ConfigurePianoSourceAsync(BeatSteps.Resolve(workflow.State).Basis!, imported.Parts[1].Id,
                "Low piano", 45), "Bass naming failed.");
        workspace.Open(WorkspaceState.PianoId);
        workspace.State.SetBounds(WorkspaceState.PianoId, new(0, 0, workspace.State.Width, workspace.State.Height));
        await Task.Delay(150);
        main.UpdateLayout();
        var pane = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        pane.Refresh();
        pane.Canvas.Fit();
        Require(await NativePointer.ComposePianoAsync(main, pane, workflow, output),
            "Native authoring refused by foreground guard; partial evidence retained.");
        Require(
            BeatSteps.Resolve(workflow.State).Pattern!.Parts.Select(p => p.Notes.Length).SequenceEqual(new[] { 19, 4 }),
            "Composition lost channels.");
        await workflow.StopAsync();
        Require(
            await workflow.AddArrangementClipAsync(ArrangementEditBasis.From(workflow.State),
                workflow.State.Selected!.Value, default, 0, 2), "Playlist clip failed.");
        string project = Path.Combine(output, "Jade Nocturne.seqvium");
        Require(await workflow.SaveAsync(project), "Save failed.");
        var saved = workflow.State.Snapshot;
        Require(await workflow.OpenAsync(project, UnsavedDecision.Discard), "Reopen failed.");
        Require(workflow.State.Snapshot.Revision == saved.Revision && workflow.State.Execution is null,
            "Reopen changed identity or started audio.");
        pane.Refresh();
        pane.OpenPart(BeatSteps.Resolve(workflow.State).Pattern!.Parts[0].Id);
        pane.Canvas.Fit();
        await CaptureAsync(main, output, "music-dark");
        var beforeAudition = workflow.State.Snapshot;
        Require(
            await workflow.AuditionNoteAsync(BeatSteps.Resolve(workflow.State).Basis!, pane.Canvas.Part!.Id, 69, .65m),
            "Keyboard audition failed.");
        await Task.Delay(500);
        Require(workflow.State.Snapshot == beforeAudition, "Audition entered history.");
        await workflow.StopAsync();
        Require(await workflow.SetSongModeAsync(false) && await workflow.PlayAsync(),
            "Pattern Play failed: " + workflow.State.Detail);
        await Task.Delay(1200);
        await workflow.ObserveAsync();
        Require(workflow.State.Execution?.Execution is { Playing: true, Looping: true, Voices: > 0 },
            "Pattern did not render notes.");
        var patternExecution = workflow.State.Execution;
        await CaptureAsync(main, output, "pattern-playing");
        await workflow.StopAsync();
        Require(await workflow.SetSongModeAsync(true) && await workflow.PlayAsync(),
            "Song Play failed: " + workflow.State.Detail);
        await Task.Delay(100);
        await workflow.ObserveAsync();
        long epoch = workflow.State.Execution!.Execution.Epoch;
        var melody = BeatSteps.Resolve(workflow.State).Pattern!.Parts[0];
        var changed = melody.Notes.Select(n => n.Position.Ticks >= 12000000 ? n with { Intensity = .55m } : n)
            .ToArray();
        Require(await workflow.EditPianoNotesAsync(BeatSteps.Resolve(workflow.State).Basis!, melody.Id, changed),
            "Live edit failed.");
        for (int wait = 0;
             wait < 100 && workflow.State.Execution?.Execution.EquivalentCanonicalRevision !=
             workflow.State.Snapshot.Revision;
             wait++)
        {
            await Task.Delay(20);
            await workflow.ObserveAsync();
        }

        Require(
            workflow.State.Execution?.Execution is { Playing: true } &&
            workflow.State.Execution.Execution.Epoch == epoch, "Live edit restarted Song.");
        Require(await workflow.UndoAsync(), "Live Undo failed.");
        for (int wait = 0;
             wait < 100 && workflow.State.Execution?.Execution.EquivalentCanonicalRevision != saved.Revision;
             wait++)
        {
            await Task.Delay(20);
            await workflow.ObserveAsync();
        }

        Require(
            workflow.State.Execution?.Execution is { Playing: true } &&
            workflow.State.Execution.Execution.Epoch == epoch, "Live Undo restarted Song.");
        int peakVoices = 0;
        for (int wait = 0;
             wait < 550 && workflow.State.Execution?.Execution.Transport != SamplerTransportState.Ended;
             wait++)
        {
            await Task.Delay(40);
            await workflow.ObserveAsync();
            peakVoices = Math.Max(peakVoices, workflow.State.Execution?.Execution.Voices ?? 0);
            Require(workflow.State.StatusKey != "Workflow.DeviceFault" && workflow.State.Execution?.Blocker is null,
                "Song blocked: " + workflow.State.Detail);
        }

        Require(workflow.State.Execution?.Execution.Transport == SamplerTransportState.Ended && peakVoices >= 4,
            "Song did not finish chords plus bass.");
        var songExecution = workflow.State.Execution;
        await workflow.StopAsync();
        Require(workflow.State.Snapshot.Revision == saved.Revision, "Live Undo did not restore saved music.");
        Require(
            await workflow.ExportWaveAsync(BeatSteps.Resolve(workflow.State).Basis!,
                Path.Combine(output, "Jade Nocturne - Pattern.wav")), "Pattern export failed.");
        Require(
            await workflow.ExportSongAsync(ArrangementEditBasis.From(workflow.State),
                Path.Combine(output, "Jade Nocturne - Song.wav")), "Song export failed.");
        using var rendered = WavDecoder.Decode(File.ReadAllBytes(Path.Combine(output, "Jade Nocturne - Song.wav")));
        Require(rendered.SampleRate == 48000 && rendered.Channels == 2 && rendered.Frames == 768000,
            "Export is not eight 120-BPM bars.");
        Require(
            hashes.SequenceEqual(sources.Select(p => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(p))))),
            "Library changed.");
        if (shell.Preferences.Theme != HostTheme.Light) await shell.ToggleThemeAsync();
        pane.Refresh();
        pane.Canvas.Fit();
        await CaptureAsync(main, output, "music-light");
        workspace.Open(WorkspaceState.BeatId);
        var rack = main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        rack.Refresh();
        Require(
            rack.GetVisualDescendants().OfType<PartNoteThumbnail>().Count() == 2 &&
            workflow.State.Snapshot.Revision == saved.Revision, "Rack changed melody.");
        await CaptureAsync(main, output, "music-rack");
        workspace.Open(WorkspaceState.ArrangementId);
        workspace.State.SetBounds(WorkspaceState.ArrangementId,
            new(0, 0, workspace.State.Width, workspace.State.Height));
        await CaptureAsync(main, output, "music-playlist");
        double peak = 0;
        int saturated = 0;
        for (int frame = 0; frame < rendered.Frames; frame++)
        for (int channel = 0; channel < rendered.Channels; channel++)
        {
            float value = rendered.Sample(frame, channel);
            peak = Math.Max(peak, Math.Abs(value));
            if (value <= -1 || value >= 32767f / 32768) saturated++;
        }

        Require(peak > 0 && saturated == 0, "Authored Song export is silent or saturated.");
        string? delivered = null;
        if (deliveryRoot is not null)
        {
            delivered = Path.Combine(Path.GetFullPath(deliveryRoot),
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N")[..8]);
            Directory.CreateDirectory(delivered);
            Require(await workflow.SaveAsync(Path.Combine(delivered, "Jade Nocturne.seqvium")),
                "Deliverable Save As failed.");
            Require(
                await workflow.ExportWaveAsync(BeatSteps.Resolve(workflow.State).Basis!,
                    Path.Combine(delivered, "Jade Nocturne - Pattern.wav")), "Deliverable Pattern export failed.");
            Require(
                await workflow.ExportSongAsync(ArrangementEditBasis.From(workflow.State),
                    Path.Combine(delivered, "Jade Nocturne - Song.wav")), "Deliverable Song export failed.");
            foreach (string picture in new[] { "music-dark", "music-light", "music-rack", "music-playlist" })
                File.Copy(Path.Combine(output, picture + ".png"), Path.Combine(delivered, picture + ".png"));
            Console.WriteLine("Preserved music and review: " + delivered);
        }

        Json(output, "piano-music", new
        {
            Result = "passed", NativeInput = true, PhysicalAudio = true, AcousticJudgement = false,
            Roots = new[] { 57, 45 }, Notes = new[] { 19, 4 }, LibraryHashesPreserved = true,
            Pattern = patternExecution,
            Song = songExecution, PeakVoices = peakVoices, ExportFrames = rendered.Frames, Peak = peak,
            SaturatedSamples = saturated,
            OutputSampleRate = workflow.State.ExecutionSampleRate, Project = project, Delivered = delivered
        });
    }
}
