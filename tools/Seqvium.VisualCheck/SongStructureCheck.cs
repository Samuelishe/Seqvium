// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;
using Seqvium.Desktop.Workspace;

namespace Seqvium.VisualCheck;

internal static partial class Program
{
    private static async Task SongStructureAsync(MainWindow main, string output, string project, string audioSource)
    {
        Require(!project.Contains(Path.DirectorySeparatorChar + "examples" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Use an independent musical project copy outside examples.");
        var workflow = Field<DesktopWorkflow>(main, "_workflow");
        var workspace = Field<WorkspaceHost>(main, "_workspace");
        var shell = Field<ShellSession>(main, "_session");
        var input = new NativePointer.DrumInput(main);
        var passed = new List<string>();
        ArrangementPaneContent? playlist = null;

        Control Id(string id)
        {
            var controls = main.GetVisualDescendants().OfType<Control>();
            controls = controls.Concat(main.GetVisualDescendants().OfType<PianoRollPaneContent>()
                .Select(p => Field<Flyout>(p, "_noteFlyout").Content).OfType<Control>()
                .SelectMany(c => c.GetVisualDescendants().OfType<Control>().Prepend(c)));
            if (playlist is not null)
                controls = controls.Concat(new[] { playlist.SectionPropertiesContent, playlist.ClipPropertiesContent, playlist.AudioPropertiesContent }
                    .OfType<Control>().SelectMany(c => c.GetVisualDescendants().OfType<Control>().Prepend(c)));
            return controls.Distinct().Single(c => c.IsEffectivelyVisible && TopLevel.GetTopLevel(c) is not null && AutomationProperties.GetAutomationId(c) == id);
        }
        async Task Wait(Func<bool> condition, string message)
        {
            for (int i = 0; i < 400 && !condition(); i++) await Task.Delay(20);
            Require(condition(), message + " · " + workflow.State.StatusKey + " · " + workflow.State.Detail);
        }
        async Task Click(string id)
        {
            var control = Id(id);
            await Wait(() => control.IsEnabled, "Native target stayed disabled: " + id);
            await input.Click(control, allowOwnedDialog: id == "Workflow.Open" || id == "Arrangement.ImportAudio");
        }
        async Task Idle() => await Wait(() => !workflow.State.Busy, "Workflow remained busy");
        async Task Only(string paneId)
        {
            foreach (var p in workspace.State.Panes.Where(p => p.InstanceId != paneId && p.Visibility == PaneVisibility.Visible).ToArray())
            { await Click(p.InstanceId + ".open"); await Click(p.InstanceId + ".hide"); }
            await Click(paneId + ".open");
        }
        Task PlaylistOnly() => Only(WorkspaceState.ArrangementId);
        main.WindowState = WindowState.Maximized; await Task.Delay(500); main.Activate();
        await Task.Delay(350);
        await input.ActivateVisibleWindow();
        if (shell.Preferences.Language != HostLanguage.English) await Click("Preference.Language");
        await Click("Workflow.Open"); await input.FileDialog(project);
        await Wait(() => workflow.State.SavedPath == project && !workflow.State.Busy, "Independent project Open failed");
        Require(workflow.State.Execution is null, "Open started audio");
        var original = workflow.State.Snapshot.State;
        await PlaylistOnly();
        playlist = main.GetVisualDescendants().OfType<ArrangementPaneContent>().Single();
        var frame = (WorkspacePane)Id(WorkspaceState.ArrangementId);
        var bounds = workspace.State.Bounds(WorkspaceState.ArrangementId);
        await input.Drag(Field<Border>(frame, "_header"), new(120, 12), new(120 - bounds.X, 12 - bounds.Y));
        bounds = workspace.State.Bounds(WorkspaceState.ArrangementId);
        await input.Drag(Field<Border>(frame, "_corner"), new(12, 12), new(12 + workspace.State.Width - bounds.Width, 12 + workspace.State.Height - bounds.Height));
        var canvas = playlist.Canvas;
        long bar = original.Settings.Meter.TicksPerBar;

        foreach (var (name, start, length) in new[] { ("Intro", 1, 4), ("Groove", 5, 8), ("Bridge", 13, 4), ("Bloom", 17, 12), ("Coda", 29, 4) })
        {
            int count = workflow.State.Snapshot.State.ArrangementSections.Length;
            await Click("Arrangement.SectionAdd");
            await input.Exact(Id("Arrangement.Field.SectionName"), name);
            await input.Exact(Id("Arrangement.Field.StartBar"), start.ToString());
            await input.Exact(Id("Arrangement.Field.LengthBars"), length.ToString());
            await Click("Arrangement.ApplySection");
            await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.ArrangementSections.Length == count + 1, "Native section creation failed");
        }
        Require(original.Patterns.SequenceEqual(workflow.State.Snapshot.State.Patterns), "Labels edited musical content");
        passed.Add("native creation of five named ranges / reader 1.5 / silent project open");
        await Click("Arrangement.Fit");
        await CaptureAsync(main, output, "structured-original");

        await Click("Arrangement.ImportAudio"); await input.FileDialog(audioSource);
        await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == 1, "Real library import failed");
        await Click("Arrangement.AudioProperties");
        await input.Exact(Id("AudioClip.Length"), "4");
        await input.Exact(Id("AudioClip.Gain"), "-18");
        await input.Exact(Id("AudioClip.FadeIn"), "350");
        await input.Exact(Id("AudioClip.FadeOut"), "650");
        await Click("AudioClip.Apply"); await Idle();
        await Click("Arrangement.Properties");
        await input.Exact(Id("Arrangement.Field.StartBar"), "17");
        await input.Exact(Id("Arrangement.Field.Lane"), "10");
        await Click("Arrangement.ApplyProperties"); await Idle();
        var audio = workflow.State.Snapshot.State.AudioClips.Single();
        Require(audio.Position.Ticks == 16 * bar && audio.Lane == 9 && audio.GainDb == -18, "Exact audio placement failed");
        passed.Add("native real stereo library import / source fades and gain / exact musical placement");

        await input.Select((ComboBox)Id("Arrangement.Sections"), 3);
        var beforeSwap = workflow.State.Snapshot;
        await Click("Arrangement.SectionEarlier"); await Idle();
        Require(workflow.State.Snapshot.State.AudioClips.Single().Position.Ticks == 12 * bar, "Section did not move audio with Patterns");
        await Click("Workflow.Undo"); await Idle();
        Require(workflow.State.Snapshot == beforeSwap, "Section Undo lost identities");
        await Click("Workflow.Redo"); await Idle();
        await Click("Workflow.Undo"); await Idle();
        passed.Add("native section swap with both Pattern and audio / exact Undo and Redo");

        await input.Select((ComboBox)Id("Arrangement.Sections"), 1);
        var beforeRepeat = workflow.State.Snapshot.State;
        await Click("Arrangement.SectionDuplicate"); await Idle();
        var state = workflow.State.Snapshot.State;
        Require(state.ArrangementSections.Length == 6 && state.AudioClips.Single().Position.Ticks == 24 * bar, "Section insert did not shift later music");
        Require(state.Patterns.SequenceEqual(beforeRepeat.Patterns) && state.Resources.SequenceEqual(beforeRepeat.Resources), "Repeat copied source material");
        passed.Add("native insertion of an eight-bar Groove repeat / later Pattern and audio ripple / shared sources");
        await Click("Arrangement.Fit");
        var clip = state.ArrangementClips.Where(c => c.Lane == 6 && c.Position.Ticks >= 12 * bar && c.Position.Ticks < 20 * bar)
            .OrderByDescending(c => c.Position.Ticks).First();
        var rect = canvas.ClipRect(clip);
        await input.Click(canvas, new(rect.X + Math.Min(16, rect.Width / 2), rect.Y + 7));
        var source = state.Patterns.Single(p => p.Id == state.Placements.Single(p => p.Id == clip.SourcePlacementId).PatternId);
        await Click("Arrangement.Variation"); await Idle();
        var placement = workflow.State.Snapshot.State.Placements.Single(p => p.Id == workflow.State.Snapshot.State.ArrangementClips.Single(c => c.Id == clip.Id).SourcePlacementId);
        var variation = workflow.State.Snapshot.State.Patterns.Single(p => p.Id == placement.PatternId);
        Require(variation.Id != source.Id && workflow.State.Snapshot.State.ArrangementClips.Count(c => c.SourcePlacementId == placement.Id) == 1, "Variation detached more than one clip");
        await input.Exact(Id("Beat.Name"), "Orchard keys · Turnaround");
        await Click("Beat.ApplyName"); await Idle();
        await Click("Beat.Actions." + variation.Parts[0].Id);
        await input.Click(((Button)Id("Beat.Actions." + variation.Parts[0].Id)).ContextMenu!.Items.OfType<MenuItem>()
            .Single(i => AutomationProperties.GetAutomationId(i) == "Beat.Piano." + variation.Parts[0].Id));
        await Only(WorkspaceState.PianoId);
        await Task.Delay(180);
        var piano = main.GetVisualDescendants().OfType<PianoRollPaneContent>().Single();
        await Click("Piano.Fit");
        var note = piano.Canvas.Part!.Notes.OrderByDescending(n => n.Position.Ticks).ThenByDescending(n => n.Pitch).First();
        var noteRect = piano.Canvas.NoteRect(note);
        await input.Click(piano.Canvas, new(noteRect.X + Math.Min(8, noteRect.Width / 2), noteRect.Y + noteRect.Height / 2));
        await Click("Piano.Properties");
        await input.Exact(Id("Piano.NotePitch"), (note.Pitch + 12).ToString(System.Globalization.CultureInfo.InvariantCulture));
        await Click("Piano.Apply");
        await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.Patterns.Single(p => p.Id == variation.Id)
            .Parts[0].Notes.Single(n => n.Id == note.Id).Pitch == note.Pitch + 12, "Native Piano octave change did not reach canonical notes");
        Require(workflow.State.Snapshot.State.Patterns.Single(p => p.Id == variation.Id).Parts[0].Notes.Single(n => n.Id == note.Id).Pitch == note.Pitch + 12,
            "Native Piano variation edit failed");
        Require(workflow.State.Snapshot.State.Patterns.Single(p => p.Id == source.Id) == source, "Variation edited shared source");
        await CaptureAsync(main, output, "turnaround-piano");
        passed.Add("native one-clip variation / explicit Rack target / Piano octave phrase change / original notes retained");
        await PlaylistOnly(); await Click("Arrangement.Fit");

