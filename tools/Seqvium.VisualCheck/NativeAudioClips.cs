// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.VisualTree;
using NAudio.MediaFoundation;
using NAudio.Wave;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static void CreateAudioClipFixtures(string directory)
    {
        Require(!Directory.Exists(directory), "Audio fixture destination must be new and independent.");
        Directory.CreateDirectory(directory);
        string original = Path.GetFullPath("examples/Neon Orchard/Neon Orchard.seqvium");
        string project = Path.Combine(directory, "Neon Orchard - Audio Clips.seqvium");
        File.Copy(original, project);
        foreach (var file in Directory.EnumerateFiles(original + ".media", "*", SearchOption.AllDirectories))
        {
            string destination = Path.Combine(project + ".media", Path.GetRelativePath(original + ".media", file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }

        string sources = Path.Combine(directory, "Sources");
        Directory.CreateDirectory(sources);

        void Wave(string name, int seconds)
        {
            using var stream = new FileStream(Path.Combine(sources, name), FileMode.CreateNew, FileAccess.Write);
            using var writer = new BinaryWriter(stream, Encoding.ASCII);
            int bytes = seconds * 48000 * 4;
            writer.Write("RIFF"u8);
            writer.Write(bytes + 36);
            writer.Write("WAVEfmt "u8);
            writer.Write(16);
            writer.Write((ushort)1);
            writer.Write((ushort)2);
            writer.Write(48000);
            writer.Write(192000);
            writer.Write((ushort)4);
            writer.Write((ushort)16);
            writer.Write("data"u8);
            writer.Write(bytes);
            // Original quiet evolving pad with periodic accents. This is source material, not a project edit.
            for (int i = 0; i < seconds * 48000; i++)
            {
                double time = i / 48000d, envelope = Math.Min(1, time / 2) * Math.Min(1, (seconds - time) / 2);
                double phase = time * 2 * Math.PI;
                double accent = Math.Exp(-(time % (60d / 112 * 4)) * 12);
                writer.Write((short)(32768 * envelope * (.018 * Math.Sin(phase * 110) + .012 * Math.Sin(phase * 165) +
                                                         .012 * accent * Math.Sin(phase * 440))));
                writer.Write((short)(32768 * envelope * (.018 * Math.Sin(phase * 110 + .3) +
                                                         .012 * Math.Sin(phase * 220) +
                                                         .008 * accent * Math.Sin(phase * 440))));
            }
        }

        Wave("Long evolving pad.wav", 180);
        Wave("Short pad.wav", 4);
        MediaFoundationApi.Startup();
        using (var pcm = new WaveFileReader(Path.Combine(sources, "Short pad.wav")))
            MediaFoundationEncoder.EncodeToMp3(pcm, Path.Combine(sources, "Pad.mp3"), 192000);
        using (var pcm = new WaveFileReader(Path.Combine(sources, "Short pad.wav")))
        {
            var type = MediaFoundationEncoder.GetOutputMediaTypes(AudioSubtypes.MFAudioFormat_FLAC)
                           .FirstOrDefault(t =>
                               t.SampleRate == pcm.WaveFormat.SampleRate && t.ChannelCount == pcm.WaveFormat.Channels &&
                               t.BitsPerSample == 16)
                       ?? throw new NotSupportedException(
                           "This Windows installation supplies no FLAC encoder for fixture creation.");
            using var encoder = new MediaFoundationEncoder(type);
            encoder.Encode(Path.Combine(sources, "Pad.flac"), pcm);
        }

        Console.WriteLine(project);
    }

    private static async Task NativeAudioClipsAsync(MainWindow main, string output, string project, string phase)
    {
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var input = new NativePointer.DrumInput(main);

        Control Id(string id) => main.GetVisualDescendants().OfType<Control>().Concat(
            main.GetVisualDescendants().OfType<ArrangementPaneContent>().SelectMany(p =>
                p.AudioPropertiesContent is Control content
                    ? content.GetVisualDescendants().OfType<Control>().Prepend(content)
                    : [])).Single(c =>
            c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);

        async Task Wait(Func<bool> condition, string message)
        {
            for (int i = 0; i < 1000 && !condition(); i++) await Task.Delay(20);
            Require(condition(), message + " · " + workflow.State.Detail);
        }

        async Task Click(string id, bool dialog = false) => await input.Click(Id(id), allowOwnedDialog: dialog);
        main.WindowState = WindowState.Maximized;
        await Task.Delay(250);
        main.Activate();
        await Task.Delay(150);
        if (shell.Preferences.Language != HostLanguage.English) await Click("Preference.Language");
        await Click("Workflow.Open", true);
        try
        {
            await input.FileDialog(project);
        }
        catch
        {
            await CaptureAsync(main, output, "audio-open-failure");
            NativeScreen.CaptureOwnedDialog(main, Path.Combine(output, "audio-open-dialog.png"));
            Json(output, "audio-open-state",
                new { workflow.State.SavedPath, workflow.State.StatusKey, workflow.State.Detail });
            throw;
        }

        await Wait(() => workflow.State.SavedPath == project && !workflow.State.Busy,
            "Native audio project Open failed");
        var original = workflow.State.Snapshot.State;
        foreach (var pane in workspace.State.Panes
                     .Where(p => p.InstanceId != WorkspaceState.ArrangementId && p.Visibility == PaneVisibility.Visible)
                     .ToArray())
        {
            await Click(pane.InstanceId + ".open");
            await Click(pane.InstanceId + ".hide");
        }

        await Click(WorkspaceState.ArrangementId + ".open");
        var frame = (WorkspacePane)Id(WorkspaceState.ArrangementId);
        var bounds = workspace.State.Bounds(WorkspaceState.ArrangementId);
        await input.Drag(Field<Border>(frame, "_header"), new(120, 12), new(120 - bounds.X, 12 - bounds.Y));
        bounds = workspace.State.Bounds(WorkspaceState.ArrangementId);
        await input.Drag(Field<Border>(frame, "_corner"), new(12, 12),
            new(12 + workspace.State.Width - bounds.Width, 12 + workspace.State.Height - bounds.Height));
        var paneContent = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var canvas = paneContent.Canvas;
        var scroll = Field<ScrollViewer>(paneContent, "_scroll");

        async Task Reach(AudioClip clip)
        {
            var rect = canvas.AudioRect(clip);
            for (int i = 0; i < 30; i++)
            {
                double y = rect.Y - scroll.Offset.Y;
                if (y >= 40 && y + rect.Height < scroll.Viewport.Height - 12) return;
                await input.Wheel(scroll, new(scroll.Viewport.Width - 60, scroll.Viewport.Height / 2),
                    y < 40 ? 240 : -240);
            }

            throw new InvalidOperationException("Audio lane cannot be reached through native scroll.");
        }

        async Task Save()
        {
            await Click("Workflow.Save");
            await Wait(() => !workflow.State.IsDirty && !workflow.State.Busy, "Audio checkpoint Save failed");
        }

        if (phase == "author")
        {
            Require(original.AudioClips.Length <= 3,
                "Native author phase requires an import checkpoint with at most three clips.");
            string sources = Path.Combine(Path.GetDirectoryName(project)!, "Sources");
            foreach (string name in new[] { "Pad.mp3", "Pad.flac", "Long evolving pad.wav" })
            {
                if (workflow.State.Snapshot.State.Sounds.Any(s => s.Name == Path.GetFileNameWithoutExtension(name) &&
                                                                  s.ResourceIds.Any(id =>
                                                                      workflow.State.Snapshot.State.Resources
                                                                          .Single(r => r.Id == id).Origin ==
                                                                      "Imported " + Path.GetExtension(name)
                                                                          .TrimStart('.').ToUpperInvariant())))
                    continue;
                int count = workflow.State.Snapshot.State.AudioClips.Length;
                await Click("Arrangement.ImportAudio", true);
                await input.FileDialog(Path.Combine(sources, name));
                await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == count + 1,
                    "Native audio import failed: " + name);
                await Save();
            }

            Require(
                original.Patterns.SequenceEqual(workflow.State.Snapshot.State.Patterns) &&
                original.Placements.SequenceEqual(workflow.State.Snapshot.State.Placements),
                "Audio import changed Pattern music.");
            var clip = workflow.State.Snapshot.State.AudioClips.Last();
            await Reach(clip);
            var rect = canvas.AudioRect(clip);
            long beforeMove = clip.Position.Ticks;
            await input.Drag(canvas, new(rect.X + 30, rect.Y + 6),
                new(rect.X + 30 + 2 * original.Settings.Meter.TicksPerBar * canvas.TickPixels, rect.Y + 6));
            await Wait(() => !workflow.State.Busy, "Audio move stayed busy");
            clip = workflow.State.Snapshot.State.AudioClips.Single(c => c.Id == clip.Id);
            Require(clip.Position.Ticks > 0, "Native audio move did not commit");
            int undo = workflow.State.UndoCount;
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Audio Undo stayed busy");
            Require(workflow.State.Snapshot.State.AudioClips.Single(c => c.Id == clip.Id).Position.Ticks == beforeMove,
                "Audio move Undo failed");
            await Click("Workflow.Redo");
            await Wait(() => !workflow.State.Busy, "Audio Redo stayed busy");
            Require(workflow.State.UndoCount == undo, "Audio move Redo history failed");
            await input.Click(canvas, new(canvas.AudioRect(clip).X + 20, canvas.AudioRect(clip).Y + 6));
            await Click("Arrangement.AudioProperties");
            await Task.Delay(100);
            await input.Exact(Id("AudioClip.Length"), "50");
            await input.Exact(Id("AudioClip.FadeIn"), "1000");
            await input.Exact(Id("AudioClip.FadeOut"), "2000");
            await input.Exact(Id("AudioClip.Gain"), "-3");
            await Click("AudioClip.Apply");
            await Wait(() => !workflow.State.Busy, "Audio range stayed busy");
            clip = workflow.State.Snapshot.State.AudioClips.Single(c => c.Id == clip.Id);
            Require(clip.LengthFrames == 2400000 && clip.FadeInFrames == 48000 && clip.FadeOutFrames == 96000,
                "Native audio range/fade edit failed");
            await Click("Arrangement.AudioProperties");
            await Click("AudioClip.NewChannel");
            await Wait(() => !workflow.State.Busy, "Audio Mixer assignment stayed busy");
            Require(workflow.State.Snapshot.State.Mixer!.Channels.Any(c => c.SoundIds.Contains(clip.SoundId)),
                "Audio source has no Mixer assignment");
            await input.Click(canvas, new(canvas.AudioRect(clip).X + 20, canvas.AudioRect(clip).Y + 6));
            input.Key(0x44, modifier: 0x11);
            await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == 4,
                "Audio duplicate failed");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Audio duplicate Undo failed");
            await Click("Arrangement.Slice");
            rect = canvas.AudioRect(clip);
            await input.Click(canvas,
                new(rect.X + 4 * original.Settings.Meter.TicksPerBar * canvas.TickPixels, rect.Y + 30));
            await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == 4,
                "Audio slice failed");
            await Click("Arrangement.Slice");
            await Save();
            await CaptureAsync(main, output, "audio-authored");
        }
        else if (phase == "review")
        {
            var clip = original.AudioClips.Last();
            await Reach(clip);
            await Click("Arrangement.Fit");
            foreach (int variation in Enumerable.Range(0, 4))
            {
                await input.Click(canvas, new(canvas.AudioRect(clip).X + 16, canvas.AudioRect(clip).Y + 7));
                await Click("Arrangement.AudioProperties");
                await Task.Delay(120);
                var alignments = new List<object>();
                foreach (string key in new[] { "Offset", "Length", "FadeIn", "FadeOut", "Gain" })
                {
                    var field = Id("AudioClip." + key);
                    var row = (Grid)field.Parent!;
                    alignments.Add(FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().First(), row, key));
                }

                Json(output, "audio-field-alignment-" + variation, alignments);
                await CaptureAsync(main, output, "audio-" + shell.Preferences.Theme + "-" + shell.Preferences.Language);
                Require(
                    NativeScreen.Capture(main, Path.Combine(output, "audio-screen-" + variation + ".png"))
                        .StartsWith("passed"), "Audio native screen capture failed");
                input.Key(0x1B);
                await Task.Delay(80);
                if (variation % 2 == 0) await Click("Preference.Language");
                else await Click("Preference.Theme");
            }

            await Click("Window.Maximize");
            await Task.Delay(150);
            await input.Drag(main, new(main.Bounds.Width - 3, main.Bounds.Height - 3), new(1021, 765));
            await Task.Delay(150);
            await Reach(clip);
            await Click("Arrangement.Fit");
            await input.Click(canvas, new(canvas.AudioRect(clip).X + 16, canvas.AudioRect(clip).Y + 7));
            await Click("Arrangement.AudioProperties");
            await Task.Delay(100);
            Require(NativeScreen.Capture(main, Path.Combine(output, "audio-screen-reduced.png")).StartsWith("passed"),
                "Reduced Audio Clip native capture failed");
            Json(output, "audio-reduced-layout",
                new { main.Bounds, RestoredFromMaximized = true, NativeResizeAttempted = true });
            input.Key(0x1B);
            await Save();
        }
        else if (phase == "play")
        {
            await Click("Arrangement.Start");
            await Click("Arrangement.Play");
            await Wait(() => workflow.State.Execution?.Execution.Playing == true, "Audio Song did not start");
            await Task.Delay(1500);
            Require(workflow.State.MixerMeters.Values.Any(m => m.Frames > 0 && m.PeakLeft > 0),
                "Audio Song has no measured signal");
            await CaptureAsync(main, output, "audio-playing");
            // Native ruler seek and range wrap, including source age reconstruction in the middle of the pad.
            double x = canvas.HeaderSize + 8 * original.Settings.Meter.TicksPerBar * canvas.TickPixels;
            await input.Click(canvas, new(x, scroll.Offset.Y + 12));
            await Task.Delay(300);
            await input.Drag(canvas, new(x, scroll.Offset.Y + 12),
                new(x + 2 * original.Settings.Meter.TicksPerBar * canvas.TickPixels, scroll.Offset.Y + 12));
            await Task.Delay(4800);
            Require(
                workflow.State.SongLoopEnabled && workflow.State.Execution?.Execution.Playing == true &&
                workflow.State.StatusKey != "Workflow.DeviceFault", "Physical audio range loop failed");
            await Click("Workflow.Stop");
            string wave = Path.Combine(Path.GetDirectoryName(project)!,
                "Audio Clips verified " + DateTime.UtcNow.ToString("HHmmss") + ".wav");
            await Click("Arrangement.Export", true);
            await input.FileDialog(wave);
            await Wait(() => File.Exists(wave) && !workflow.State.Busy, "Native audio Song WAV failed");
            using var read = new WaveFileReader(wave);
            Require(read.Length > 0 && read.WaveFormat.Channels == 2 && read.WaveFormat.SampleRate == 48000,
                "Export has invalid PCM format");
            await Save();
        }
        else if (phase == "check")
        {
            string directory = Path.GetDirectoryName(project)!;
            string sources = Path.Combine(directory, "Sources");
            string cancellationFile = Path.Combine(sources, "Cancellation 1200s.wav");
            if (!File.Exists(cancellationFile))
            {
                using var writer = new WaveFileWriter(cancellationFile, WaveFormat.CreateIeeeFloatWaveFormat(48000, 2));
                var block = new byte[48000 * 8];
                for (int second = 0; second < 1200; second++) writer.Write(block, 0, block.Length);
            }

            string surround = Path.Combine(sources, "Surround.wav");
            if (!File.Exists(surround))
            {
                using var writer = new WaveFileWriter(surround, new WaveFormat(48000, 16, 6));
                writer.Write(new byte[48000 * 12], 0, 48000 * 12);
            }

            foreach (string name in new[] { "Broken.wav", "Unsupported.ogg" })
                if (!File.Exists(Path.Combine(sources, name)))
                    File.WriteAllBytes(Path.Combine(sources, name), "Not audio"u8.ToArray());

            int count = original.AudioClips.Length, history = workflow.State.UndoCount;
            await Click("Arrangement.ImportAudio", true);
            await input.FileDialog(cancellationFile);
            await Wait(() => workflow.State.Busy && canvas.IsImportingAudio && main.GetVisualDescendants()
                    .OfType<Control>()
                    .Any(c => c.IsEffectivelyVisible &&
                              AutomationProperties.GetAutomationId(c) == "Arrangement.CancelAudio"),
                "Import completed before native cancellation could be exercised");
            await Click("Arrangement.CancelAudio");
            await Wait(() => !workflow.State.Busy, "Cancelled import did not end");
            Require(workflow.State.StatusKey == "Workflow.Cancelled" && workflow.State.UndoCount == history &&
                    workflow.State.Snapshot.State.AudioClips.Length == count, "Cancellation changed project/history");
            var failures = new List<object>();
            foreach (string name in new[] { "Broken.wav", "Unsupported.ogg", "Surround.wav" })
            {
                await Click("Arrangement.ImportAudio", true);
                await input.FileDialog(Path.Combine(sources, name));
                await Task.Delay(150); // Let the native dialog's posted import command reach the owner.
                await Wait(() => !workflow.State.Busy && workflow.State.StatusKey == "Workflow.ImportFailed",
                    "Invalid source did not show an import error");
                Require(workflow.State.Snapshot.State.AudioClips.Length == count && workflow.State.UndoCount == history,
                    "Rejected source changed project/history");
                failures.Add(new { name, workflow.State.Detail });
                string expected = name == "Broken.wav"
                    ? "AudioClip.DecodeFailed"
                    : name == "Unsupported.ogg"
                        ? "AudioClip.UnsupportedFormat"
                        : "AudioClip.UnsupportedChannels";
                Require(workflow.State.Detail == expected, "Import error belongs to a different source");
                await CaptureAsync(main, output, "audio-error-" + Path.GetFileNameWithoutExtension(name));
            }

            Json(output, "audio-import-boundaries", new { NativeCancel = true, failures });

            await Click("Workflow.EditTempo");
            await input.Exact(Field<TextBox>(Field<WorkflowControls>(main, "_workflowControls"), "_tempoEdit"), "140");
            await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.Settings.Tempo.BeatsPerMinute == 140,
                "Native BPM change failed");
            Require(original.AudioClips.SequenceEqual(workflow.State.Snapshot.State.AudioClips),
                "BPM changed source frames/fades");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "BPM Undo failed");
            Require(workflow.State.Snapshot.State.Settings.Tempo == original.Settings.Tempo,
                "BPM Undo did not restore tempo");

            var longClip = original.AudioClips
                .OrderByDescending(c => AudioClipSource.Frames(original.Sounds.Single(s => s.Id == c.SoundId))).First();
            await Reach(longClip);
            await input.Click(canvas, new(canvas.AudioRect(longClip).X + 20, canvas.AudioRect(longClip).Y + 6));
            await Click("Arrangement.Start");
            await Click("Arrangement.Play");
            await Wait(() => workflow.State.Execution?.Execution.Playing == true, "Alias memory playback failed");
            await Task.Delay(500);
            await input.Click(canvas, new(canvas.AudioRect(longClip).X + 20, canvas.AudioRect(longClip).Y + 6));
            long baseline = Process.GetCurrentProcess().WorkingSet64;
            for (int i = 0; i < 12; i++)
            {
                input.Key(0x44, modifier: 0x11);
                await Wait(
                    () => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == count + i + 1,
                    "Native long source duplicate failed");
            }

            long aliases = Process.GetCurrentProcess().WorkingSet64;
            Require(aliases - baseline < 300 * 1024 * 1024, "Aliases retained disproportionate source PCM");
            for (int i = 0; i < 12; i++)
            {
                await Click("Workflow.Undo");
                await Wait(() => !workflow.State.Busy, "Alias Undo failed");
            }

            await Click("Workflow.Stop");
            Require(original.AudioClips.SequenceEqual(workflow.State.Snapshot.State.AudioClips),
                "Alias Undo did not restore clips");
            Json(output, "audio-alias-memory", new
            {
                AdditionalClips = 12, baseline, aliases, Growth = aliases - baseline,
                UniqueLongSourceBytes =
                    AudioClipSource.Frames(original.Sounds.Single(s => s.Id == longClip.SoundId)) * 8
            });

            string relocated = Path.Combine(directory, "Relocated", "Neon Orchard - Portable Audio.seqvium");
            Directory.CreateDirectory(Path.GetDirectoryName(relocated)!);
            Require(!File.Exists(relocated), "Native portability destination must be new");
            await Click("Workflow.More");
            var overflow = Field<StackPanel>(Field<WorkflowControls>(main, "_workflowControls"), "_overflowBody");
            await input.Click(overflow.Children.Single(c =>
                AutomationProperties.GetAutomationId(c) == "Workflow.SaveAs"), allowOwnedDialog: true);
            await input.FileDialog(relocated);
            await Wait(() => !workflow.State.Busy && workflow.State.SavedPath == relocated && !workflow.State.IsDirty,
                "Native Audio Save As failed");
            Json(output, "audio-portable-save",
                new
                {
                    relocated,
                    ManagedMedia = Directory.GetFiles(relocated + ".media", "*.wav", SearchOption.AllDirectories).Length
                });
        }
        else if (phase == "edit")
        {
            var audio = original.AudioClips.OrderByDescending(c => c.LengthFrames).First(c => c.Repeats == 1);
            await Reach(audio);
            await Click("Arrangement.Fit");
            var rect = canvas.AudioRect(audio);
            await input.Drag(canvas, new(rect.Right - 2, rect.Bottom - 5),
                new(rect.Right - 2 - original.Settings.Meter.TicksPerBar * canvas.TickPixels, rect.Bottom - 5));
            await Wait(() => !workflow.State.Busy, "Native right trim stayed busy");
            Require(
                workflow.State.Snapshot.State.AudioClips.Single(c => c.Id == audio.Id).LengthFrames <
                audio.LengthFrames,
                "Native right trim did not change the source range");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Native trim Undo failed");
            rect = canvas.AudioRect(audio);
            await input.Drag(canvas, new(rect.X + 2, rect.Bottom - 5),
                new(rect.X + 2 + original.Settings.Meter.TicksPerBar * canvas.TickPixels, rect.Bottom - 5));
            await Wait(() => !workflow.State.Busy, "Native left trim stayed busy");
            Require(
                workflow.State.Snapshot.State.AudioClips.Single(c => c.Id == audio.Id).OffsetFrames >
                audio.OffsetFrames,
                "Native left trim did not advance source offset");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Native left trim Undo failed");
            rect = canvas.AudioRect(audio);
            await input.Drag(canvas, new(rect.X + 5, rect.Y + 21), new(rect.X + 42, rect.Y + 21));
            await Wait(() => !workflow.State.Busy, "Native fade stayed busy");
            Require(
                workflow.State.Snapshot.State.AudioClips.Single(c => c.Id == audio.Id).FadeInFrames !=
                audio.FadeInFrames,
                "Native upper-corner fade did not commit");
            await CaptureAsync(main, output, "audio-native-fade");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Native fade Undo failed");
            Require(original.AudioClips.SequenceEqual(workflow.State.Snapshot.State.AudioClips),
                "Native pointer edit checks did not restore the checkpoint");
            await Save();
            await Wait(() => Field<WorkflowState>(canvas, "_observation").Snapshot == workflow.State.Snapshot,
                "Canvas did not adopt the restored audio snapshot");
            await Task.Delay(150); // Allow the native compositor to present the restored waveform.
            await CaptureAsync(main, output, "audio-final-saved");
            Json(output, "audio-pointer-edits",
                new { NativeInput = true, LeftTrim = true, RightTrim = true, CornerFade = true, Undo = true });
        }
        else if (phase == "mixer")
        {
            var audio = original.AudioClips.OrderByDescending(c => c.LengthFrames).First(c =>
                original.Mixer!.Channels.Any(channel => channel.SoundIds.Contains(c.SoundId)));
            var channel = original.Mixer!.Channels.Single(c => c.SoundIds.Contains(audio.SoundId));
            await Click("Arrangement.Start");
            await Click("Arrangement.Play");
            await Wait(() => workflow.State.Execution?.Execution.Playing == true, "Audio Mixer playback failed");
            await input.Click(canvas,
                new(canvas.HeaderSize + audio.Position.Ticks * canvas.TickPixels, scroll.Offset.Y + 12));
            await Task.Delay(800);
            Require(workflow.State.MixerMeters.TryGetValue(channel.Id.Value, out var meter) && meter.PeakLeft > 0,
                "Audio source does not reach its existing Mixer channel");
            await Click(WorkspaceState.MixerId + ".open");
            var mute = Id("Mixer." + channel.Id + ".Mute");
            var bank = mute.GetVisualAncestors().OfType<ScrollViewer>().Last();
            for (int attempt = 0; attempt < 40; attempt++)
            {
                var p = mute.TranslatePoint(new(mute.Bounds.Width / 2, mute.Bounds.Height / 2), bank)!.Value;
                if (p.X >= 12 && p.X < bank.Viewport.Width - 12) break;
                await input.Wheel(bank, new(bank.Bounds.Width / 2, 80), p.X < 12 ? -480 : 480, true);
            }

            await input.Click(mute);
            await Wait(() => !workflow.State.Busy, "Audio Mixer mute stayed busy");
            Require(workflow.State.Snapshot.State.Mixer!.Channels.Single(c => c.Id == channel.Id).Settings.Muted,
                "Native Audio Mixer mute did not commit");
            await Task.Delay(150);
            await CaptureAsync(main, output, "audio-mixer-muted");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Audio Mixer Undo failed");
            await Click("Workflow.Redo");
            await Wait(() => !workflow.State.Busy, "Audio Mixer Redo failed");
            Require(workflow.State.Snapshot.State.Mixer!.Channels.Single(c => c.Id == channel.Id).Settings.Muted,
                "Audio Mixer Redo lost mute");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Audio Mixer mute restoration failed");
            await input.Click(Id("Mixer." + channel.Id + ".Solo"));
            await Wait(() => !workflow.State.Busy, "Audio Mixer solo stayed busy");
            Require(workflow.State.Snapshot.State.Mixer!.Channels.Single(c => c.Id == channel.Id).Settings.Solo,
                "Native Audio Mixer solo did not commit");
            await Task.Delay(300);
            await CaptureAsync(main, output, "audio-mixer-solo");
            await Click("Workflow.Undo");
            await Wait(() => !workflow.State.Busy, "Audio Mixer solo restoration failed");
            await Click("Workflow.Stop");
            Require(
                original.Mixer == workflow.State.Snapshot.State.Mixer &&
                original.AudioClips.SequenceEqual(workflow.State.Snapshot.State.AudioClips),
                "Mixer check did not restore the musical checkpoint");
            await Save();
            Json(output, "audio-mixer-check", new { NativeMuteSolo = true, UndoRedo = true, channel.Id, meter });
        }
        else if (phase == "drop")
        {
            var dropEvents = new List<object>();
            canvas.AddHandler(Avalonia.Input.DragDrop.DragOverEvent,
                (_, e) => dropEvents.Add(new
                {
                    Event = "over", e.DragEffects, Position = e.GetPosition(canvas),
                    Files = e.DataTransfer.TryGetFiles()?.Select(f => f.Path.ToString()).ToArray()
                }), handledEventsToo: true);
            canvas.AddHandler(Avalonia.Input.DragDrop.DropEvent,
                (_, e) => dropEvents.Add(new { Event = "drop", e.DragEffects, Position = e.GetPosition(canvas) }),
                handledEventsToo: true);
            var reference = original.AudioClips.Last();
            await Reach(reference);
            string file = Path.Combine(Path.GetDirectoryName(project)!, "Sources", "Library piano.wav");
            int count = original.AudioClips.Length;
            await NativePointer.ExplorerDropAsync(file, canvas,
                new(canvas.HeaderSize + 30,
                    canvas.AudioRect(reference).Y + 30 + HostVisual.Metric(canvas, "Playlist.LaneHeight")), output);
            main.Activate();
            await Task.Delay(150);
            Json(output, "explorer-drop-events", dropEvents);
            await CaptureAsync(main, output, "audio-after-drop");
            await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == count + 1,
                "Explorer audio drop failed");
            Require(workflow.State.Snapshot.State.Sounds.Last().Name == "Library piano",
                "Explorer imported a different source");
            await Save();
            await CaptureAsync(main, output, "audio-explorer-drop");
        }
        else
            throw new ArgumentException("Native audio phase must be author, review, drop, check, edit, mixer or play.");

        Json(output, "native-audio", new
        {
            Phase = phase, NativeInput = true, NativePickers = true, ExplorerDrop = phase == "drop",
            PhysicalOutput = phase == "play", SubjectiveListening = false, Project = project,
            AudioClips = workflow.State.Snapshot.State.AudioClips,
            WorkingSetBytes = Process.GetCurrentProcess().WorkingSet64
        });
    }
}
