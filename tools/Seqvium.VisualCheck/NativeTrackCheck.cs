// SPDX-License-Identifier: Apache-2.0

using System.Security.Cryptography;
using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task NativeTrackAsync(MainWindow main, string output, string project, string library,
        string phase)
    {
        project = Path.GetFullPath(project);
        var c = new TrackComposer(main, output, project, library);
        await c.Start();
        switch (phase)
        {
            case "rhythm": await c.Rhythm(); break;
            case "melody": await c.Melody(); break;
            case "preview": await c.Preview(); break;
            case "balance": await c.Balance(); break;
            case "seeds": await c.Seeds(); break;
            case "seeds-forte": await c.SeedsForte(); break;
            case "arrange": await c.Arrange(); break;
            case "play": await c.PlayAndExport(); break;
            default: throw new ArgumentException("Unknown native track phase.");
        }

        await c.Save();
        c.VerifyLibrary();
        Json(output, "native-track", new
        {
            Phase = phase, NativeInput = true, InternalMusicalMutation = false,
            Project = project, Patterns = c.State.Patterns.Length, Tracks = c.State.ArrangementTracks.Length,
            Clips = c.State.ArrangementClips.Length
        });
        await CaptureAsync(main, output, "native-track-" + phase);
    }

    /// <summary>Windows input authoring of a particular score. State and canvas geometry are read-only oracles;
    /// no musical mutation, transport, persistence or editor command API is called.</summary>
    private sealed class TrackComposer(MainWindow main, string output, string project, string library)
    {
        private readonly NativePointer.DrumInput _input = new(main);
        private readonly Dictionary<string, string> _sources = [], _hashes = [];
        private DesktopWorkflow Workflow => Field<DesktopWorkflow>(main, "_workflow");
        internal ProjectState State => Workflow.State.Snapshot.State;
        private WorkspaceHost Workspace => Field<WorkspaceHost>(main, "_workspace");
        private BeatPaneContent Rack => main.GetVisualDescendants().OfType<BeatPaneContent>().Single();
        private PianoRollPaneContent Piano => main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();

        private ArrangementPaneContent Playlist =>
            main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();

        private Pattern Pattern => BeatSteps.Resolve(Workflow.State).Pattern!;

        private Control Id(string id) => main.GetVisualDescendants().OfType<Control>()
            .Single(c => c.IsEffectivelyVisible && AutomationProperties.GetAutomationId(c) == id);

        internal Task Click(string id) => _input.Click(Id(id));

        internal async Task Start()
        {
            await Task.Delay(350);
            main.Activate();
            await Task.Delay(150);
            if (main.WindowState != WindowState.Maximized) await Click("Window.Maximize");
            if (Field<ShellSession>(main, "_session").Preferences.Language != HostLanguage.English)
                await Click("Preference.Language");
            using var catalog = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(library, "catalog.json")));
            foreach (var (key, match) in new[]
                     {
                         ("Bass", "Moe-Dee_Multi_A1"), ("Keys", "Player_dyn1_rr1_018"),
                         ("Pizz", "LLVln_Pizz_A3_p"), ("Pizz forte", "LLVln_Pizz_A3_f"), ("Flute", "LDFlute_expvib_A3"),
                         ("FX", "DNB_FX_13__")
                     })
            {
                string path = Path.GetFullPath(Path.Combine(library, catalog.RootElement.EnumerateArray().Single(e =>
                        e.GetProperty("file").GetString()!.Contains(match, StringComparison.Ordinal))
                    .GetProperty("file")
                    .GetString()!));
                _sources[key] = path;
                _hashes[path] = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
            }

            if (File.Exists(project))
            {
                await Click("Workflow.Open");
                await _input.FileDialog(project);
                await Wait(() => Workflow.State.SavedPath == project && !Workflow.State.Busy,
                    "Native project Open failed.");
                Require(Workflow.State.Execution is null, "Open started output.");
            }
            else
            {
                await Click("Workflow.EditTempo");
                await _input.Exact(Field<TextBox>(Field<WorkflowControls>(main, "_workflowControls"), "_tempoEdit"),
                    "112");
                await Wait(() => State.Settings.Tempo.BeatsPerMinute == 112, "Native tempo failed.");
                await Save();
            }

            await Pane(WorkspaceState.BeatId);
        }

        private static async Task Wait(Func<bool> condition, string failure)
        {
            for (int i = 0; i < 150 && !condition(); i++) await Task.Delay(30);
            Require(condition(), failure);
        }

        internal async Task Save()
        {
            await Click("Workflow.Save");
            if (Workflow.State.SavedPath is null) await _input.FileDialog(project);
            await Wait(() => Workflow.State.SavedPath == project && !Workflow.State.IsDirty && !Workflow.State.Busy,
                "Native checkpoint Save failed: " + Workflow.State.Detail);
            Json(output, "track-progress",
                new
                {
                    Project = project, Saved = true, Patterns = State.Patterns.Select(p => p.Name),
                    Notes = State.Patterns.Sum(p => p.Parts.Sum(part => part.Notes.Length)),
                    Clips = State.ArrangementClips.Length
                });
        }

        private async Task Pane(string id)
        {
            foreach (string other in new[]
                     {
                         WorkspaceState.NodeEditorId, WorkspaceState.PianoId, WorkspaceState.ArrangementId,
                         WorkspaceState.BeatId
                     })
            {
                if (other == id || Workspace.State.Get(other).Visibility != PaneVisibility.Visible) continue;
                await Click(other + ".open");
                await Click(other + ".hide");
            }

            await Click(id + ".open");
            var pane = (WorkspacePane)Id(id);
            var bounds = Workspace.State.Bounds(id);
            if (bounds.X != 0 || bounds.Y != 0)
                await _input.Drag(Field<Border>(pane, "_header"), new(100, 16), new(100 - bounds.X, 16 - bounds.Y));
            bounds = Workspace.State.Bounds(id);
            if (Math.Abs(bounds.Width - Workspace.State.Width) > 3 ||
                Math.Abs(bounds.Height - Workspace.State.Height) > 3)
                await _input.Drag(Field<Border>(pane, "_corner"), new(12, 12),
                    new(12 + Workspace.State.Width - bounds.Width, 12 + Workspace.State.Height - bounds.Height));
            await Task.Delay(100);
        }

        private async Task New(string name, int bars)
        {
            await Pane(WorkspaceState.BeatId);
            var existing = State.Patterns.SingleOrDefault(p => p.Name == name);
            if (existing is null)
            {
                await Click("Beat.Create");
                await Rack.PendingCommand;
                await _input.Exact(Id("Beat.Name"), name);
                await Rack.PendingCommand;
            }
            else if (Pattern.Id != existing.Id)
            {
                var attachment = State.GraphAttachments.Single(a =>
                    a.ContextId == State.Placements.Single(p => p.PatternId == existing.Id).ItemContextId);
                await _input.Select((ComboBox)Id("Beat.Target"),
                    Workflow.State.Occurrences.Select(o => o.Id).ToList().IndexOf(attachment.Id));
                await Rack.PendingCommand;
            }

            if (Pattern.Length.Ticks != bars * BeatSteps.BarTicks)
            {
                await _input.Select((ComboBox)Id("Beat.Bars"), bars - 1);
                await Rack.PendingCommand;
            }

            Require(Pattern.Name == name, "Native Pattern name failed.");
            await Save();
        }

        private async Task Drum(string key, int preset)
        {
            await Pane(WorkspaceState.BeatId);
            var instrument = DrumInstruments.All.Single(i => i.Key == key);
            var part = Pattern.Parts.SingleOrDefault(p =>
                State.Sounds.Single(s => s.Id == p.SoundId).Algorithm == instrument.Algorithm);
            if (part is null)
            {
                await Click("Beat.AddDrum");
                await _input.Click(((Button)Id("Beat.AddDrum")).ContextMenu!.Items.OfType<MenuItem>()
                    .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.AddDrum." + key));
                await Rack.PendingCommand;
            }
            else
            {
                await Click("Beat.Actions." + part.Id);
                await _input.Click(((Button)Id("Beat.Actions." + part.Id)).ContextMenu!.Items.OfType<MenuItem>()
                    .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.Sound." + part.Id));
            }

            var drum = main.GetVisualDescendants().OfType<DrumVoicePaneContent>().Single();
            await _input.Select((ComboBox)Id("Drum.Preset"), preset);
            await drum.PendingCommand;
            await Click("Drum.Audition");
            await drum.PendingCommand;
            await Task.Delay(220);
            await Click("Workflow.Stop");
            await Click(WorkspaceState.NodeEditorId + ".hide");
        }

        private async Task Steps(string key, int[] steps)
        {
            await Pane(WorkspaceState.BeatId);
            var part = Pattern.Parts.Single(p =>
                State.Sounds.Single(s => s.Id == p.SoundId).Algorithm ==
                DrumInstruments.All.Single(i => i.Key == key).Algorithm);
            foreach (int step in steps)
            {
                if (Pattern.Parts.Single(p => p.Id == part.Id).Notes.Any(n => BeatSteps.Step(n) == step)) continue;
                var pad = Id($"Beat.Step.{part.Id}.{step}");
                var scroll = Field<ScrollViewer>(Rack, "_rackScroll");
                var at = pad.TranslatePoint(default, scroll)!.Value;
                for (int i = 0; i < 30 && (at.X < 0 || at.X + pad.Bounds.Width > scroll.Viewport.Width); i++)
                {
                    await _input.Wheel(scroll, new(scroll.Bounds.Width / 2, scroll.Bounds.Height / 2),
                        at.X < 0 ? -360 : 360, true);
                    at = pad.TranslatePoint(default, scroll)!.Value;
                }

                await _input.Click(pad);
                await Rack.PendingCommand;
            }

            await Save();
        }

        private async Task PianoPart(Id<MusicalPart> part)
        {
            await Pane(WorkspaceState.BeatId);
            await Click("Beat.Actions." + part);
            await _input.Click(((Button)Id("Beat.Actions." + part)).ContextMenu!.Items.OfType<MenuItem>()
                .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.Piano." + part));
            await Pane(WorkspaceState.PianoId);
            if (((ToggleButton)Id("Piano.Audition")).IsChecked == true) await Click("Piano.Audition");
            await Click("Piano.Fit");
        }

        private async Task Sample(string key, int root)
        {
            await Pane(WorkspaceState.BeatId);
            var part = Pattern.Parts.SingleOrDefault(p => p.Name == key);
            if (part is null)
            {
                int count = Pattern.Parts.Length;
                await Click("Beat.AddWav");
                await _input.FileDialog(_sources[key]);
                await Rack.PendingCommand;
                await Wait(() => Pattern.Parts.Length == count + 1 && !Workflow.State.Busy,
                    "Native sample import failed: " + Workflow.State.Detail);
                part = Pattern.Parts.Last();
            }

            await PianoPart(part.Id);
            await Click("Piano.Root");
            await _input.Exact(Field<TextBox>(Piano, "_channelName"), key);
            if ((int)State.Sounds.Single(s => s.Id == part.SoundId).Parameters["rootPitch"] != root)
                await _input.Select(Field<ComboBox>(Piano, "_rootPitch"), root);
            await _input.Click(((StackPanel)Field<Flyout>(Piano, "_rootFlyout").Content!).GetVisualDescendants()
                .OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Piano.Apply"));
            await Piano.PendingCommand;
            Require(
                Piano.Canvas.Part!.Name == key &&
                State.Sounds.Single(s => s.Id == Piano.Canvas.Part.SoundId).Parameters["rootPitch"] == root,
                "Native source tuning/name failed.");
            await Click("Piano.Fit");
        }

        private async Task Notes((decimal At, int Pitch, decimal Length)[] notes, int velocity)
        {
            var canvas = Piano.Canvas;
            await Click("Piano.Draw");
            foreach (var extra in canvas.Part!.Notes.Where(n => !notes.Any(expected =>
                         n.Position.Ticks == (long)(expected.At * MusicalPosition.TicksPerQuarter) &&
                         n.Pitch == expected.Pitch)).ToArray())
            {
                var rect = canvas.NoteRect(extra);
                await _input.Click(canvas, new(rect.X + 8, rect.Y + rect.Height / 2));
                _input.Key(0x2E);
                await Piano.PendingCommand;
            }

            foreach (var note in notes)
            {
                long at = (long)(note.At * MusicalPosition.TicksPerQuarter),
                    length = (long)(note.Length * MusicalPosition.TicksPerQuarter);
                if (canvas.Part!.Notes.Any(n =>
                        n.Position.Ticks == at && n.Pitch == note.Pitch && n.Duration.Ticks == length)) continue;
                for (int i = 0;
                     i < 40 && (canvas.Y(note.Pitch) < canvas.RulerHeight + 2 ||
                                canvas.Y(note.Pitch) + canvas.RowHeight > canvas.NoteBottom);
                     i++)
                    await _input.Wheel(canvas, new(canvas.KeyWidth + 120, canvas.RulerHeight + 80),
                        canvas.Y(note.Pitch) < canvas.RulerHeight + 2 ? 120 : -120);
                await _input.Drag(canvas, new(canvas.X(at) + 2, canvas.Y(note.Pitch) + canvas.RowHeight / 2),
                    new(canvas.X(at + length), canvas.Y(note.Pitch) + canvas.RowHeight / 2));
                await Piano.PendingCommand;
                Require(canvas.Part!.Notes.Any(n => n.Position.Ticks == at && n.Pitch == note.Pitch),
                    "Native note draw failed.");
            }

            await Click("Piano.Select");
            _input.Key(0x41, modifier: 0x11);
            await Task.Delay(100);
            await Click("Piano.Properties");
            await _input.Exact(Field<NumericUpDown>(Piano, "_noteVelocity"), velocity.ToString());
            await _input.Click(((StackPanel)Field<Flyout>(Piano, "_noteFlyout").Content!).GetVisualDescendants()
                .OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Piano.Apply"));
            await Piano.PendingCommand;
            await Save();
        }

        internal async Task Rhythm()
        {
            await New("01 · Pulse", 1);
            await Drum("Kick", 2);
            await Steps("Kick", [0, 4, 8, 12]);
            await New("02 · Pulse variation", 2);
            await Drum("Kick", 2);
            await Steps("Kick", [0, 4, 8, 12, 16, 20, 23, 24, 28, 30]);
            await New("03 · Backbeat", 2);
            await Drum("Snare", 1);
            await Drum("Clap", 2);
            await Steps("Snare", [4, 12, 20, 28]);
            await Steps("Clap", [12, 28]);
            await New("04 · Hats", 2);
            await Drum("ClosedHat", 1);
            await Drum("OpenHat", 2);
            await Steps("ClosedHat", [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30]);
            await Steps("OpenHat", [7, 15, 23, 31]);
            await New("05 · Hats variation", 2);
            await Drum("ClosedHat", 2);
            await Drum("OpenHat", 0);
            await Steps("ClosedHat", [0, 2, 4, 6, 8, 9, 10, 12, 14, 16, 18, 20, 22, 24, 25, 26, 28, 30]);
            await Steps("OpenHat", [3, 11, 19, 27, 31]);
            await New("06 · Wooden pulse", 2);
            await Drum("Rim", 1);
            await Steps("Rim", [3, 7, 11, 15, 18, 23, 27, 31]);
            await New("07 · Falling toms", 1);
            await Drum("Tom", 1);
            await Drum("Snare", 1);
            await Steps("Snare", [12, 14, 15]);
            var tom = Pattern.Parts.Single(p =>
                State.Sounds.Single(s => s.Id == p.SoundId).Algorithm ==
                DrumInstruments.All.Single(i => i.Key == "Tom").Algorithm);
            await PianoPart(tom.Id);
            await Notes(
            [
                (2, 43, .25m), (2.5m, 41, .25m), (3, 38, .25m), (3.25m, 36, .25m), (3.5m, 34, .25m), (3.75m, 31, .25m)
            ], 75);
            await Click("Piano.Play");
            await Task.Delay(2500);
            await Click("Piano.Stop");
        }

        internal async Task Melody()
        {
            await New("08 · Root motion", 4);
            await Sample("Bass", 33);
            await Notes([
                (0, 33, .75m), (1.5m, 33, .5m), (2.5m, 40, .5m), (3.5m, 33, .25m), (4, 29, .75m), (5.5m, 29, .5m),
                (6.5m, 36, .5m), (7.5m, 29, .25m),
                (8, 36, .75m), (9.5m, 36, .5m), (10.5m, 43, .5m), (11.5m, 36, .25m), (12, 31, .75m), (13.5m, 31, .5m),
                (14.5m, 38, .5m), (15.5m, 31, .25m)
            ], 68);
            await New("09 · Root variation", 4);
            await Sample("Bass", 33);
            await Notes([
                (0, 33, .5m), (1, 40, .25m), (1.75m, 33, .25m), (2.5m, 33, .5m), (3.25m, 45, .25m), (4, 29, .5m),
                (5, 36, .25m), (5.75m, 29, .25m), (6.5m, 29, .5m), (7.25m, 41, .25m),
                (8, 36, .5m), (9, 43, .25m), (9.75m, 36, .25m), (10.5m, 36, .5m), (11.25m, 48, .25m), (12, 31, .5m),
                (13, 38, .25m), (13.75m, 31, .25m), (14.5m, 31, .5m), (15.25m, 43, .25m)
            ], 64);
            await New("10 · Orchard keys", 4);
            await Sample("Keys", 57);
            await Notes(Chords(false), 46);
            await New("11 · Open voicings", 4);
            await Sample("Keys", 57);
            await Notes(Chords(true), 42);
            await New("12 · Glass seeds", 4);
            await Sample("Pizz", 57);
            await Notes([
                (.5m, 69, .5m), (1.5m, 72, .5m), (2.5m, 76, .5m), (3.5m, 72, .5m), (4.5m, 69, .5m), (5.5m, 72, .5m),
                (6.5m, 77, .5m), (7.5m, 76, .5m),
                (8.5m, 67, .5m), (9.5m, 72, .5m), (10.5m, 76, .5m), (11.5m, 79, .5m), (12.5m, 67, .5m),
                (13.5m, 71, .5m), (14.5m, 74, .5m), (15.5m, 71, .5m)
            ], 52);
            await New("13 · Night air", 4);
            await Sample("Flute", 57);
            await Notes(
            [
                (0, 69, 1.5m), (2, 72, 1), (3, 76, 1), (4, 77, 2), (6.5m, 76, 1), (8, 72, 1.5m), (10, 67, 1),
                (11, 69, 1), (12, 71, 2), (14.5m, 67, 1)
            ], 46);
            await New("14 · Spark", 2);
            await Sample("FX", 60);
            await Notes([(0, 60, 6)], 24);
            await New("15 · Coda", 4);
            await Sample("Keys", 57);
            await Notes([
                (0, 57, 2), (0, 60, 2), (0, 64, 2), (0, 71, 2), (4, 53, 2), (4, 57, 2), (4, 60, 2), (4, 67, 2),
                (8, 52, 2), (8, 55, 2), (8, 59, 2), (8, 64, 2),
                (12, 57, 3.5m), (12, 60, 3.5m), (12, 64, 3.5m), (12, 69, 3.5m)
            ], 32);
            await Click("Piano.Play");
            await Task.Delay(3000);
            await Click("Piano.Stop");
        }

        private static (decimal, int, decimal)[] Chords(bool open)
        {
            int[][] pitches = open
                ? [[57, 64, 67, 72], [53, 60, 64, 69], [55, 60, 64, 71], [55, 62, 65, 69]]
                : [[57, 60, 64, 67], [53, 57, 60, 64], [55, 60, 64, 67], [55, 59, 62, 65]];
            return pitches.SelectMany((chord, bar) => chord.Select(p => ((decimal)(bar * 4), p, 2.5m))).ToArray();
        }

        internal async Task Preview()
        {
            await New("15 · Coda", 4);
            var part = Pattern.Parts.Single();
            var preview = Id("Beat.Melody." + part.Id);
            Require(preview.Bounds.Width > 600, "Rich Rack preview collapsed after fixing pads.");
            var before = Workflow.State.Snapshot.Revision;
            await CaptureAsync(main, output, "native-rich-rack");
            await _input.Click(preview);
            Require(
                Piano.Canvas.Part?.Id == part.Id && Piano.Canvas.Part.Notes.Length == 16 &&
                Workflow.State.Snapshot.Revision == before,
                "Rich preview navigation changed musical data.");
        }

        internal async Task Arrange()
        {
            await Pane(WorkspaceState.ArrangementId);
            string[] names =
                ["Pulse", "Backbeat", "Cymbals", "Wood", "Transitions", "Bass", "Keys", "Seeds", "Air / sparks"];
            for (int lane = 0; lane < names.Length; lane++)
            {
                await Click("Arrangement.Tracks");
                await _input.Select(Field<ComboBox>(Playlist, "_trackPicker"), lane);
                await _input.Exact(Field<TextBox>(Playlist, "_trackName"), names[lane]);
                await _input.Click(((StackPanel)Field<Flyout>(Playlist, "_trackFlyout").Content!).GetVisualDescendants()
                    .OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Arrangement.ApplyTrack"));
                await Playlist.PendingCommand;
            }

            (int Number, int Start, int Span, int Lane)[] sections =
            [
                (10, 0, 4, 6), (6, 2, 2, 3), (4, 2, 2, 2), (14, 2, 2, 8),
                (1, 4, 8, 0), (3, 4, 8, 1), (4, 4, 8, 2), (6, 4, 8, 3), (8, 4, 8, 5), (10, 4, 8, 6), (12, 8, 4, 7),
                (7, 11, 1, 4),
                (11, 12, 4, 6), (13, 12, 4, 8), (5, 12, 4, 2), (6, 12, 4, 3), (14, 14, 2, 4),
                (2, 16, 8, 0), (3, 16, 8, 1), (5, 16, 8, 2), (6, 16, 8, 3), (9, 16, 8, 5), (11, 16, 8, 6),
                (12, 16, 8, 7), (7, 23, 1, 4),
                (1, 24, 4, 0), (3, 24, 4, 1), (4, 24, 4, 2), (6, 24, 4, 3), (8, 24, 4, 5), (10, 24, 4, 6),
                (13, 24, 4, 8), (15, 28, 4, 6), (6, 28, 2, 3), (4, 28, 2, 2), (1, 28, 2, 0)
            ];
            foreach (var section in sections)
            {
                var pattern = State.Patterns.Single(p =>
                    p.Name.StartsWith(section.Number.ToString("D2") + " ·", StringComparison.Ordinal));
                var placement = State.Placements.Single(p => p.PatternId == pattern.Id);
                var attachment = State.GraphAttachments.Single(a => a.ContextId == placement.ItemContextId);
                await _input.Select(Field<ComboBox>(Playlist, "_source"),
                    Workflow.State.Occurrences.Select(o => o.Id).ToList().IndexOf(attachment.Id));
                await Playlist.PendingCommand;
                int bars = (int)(pattern.Length.Ticks / BeatSteps.BarTicks);
                for (int bar = section.Start; bar < section.Start + section.Span; bar += bars)
                {
                    long ticks = bar * BeatSteps.BarTicks;
                    if (State.ArrangementClips.Any(c =>
                            c.SourcePlacementId == placement.Id && c.Position.Ticks == ticks && c.Lane == section.Lane))
                        continue;
                    var canvas = Playlist.Canvas;
                    var scroll = Field<ScrollViewer>(Playlist, "_scroll");
                    double x = canvas.HeaderSize + (ticks + BeatSteps.BarTicks * .06) * canvas.TickPixels;
                    double y = HostVisual.Metric(canvas, "Playlist.RulerHeight") +
                               (section.Lane + .5) * HostVisual.Metric(canvas, "Playlist.LaneHeight");
                    for (int attempt = 0; attempt < 60; attempt++)
                    {
                        var visible = canvas.TranslatePoint(new(x, y), scroll)!.Value;
                        if (visible.X >= canvas.HeaderSize && visible.X <= scroll.Viewport.Width - 20 &&
                            visible.Y >= 40 && visible.Y <= scroll.Viewport.Height - 20) break;
                        if (visible.X < canvas.HeaderSize || visible.X > scroll.Viewport.Width - 20)
                            await _input.Wheel(scroll, new(scroll.Bounds.Width / 2, scroll.Bounds.Height / 2),
                                visible.X < canvas.HeaderSize ? -720 : 720, true);
                        if (visible.Y > scroll.Viewport.Height - 20 || visible.Y < 40)
                            await _input.Wheel(scroll, new(scroll.Bounds.Width / 2, scroll.Bounds.Height / 2),
                                visible.Y < 40 ? 360 : -360);
                    }

                    int count = State.ArrangementClips.Length;
                    await _input.Click(canvas, new(x, y));
                    await Playlist.PendingCommand;
                    Require(State.ArrangementClips.Length == count + 1,
                        "Native placement failed at " + bar + "/" + section.Lane);
                }

                await Save();
            }

            await Click("Arrangement.Fit");
            Require(
                State.Patterns.Length >= 15 && State.ArrangementTracks.Length == 9 &&
                ArrangementPreparation.EndTicks(State) == 32 * BeatSteps.BarTicks, "Composition structure incomplete.");
            await Save();
        }

        internal async Task SeedsForte()
        {
            await New("12 · Glass seeds", 4);
            var old = Pattern.Parts.Single(p => p.Name == "Pizz");
            var score = old.Notes.Select(n => ((decimal)n.Position.Ticks / MusicalPosition.TicksPerQuarter,
                (int)n.Pitch, (decimal)n.Duration.Ticks / MusicalPosition.TicksPerQuarter)).ToArray();
            var before = await ExportSeeds("forte-before");
            await Sample("Pizz forte", 57);
            await Notes(score, 85);
            await Pane(WorkspaceState.BeatId);
            await Click("Beat.Actions." + old.Id);
            await _input.Click(((Button)Id("Beat.Actions." + old.Id)).ContextMenu!.Items.OfType<MenuItem>()
                .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.DeleteChannel." + old.Id));
            await Rack.PendingCommand;
            await Save();
            var after = await ExportSeeds("forte-after");
            Require(
                Pattern.Parts.Length == 1 && Pattern.Parts.Single().Notes.Length == 16 &&
                after.Peak > before.Peak * 10 && after.Peak < .95,
                "Forte pizzicato replacement did not preserve/strengthen the actual musical part.");
            Json(output, "glass-seeds-forte",
                new
                {
                    Before = new { before.Peak, before.Energy }, After = new { after.Peak, after.Energy },
                    Source = _sources["Pizz forte"], Gain = .9, Velocity = 85
                });
            await Click("Workflow.Play");
            await Task.Delay(5000);
            await Click("Workflow.Stop");
        }

        internal async Task Seeds()
        {
            await New("12 · Glass seeds", 4);
            var before = await ExportSeeds("before");
            using var original = WavDecoder.Decode(File.ReadAllBytes(_sources["Pizz"]));
            double sourcePeak = 0;
            int first = -1;
            for (int f = 0; f < original.Frames; f++)
            for (int c = 0; c < original.Channels; c++)
            {
                double v = Math.Abs(original.Sample(f, c));
                sourcePeak = Math.Max(sourcePeak, v);
                if (first < 0 && v > .005) first = f;
            }

            await _input.Exact(Field<NumericUpDown>(Field<WorkflowControls>(main, "_workflowControls"), "_gain"),
                "0.90");
            await Field<WorkflowControls>(main, "_workflowControls").PendingGainCommand;
            await Save();
            await PianoPart(Pattern.Parts.Single().Id);
            await Click("Piano.Select");
            _input.Key(0x41, modifier: 0x11);
            await Task.Delay(100);
            await Click("Piano.Properties");
            await _input.Exact(Field<NumericUpDown>(Piano, "_noteVelocity"), "75");
            await _input.Click(((StackPanel)Field<Flyout>(Piano, "_noteFlyout").Content!).GetVisualDescendants()
                .OfType<Button>().Single(b => AutomationProperties.GetAutomationId(b) == "Piano.Apply"));
            await Piano.PendingCommand;
            await Save();
            await Click("Piano.Play");
            await Task.Delay(5000);
            await Click("Piano.Stop");
            var after = await ExportSeeds("after");
            Require(after.Peak > before.Peak * 3 && after.Peak < .95,
                "Seeds improvement did not reach the actual export.");
            Json(output, "glass-seeds",
                new
                {
                    SourcePeak = sourcePeak, FirstSignalSeconds = first / (double)original.SampleRate, Before = before,
                    After = after, Gain = .90, Velocity = 75, Notes = Pattern.Parts.Single().Notes.Length
                });
        }

        private async Task<(double Peak, double Energy)> ExportSeeds(string stage)
        {
            await Pane(WorkspaceState.BeatId);
            string wave = Path.Combine(Path.GetDirectoryName(project)!, "Glass Seeds - " + stage + ".wav");
            await Click("Workflow.More");
            await _input.Click(
                Field<Dictionary<string, Button>>(Field<WorkflowControls>(main, "_workflowControls"), "_buttons")[
                    "ExportWave"]);
            await _input.FileDialog(wave);
            await Wait(() => File.Exists(wave) && !Workflow.State.Busy, "Native Seeds Pattern export failed.");
            using var pcm = WavDecoder.Decode(File.ReadAllBytes(wave));
            double peak = 0, energy = 0;
            for (int f = 0; f < pcm.Frames; f++)
            for (int c = 0; c < pcm.Channels; c++)
            {
                double v = pcm.Sample(f, c);
                peak = Math.Max(peak, Math.Abs(v));
                energy += v * v;
            }

            return (peak, energy);
        }

        internal async Task Balance()
        {
            foreach (var (number, gain) in new (int, decimal)[]
                     {
                         (1, .32m), (2, .32m), (3, .32m), (4, .30m), (5, .30m), (6, .32m), (7, .32m),
                         (8, .34m), (9, .34m), (10, .45m), (11, .45m), (12, .40m), (13, .48m), (14, .36m), (15, .45m)
                     })
            {
                var pattern = State.Patterns.Single(p =>
                    p.Name.StartsWith(number.ToString("D2") + " ·", StringComparison.Ordinal));
                await New(pattern.Name, (int)(pattern.Length.Ticks / BeatSteps.BarTicks));
                await _input.Exact(Field<NumericUpDown>(Field<WorkflowControls>(main, "_workflowControls"), "_gain"),
                    gain.ToString(System.Globalization.CultureInfo.InvariantCulture));
                await Field<WorkflowControls>(main, "_workflowControls").PendingGainCommand;
                Require(Workflow.State.Gain == gain, "Native occurrence balance failed.");
                await Save();
            }

            await Pane(WorkspaceState.ArrangementId);
            await Click("Arrangement.Fit");
            var navigator = main.GetVisualDescendants().OfType<PatternNavigator>().Single();
            var scroll = navigator.GetVisualDescendants().OfType<ScrollViewer>().Single();
            foreach (var theme in new[] { HostTheme.Dark, HostTheme.Light })
            {
                if (Field<ShellSession>(main, "_session").Preferences.Theme != theme) await Click("Preference.Theme");
                await Task.Delay(120);
                var bar = scroll.GetVisualDescendants().OfType<ScrollBar>()
                    .Single(b => b.Orientation == Avalonia.Layout.Orientation.Vertical);
                double edge = bar.TranslatePoint(default, scroll)!.Value.X;
                Require(navigator.GetVisualDescendants().OfType<Button>().Where(b =>
                            AutomationProperties.GetAutomationId(b)
                                ?.StartsWith("Navigator.Pattern.", StringComparison.Ordinal) == true)
                        .All(b => b.TranslatePoint(new(b.Bounds.Width, 0), scroll)!.Value.X <= edge - 2),
                    "Navigator scrollbar overlaps pattern cards.");
                await CaptureAsync(main, output, "balanced-playlist-" + theme);
            }

            await Click("Preference.Theme");
        }

        internal async Task PlayAndExport()
        {
            await Pane(WorkspaceState.ArrangementId);
            await Click("Arrangement.Fit");
            await Click("Arrangement.Start");
            await Click("Arrangement.Play");
            await Playlist.PendingCommand;
            await Wait(() => Workflow.State.Execution?.Execution.Playing == true,
                "Native Song startup observation did not settle: " + Workflow.State.Detail);
            Require(Workflow.State.Execution?.Execution.Playing == true,
                "Native Song did not start: " + Workflow.State.Detail);
            int voices = 0;
            for (int i = 0;
                 i < 2400 && Workflow.State.Execution?.Execution.Transport != SamplerTransportState.Ended;
                 i++)
            {
                await Task.Delay(40);
                voices = Math.Max(voices, Workflow.State.Execution?.Execution.Voices ?? 0);
                Require(Workflow.State.Execution?.Blocker is null && Workflow.State.StatusKey != "Workflow.DeviceFault",
                    "Song blocked: " + Workflow.State.Detail);
            }

            Require(Workflow.State.Execution?.Execution.Transport == SamplerTransportState.Ended,
                "Whole Song did not end.");
            await Click("Arrangement.Stop");
            string wave = Path.Combine(Path.GetDirectoryName(project)!, "Neon Orchard.wav");
            await Click("Arrangement.Export");
            await _input.FileDialog(wave);
            await Playlist.PendingCommand;
            await Wait(() => File.Exists(wave) && !Workflow.State.Busy, "Native Song export failed.");
            using var pcm = WavDecoder.Decode(File.ReadAllBytes(wave));
            double peak = 0, energy = 0;
            int clipped = 0;
            for (int f = 0; f < pcm.Frames; f++)
            for (int ch = 0; ch < pcm.Channels; ch++)
            {
                double v = pcm.Sample(f, ch);
                peak = Math.Max(peak, Math.Abs(v));
                energy += v * v;
                if (Math.Abs(v) >= .9999) clipped++;
            }

            Require(pcm.Frames / (double)pcm.SampleRate is >= 60 and <= 120 && energy > 1 && clipped == 0,
                "Export duration/signal/clipping failed.");
            Json(output, "neon-orchard", new
            {
                Name = "Neon Orchard", Bpm = 112, Seconds = pcm.Frames / (double)pcm.SampleRate,
                Patterns = State.Patterns.Length, Tracks = State.ArrangementTracks.Length,
                Clips = State.ArrangementClips.Length,
                Peak = peak, Energy = energy, ClippedSamples = clipped, PeakVoices = voices, NativeAuthoring = true,
                NativeSaveOpenExport = true, PhysicalWholeSong = true, SubjectiveListening = false, Project = project,
                Wave = wave, Samples = _sources, OriginalHashes = _hashes
            });
        }

        internal void VerifyLibrary()
        {
            foreach (var pair in _hashes)
                Require(pair.Value == Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(pair.Key))),
                    "Library original changed.");
        }
    }
}