        // A real audio phrase deliberately crosses Bloom/Coda: refuse visibly and reveal its exact item.
        var crossingRect = canvas.AudioRect(workflow.State.Snapshot.State.AudioClips.Single());
        await input.Click(canvas, new(crossingRect.X + 15, crossingRect.Y + 7));
        await Click("Arrangement.Properties");
        await input.Exact(Id("Arrangement.Field.StartBar"), "36");
        await Click("Arrangement.ApplyProperties"); await Idle();
        var crossingState = workflow.State.Snapshot;
        await input.Select((ComboBox)Id("Arrangement.Sections"), 4);
        await Click("Arrangement.SectionEarlier"); await Idle();
        Require(workflow.State.Snapshot == crossingState && workflow.State.Detail?.Contains("Arrangement.CrossingSection") == true,
            "Crossing section was silently changed");
        await Click("Arrangement.ShowConflict");
        Require(canvas.SelectedAudio == audio.Id, "Crossing feedback did not reveal the exact clip");
        await CaptureAsync(main, output, "crossing-clip-feedback");
        await Click("Workflow.Undo"); await Idle(); await Click("Arrangement.Fit");
        passed.Add("native crossing refusal / visible named cause / Show crossing clip selects and navigates / no partial history");

        // Native marquee begins outside clip bodies, then moves the mixed group as one captured draft.
        state = workflow.State.Snapshot.State;
        var bloom = state.ArrangementSections.Single(s => s.Name == "Bloom");
        var audioRect = canvas.AudioRect(state.AudioClips.Single());
        var pattern = state.ArrangementClips.First(c => c.Position.Ticks == bloom.Position.Ticks && c.Lane == 7);
        var patternRect = canvas.ClipRect(pattern);
        await Click("Arrangement.Select");
        double left = Math.Min(audioRect.X, patternRect.X) - 1, right = Math.Max(audioRect.Right, patternRect.Right) + 1;
        await input.Drag(canvas, new(left, patternRect.Y - 2), new(right, audioRect.Bottom + 2));
        Require(canvas.Selection.Length > 0 && canvas.AudioSelection.Length == 1, "Mixed marquee failed");
        var beforeMove = workflow.State.Snapshot;
        await input.Drag(canvas, new(audioRect.X + 15, audioRect.Y + 7), new(audioRect.X + 15 + bar * canvas.TickPixels, audioRect.Y + 7));
        await Idle();
        Require(workflow.State.Snapshot.State.AudioClips.Single().Position.Ticks == beforeMove.State.AudioClips.Single().Position.Ticks + bar, "Mixed native move lost audio");
        await Click("Workflow.Undo"); await Idle();
        Require(workflow.State.Snapshot == beforeMove, "Mixed move Undo failed");
        await input.Click(canvas, new(audioRect.X + 15, audioRect.Y + 7));
        await input.RightClick(canvas, new(audioRect.X + 15, audioRect.Y + 7));
        await input.Click(canvas.ContextMenu!.Items.OfType<MenuItem>().Single(i =>
            AutomationProperties.GetAutomationId(i) == "Arrangement.Context.Copy"));
        Require(canvas.HasClipboard, "Native context Copy did not capture mixed material");
        input.Key(0x43, modifier: 0x11);
        input.Key(0x58, modifier: 0x11);
        await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == 0, "Native Cut did not reach canonical clips");
        Require(workflow.State.Snapshot.State.AudioClips.Length == 0, "Mixed native Cut failed");
        await input.Click(canvas, new(canvas.HeaderSize + 34 * bar * canvas.TickPixels, 10));
        input.Key(0x56, modifier: 0x11);
        await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == 1, "Native Paste did not reach canonical clips");
        Require(workflow.State.Snapshot.State.AudioClips.Length == 1 && workflow.State.Snapshot.State.AudioClips.Single().Position.Ticks >= 34 * bar,
            "Mixed native clipboard Paste failed");
        await Click("Workflow.Undo"); await Idle();
        await Click("Workflow.Undo"); await Idle();
        Require(workflow.State.Snapshot == beforeMove, "Mixed clipboard restoration failed");
        await input.Drag(canvas, new(left, patternRect.Y - 2), new(right, audioRect.Bottom + 2));
        Require(canvas.Selection.Length > 0 && canvas.AudioSelection.Length == 1, "Restored mixed marquee failed");
        await input.Click(canvas, new(audioRect.X + 15, audioRect.Y + 7));
        int patternCount = workflow.State.Snapshot.State.ArrangementClips.Length;
        input.Key(0x44, modifier: 0x11);
        await Wait(() => !workflow.State.Busy && workflow.State.Snapshot.State.AudioClips.Length == 2, "Native duplicate did not reach canonical clips");
        Require(workflow.State.Snapshot.State.AudioClips.Length == 2 && workflow.State.Snapshot.State.ArrangementClips.Length > patternCount,
            "Mixed native duplicate failed");
        await Click("Workflow.Undo"); await Idle();
        await Click("Arrangement.Select");
        passed.Add("native mixed marquee / captured group drag / clipboard Cut and Paste / mixed duplicate and exact restoration");

        for (int i = 0; i < 4; i++)
        {
            await input.Select((ComboBox)Id("Arrangement.Sections"), 1);
            await Click("Arrangement.SectionEdit");
            foreach (string key in new[] { "SectionName", "StartBar", "LengthBars" })
            {
                var field = Id("Arrangement.Field." + key);
                var row = (StackPanel)field.Parent!;
                FieldAlignment.Verify(field, row.Children.OfType<TextBlock>().Single(), row, key);
            }
            await CaptureAsync(main, output, "sections-" + shell.Preferences.Theme + "-" + shell.Preferences.Language);
            input.Key(0x1B); await Task.Delay(70);
            await Click(i % 2 == 0 ? "Preference.Language" : "Preference.Theme");
        }
        if (shell.Preferences.Language != HostLanguage.English) await Click("Preference.Language");
        passed.Add("actual Dark/Light EN/RU section forms / text-presenter alignment");
        await Click("Workflow.Save"); await Wait(() => !workflow.State.IsDirty && !workflow.State.Busy, "Independent study Save failed");
        var saved = workflow.State.Snapshot;
        Guid savedLifecycle = workflow.State.Document.LifecycleId;
        var savedBytes = ProjectPersistence.Encode(workflow.State.Document);
        await Click("Workflow.Open"); await input.FileDialog(project);
        await Wait(() => !workflow.State.Busy && workflow.State.Document.LifecycleId != savedLifecycle &&
            workflow.State.Snapshot.Revision == saved.Revision, "Study reopen failed");
        Require(savedBytes.SequenceEqual(ProjectPersistence.Encode(workflow.State.Document)), "Study reopen changed content");
        await Click("Arrangement.Fit");
        await CaptureAsync(main, output, "arrangement-study");
        Require(NativeScreen.Capture(main, Path.Combine(output, "arrangement-study.screen.png")).StartsWith("passed"), "Native arrangement pixels unavailable");
        await Click("Arrangement.Play"); await Wait(() => workflow.State.Execution?.Execution.Playing == true, "Musical study Play failed");
        await Task.Delay(2500); await Click("Arrangement.Stop"); await Idle();
        passed.Add("native saved/reopened independent study / physical Song Play and Stop");
        Json(output, "song-structure", new { Result = "passed", NativeWindowsInput = true, Project = project, Passed = passed,
            OriginalPatterns = original.Patterns.Length, Patterns = saved.State.Patterns.Length, Sections = saved.State.ArrangementSections.Length,
            Clips = saved.State.ArrangementClips.Length, Audio = saved.State.AudioClips.Length, SubjectiveListening = false });
    }
}
