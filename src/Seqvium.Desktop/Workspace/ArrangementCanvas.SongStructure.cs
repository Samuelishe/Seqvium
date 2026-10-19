// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class ArrangementCanvas
{
    private (Guid Lifecycle, ArrangementClip[] Patterns, AudioClip[] Audio)? _clipboard;
    private int _pasteLane;
    internal event Action<Id<ArrangementSection>?>? SectionPropertiesRequested;
    internal event Action? ClipPropertiesRequested;
    internal Func<Id<ArrangementSection>, string, Task>? SectionCommandRequested { get; set; }
    internal Id<ArrangementSection>? SelectedSection { get; set; }
    internal bool HasClipboard => _clipboard is { } c && c.Lifecycle == _observation.Document.LifecycleId;

    internal void SelectBoundaryConflict(PlaylistBoundaryConflict conflict)
    {
        Cancel(); _selection.Clear(); _audioSelection.Clear();
        Selected = conflict.PatternClip; SelectedAudio = conflict.AudioClip;
        if (Selected is { } pattern) _selection.Add(pattern);
        if (SelectedAudio is { } audio) _audioSelection.Add(audio);
        SelectionChanged?.Invoke(); InvalidateVisual(); Focus();
    }

    internal Rect SectionRect(ArrangementSection section) => new(HeaderWidth + section.Position.Ticks * TickPixels,
        RulerY + BeatRulerHeight, section.Length.Ticks * TickPixels, SectionHeight);

    private ArrangementSection? SectionAt(Point point) => point.X < HeaderX + HeaderWidth ? null :
        _observation.Snapshot.State.ArrangementSections.FirstOrDefault(s => SectionRect(s).Contains(point));

    private void DrawSections(DrawingContext context)
    {
        IBrush Brush(string key) => HostVisual.Brush(this, _shell.Preferences.Theme, key);
        double y = RulerY + BeatRulerHeight;
        context.DrawRectangle(Brush("Surface.Workspace"), null, new Rect(HeaderX, y, Bounds.Width - HeaderX, SectionHeight));
        using (context.PushClip(new Rect(HeaderX + HeaderWidth, y, Math.Max(0, Bounds.Width - HeaderX - HeaderWidth), SectionHeight)))
            foreach (var section in _observation.Snapshot.State.ArrangementSections)
            {
                var rect = SectionRect(section);
                if (_viewport.Width > 0 && rect.Right < _viewport.Left || rect.X > (_viewport.Width > 0 ? _viewport.Right : Bounds.Width)) continue;
                using (context.PushOpacity(.18)) context.DrawRectangle(Brush($"Rack.Channel{section.ColorIndex}"), null, rect);
                context.DrawRectangle(Brush($"Rack.Channel{section.ColorIndex}"), null, new Rect(rect.X, rect.Y, rect.Width, 3));
                context.DrawRectangle(null, new Pen(Brush(section.Id == SelectedSection ? "Accent.Action" : "Border.Default"), 1), rect.Deflate(.5));
                double left = Math.Max(rect.Left, HeaderX + HeaderWidth);
                using (context.PushClip(new Rect(left, y, Math.Max(0, rect.Right - left - 4), SectionHeight)))
                    HostVisual.Caption(this, _shell, context, section.Name, new(left + 8, y + 7));
            }
        context.DrawRectangle(Brush("Material.Panel"), null, new Rect(HeaderX, y, HeaderWidth, SectionHeight));
        HostVisual.Caption(this, _shell, context, "+ " + _shell["Arrangement.Sections"], new(HeaderX + 12, y + 7));
    }

    private bool SectionPressed(PointerPressedEventArgs e, Point point)
    {
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed || point.Y < RulerY + BeatRulerHeight ||
            point.Y >= RulerY + RulerHeight) return false;
        e.Handled = true;
        if (point.X < HeaderX + HeaderWidth || SectionAt(point) is not { } section)
            SectionPropertiesRequested?.Invoke(null);
        else
        {
            SelectedSection = section.Id;
            SelectionChanged?.Invoke();
            if (e.ClickCount == 2) SectionPropertiesRequested?.Invoke(section.Id);
            else CommandSubmitted?.Invoke(NavigateSectionAsync(section));
            InvalidateVisual();
        }
        return true;
    }

    private async Task NavigateSectionAsync(ArrangementSection section)
    {
        await _workflow.SetSongLoopAsync(section.Position, new(section.Position.Ticks + section.Length.Ticks), false);
        await _workflow.SeekSongAsync(section.Position);
    }

    internal void SelectSectionClips(Id<ArrangementSection> id)
    {
        Cancel();
        var state = _observation.Snapshot.State;
        var section = state.ArrangementSections.Single(s => s.Id == id);
        long end = section.Position.Ticks + section.Length.Ticks;
        _selection.Clear(); _audioSelection.Clear();
        foreach (var c in state.ArrangementClips.Where(c => c.Position.Ticks >= section.Position.Ticks && PlaylistEditing.EndTicks(state, c) <= end)) _selection.Add(c.Id);
        foreach (var c in state.AudioClips.Where(c => c.Position.Ticks >= section.Position.Ticks && AudioClipSource.EndTicks(state, c) <= end)) _audioSelection.Add(c.Id);
        Selected = _selection.Select(i => (Id<ArrangementClip>?)i).FirstOrDefault();
        SelectedAudio = _audioSelection.Select(i => (Id<AudioClip>?)i).FirstOrDefault();
        SelectedSection = id;
        SelectionChanged?.Invoke(); InvalidateVisual();
    }

    private void SetGroupDelta(long ticks, int lanes)
    {
        var starts = _group.Select(c => c.Position.Ticks).Concat(_audioGroup.Select(c => c.Position.Ticks)).ToArray();
        var rows = _group.Select(c => c.Lane).Concat(_audioGroup.Select(c => c.Lane)).ToArray();
        _tickDelta = Math.Max(ticks, -starts.Min());
        _laneDelta = Math.Clamp(lanes, -rows.Min(), 31 - rows.Max());
    }

    internal async Task EditSelection(bool duplicate = false, bool delete = false)
    {
        var before = _observation.Snapshot.State;
        if (await _workflow.EditPlaylistSelectionAsync(Basis, Selection, AudioSelection, delete: delete, duplicate: duplicate)
            && duplicate) SelectNewClips(before);
    }

    private async Task CommitGroupAsync(ArrangementEditBasis basis, Id<ArrangementClip>[] patterns, Id<AudioClip>[] audio,
        long ticks, int lanes, bool copy)
    {
        var before = _observation.Snapshot.State;
        if (await _workflow.EditPlaylistSelectionAsync(basis, patterns, audio, ticks, lanes, copy) && copy)
            SelectNewClips(before);
    }

    internal Task CopySelection(bool cut = false)
    {
        var state = _observation.Snapshot.State;
        var patterns = state.ArrangementClips.Where(c => _selection.Contains(c.Id)).ToArray();
        var audio = state.AudioClips.Where(c => _audioSelection.Contains(c.Id)).ToArray();
        if (patterns.Length + audio.Length == 0) return Task.CompletedTask;
        _clipboard = (_observation.Document.LifecycleId, patterns, audio);
        SelectionChanged?.Invoke();
        return cut ? EditSelection(delete: true) : Task.CompletedTask;
    }

    internal async Task PasteSelection()
    {
        if (_clipboard is not { } c || !HasClipboard) return;
        var before = _workflow.State.Snapshot.State;
        if (!await _workflow.PastePlaylistAsync(Basis, c.Patterns, c.Audio, PositionTicks, _pasteLane)) return;
        SelectNewClips(before);
    }

    private void SelectNewClips(ProjectState before)
    {
        _observation = _workflow.State;
        var after = _observation.Snapshot.State;
        _selection.Clear(); _audioSelection.Clear();
        foreach (var c in after.ArrangementClips.Where(c => !before.ArrangementClips.Any(o => o.Id == c.Id))) _selection.Add(c.Id);
        foreach (var c in after.AudioClips.Where(c => !before.AudioClips.Any(o => o.Id == c.Id))) _audioSelection.Add(c.Id);
        Selected = _selection.Select(i => (Id<ArrangementClip>?)i).FirstOrDefault();
        SelectedAudio = _audioSelection.Select(i => (Id<AudioClip>?)i).FirstOrDefault();
        SelectionChanged?.Invoke(); InvalidateVisual();
    }

    internal async Task MakeVariation()
    {
        if (Selected is not { } id || Selection.Length != 1 || AudioSelection.Length != 0) return;
        var source = _observation.Snapshot.State.ArrangementClips.Single(c => c.Id == id);
        if (!await _workflow.MakeArrangementVariationAsync(Basis, id, PatternFor(source).Name + " · " + _shell["Arrangement.VariationName"])) return;
        _observation = _workflow.State;
        SelectionChanged?.Invoke();
        var state = _observation.Snapshot.State;
        var placement = state.Placements.Single(p => p.Id == state.ArrangementClips.Single(c => c.Id == id).SourcePlacementId);
        if (state.GraphAttachments.SingleOrDefault(a => a.ContextId == placement.ItemContextId) is { } graph)
            RackRequested?.Invoke(graph.Id);
    }

    internal int? RepeatAt(ArrangementClip clip, Point point)
    {
        if (clip.Repeats <= 1 || point.X < HeaderX + HeaderWidth ||
            point.Y < RulerHeight + clip.Lane * LaneHeight ||
            point.Y >= RulerHeight + (clip.Lane + 1) * LaneHeight) return null;
        long length = PatternFor(clip).Length.Ticks;
        double Boundary(int i) => HeaderWidth + checked(clip.Position.Ticks + length * i) * TickPixels;
        // Half-open time bounds, independent of the padded/minimum-width painted clip body.
        if (point.X < Boundary(0) || point.X >= Boundary(clip.Repeats)) return null;
        int low = 0, high = clip.Repeats;
        while (high - low > 1)
        {
            int middle = (low + high) / 2;
            if (point.X < Boundary(middle)) high = middle;
            else low = middle;
        }
        return low;
    }

    private async Task MakeRepeatUnique(ArrangementEditBasis basis, ArrangementClip clip, int index)
    {
        var selection = Selection; var audio = AudioSelection; var selected = Selected;
        var id = await _workflow.MakeArrangementRepeatUniqueAsync(basis, clip.Id, index,
            PatternFor(clip).Name + " · " + _shell["Arrangement.VariationName"]);
        if (id is null || _workflow.State.Document.LifecycleId != basis.Lifecycle ||
            _workflow.State.Generation != basis.Generation + 1 || selected != Selected ||
            !selection.SequenceEqual(Selection) || !audio.SequenceEqual(AudioSelection)) return;
        _observation = _workflow.State;
        _selection.Clear(); _audioSelection.Clear(); SelectedAudio = null; SelectedSection = null;
        Selected = id; _selection.Add(id.Value);
        SelectionChanged?.Invoke(); InvalidateVisual();
        var state = _observation.Snapshot.State;
        var placement = state.Placements.Single(p => p.Id == state.ArrangementClips.Single(c => c.Id == id).SourcePlacementId);
        if (state.GraphAttachments.SingleOrDefault(a => a.ContextId == placement.ItemContextId) is { } graph)
            RackRequested?.Invoke(graph.Id);
    }

    private bool PlaylistKey(KeyEventArgs e)
    {
        bool control = e.KeyModifiers == KeyModifiers.Control;
        if (e.Key is Key.Home or Key.End)
        {
            var state = _observation.Snapshot.State;
            var ordered = state.ArrangementClips.Select(c => (c.Position.Ticks, c.Lane, Pattern: (Id<ArrangementClip>?)c.Id, Audio: (Id<AudioClip>?)null))
                .Concat(state.AudioClips.Select(c => (c.Position.Ticks, c.Lane, Pattern: (Id<ArrangementClip>?)null, Audio: (Id<AudioClip>?)c.Id)))
                .OrderBy(c => c.Ticks).ThenBy(c => c.Lane).ToArray();
            if (ordered.Length == 0) return false;
            var target = e.Key == Key.Home ? ordered[0] : ordered[^1];
            _selection.Clear(); _audioSelection.Clear(); Selected = target.Pattern; SelectedAudio = target.Audio;
            if (Selected is { } pattern) _selection.Add(pattern);
            if (SelectedAudio is { } audio) _audioSelection.Add(audio);
            _pasteLane = target.Lane;
            SelectionChanged?.Invoke(); InvalidateVisual(); e.Handled = true; return true;
        }
        if (e.Key == Key.A && control)
        {
            _selection.Clear(); _audioSelection.Clear();
            foreach (var c in _observation.Snapshot.State.ArrangementClips) _selection.Add(c.Id);
            foreach (var c in _observation.Snapshot.State.AudioClips) _audioSelection.Add(c.Id);
            Selected = _selection.Select(i => (Id<ArrangementClip>?)i).FirstOrDefault();
            SelectedAudio = _audioSelection.Select(i => (Id<AudioClip>?)i).FirstOrDefault();
            SelectionChanged?.Invoke(); InvalidateVisual(); e.Handled = true; return true;
        }
        Task? task = e.Key switch
        {
            Key.C when control => CopySelection(), Key.X when control => CopySelection(true),
            Key.V when control => PasteSelection(),
            Key.D when control => EditSelection(duplicate: true), Key.Delete => EditSelection(delete: true),
            Key.Left or Key.Right or Key.Up or Key.Down when _selection.Count + _audioSelection.Count > 0 =>
                _workflow.EditPlaylistSelectionAsync(Basis, Selection, AudioSelection,
                    e.Key == Key.Left ? -SnapTicks : e.Key == Key.Right ? SnapTicks : 0,
                    e.Key == Key.Up ? -1 : e.Key == Key.Down ? 1 : 0),
            _ => null
        };
        if (task is null) return false;
        Cancel(); e.Handled = true; CommandSubmitted?.Invoke(task); return true;
    }

    private void OpenContextMenu(Point point)
    {
        Cancel(); Focus();
        var menu = new ContextMenu();
        var basis = Basis;
        void Add(string key, Func<Task> action, bool enabled = true)
        {
            var item = new MenuItem { Header = _shell["Arrangement." + key], IsEnabled = enabled };
            AutomationProperties.SetAutomationId(item, "Arrangement.Context." + key);
            item.Click += (_, _) => { if (basis == Basis) CommandSubmitted?.Invoke(action()); };
            menu.Items.Add(item);
        }
        if (SectionAt(point) is { } section)
        {
            Add("SectionEdit", () => { SectionPropertiesRequested?.Invoke(section.Id); return Task.CompletedTask; });
            Add("SectionSelect", () => { SelectSectionClips(section.Id); return Task.CompletedTask; });
            Add("SectionLoop", () => _workflow.SetSongLoopAsync(section.Position, new(section.Position.Ticks + section.Length.Ticks), true));
            Add("SectionEarlier", () => SectionCommandRequested?.Invoke(section.Id, "Earlier") ?? Task.CompletedTask);
            Add("SectionLater", () => SectionCommandRequested?.Invoke(section.Id, "Later") ?? Task.CompletedTask);
            Add("SectionDuplicate", () => SectionCommandRequested?.Invoke(section.Id, "Duplicate") ?? Task.CompletedTask);
            Add("SectionDelete", () => _workflow.SectionCommandAsync(basis, section.Id, "Delete"));
        }
        else
        {
            var pattern = Hit(point) ?? _observation.Snapshot.State.ArrangementClips.Reverse()
                .FirstOrDefault(c => RepeatAt(c, point) is not null && point.Y >= ClipRect(c).Top && point.Y < ClipRect(c).Bottom);
            var audio = AudioHit(point);
            if (audio is not null && !_audioSelection.Contains(audio.Id) || audio is null && pattern is not null && !_selection.Contains(pattern.Id))
            {
                _selection.Clear(); _audioSelection.Clear(); Selected = null; SelectedAudio = null;
                if (audio is not null) { _audioSelection.Add(audio.Id); SelectedAudio = audio.Id; }
                else if (pattern is not null) { _selection.Add(pattern.Id); Selected = pattern.Id; }
                SelectionChanged?.Invoke(); InvalidateVisual();
            }
            _pasteLane = Math.Clamp((int)((point.Y - RulerHeight) / LaneHeight), 0, 31);
            bool selected = _selection.Count + _audioSelection.Count > 0;
            Add("Properties", () => { ClipPropertiesRequested?.Invoke(); return Task.CompletedTask; }, selected);
            Add("Variation", MakeVariation, _selection.Count == 1 && _audioSelection.Count == 0);
            if (audio is null && pattern is not null && RepeatAt(pattern, point) is { } repeat)
            {
                var patterns = Selection; var audios = AudioSelection; var selectedId = Selected;
                Add("RepeatUnique", () => patterns.SequenceEqual(Selection) && audios.SequenceEqual(AudioSelection) &&
                    selectedId == Selected && IsEffectivelyVisible && TopLevel.GetTopLevel(this) is not null
                    ? MakeRepeatUnique(basis, pattern, repeat) : Task.CompletedTask);
                ToolTip.SetTip((MenuItem)menu.Items[^1]!, _shell["Arrangement.RepeatUniqueHint"]);
            }
            Add("Copy", () => CopySelection(), selected);
            Add("Cut", () => CopySelection(true), selected);
            Add("Paste", PasteSelection, HasClipboard);
            Add("Duplicate", () => EditSelection(duplicate: true), selected);
            Add("Delete", () => EditSelection(delete: true), selected);
            Add("SectionAdd", () => { SectionPropertiesRequested?.Invoke(null); return Task.CompletedTask; });
        }
        ContextMenu = menu;
        menu.Open(this);
    }
}
