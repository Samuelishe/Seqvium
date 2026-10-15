// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Threading;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Project navigation from actual occurrences, with canonical rhythm thumbnails.</summary>
internal sealed class PatternNavigator : Grid, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly WorkspaceHost _workspace;
    private readonly Func<Id<GraphAttachment>, string?, Id<MusicalPart>?, Task> _openEditor;
    private readonly TextBlock _title = new() { Classes = { "caption" } };
    private readonly Button _create = new() { Classes = { "command" } };
    private readonly StackPanel _items = new() { Classes = { "compact" } };
    private readonly Dictionary<Id<GraphAttachment>, Button> _choices = [];
    private bool _disposed;
    private Id<GraphAttachment>? _selecting;
    private Task _selectionWork = Task.CompletedTask;

    private (Guid Lifecycle, long Generation, Id<GraphAttachment>? Selected, bool Busy, bool Closed, HostPreferences
        Preferences)? _rendered;

    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    internal PatternNavigator(ShellSession shell, DesktopWorkflow workflow, WorkspaceHost workspace,
        Func<Id<GraphAttachment>, string?, Id<MusicalPart>?, Task> openEditor)
    {
        _shell = shell;
        _workflow = workflow;
        _workspace = workspace;
        _openEditor = openEditor;
        Width = 240;
        RowDefinitions = new("Auto,*");
        Margin = new(10, 12, 10, 8);
        var header = new Grid { ColumnDefinitions = new("*,Auto") };
        _title.VerticalAlignment = VerticalAlignment.Center;
        header.Children.Add(_title);
        _create.Content = HostIcons.Create("Add", _create, 16);
        SetColumn(_create, 1);
        header.Children.Add(_create);
        Children.Add(header);
        AutomationProperties.SetAutomationId(_create, "Navigator.Create");
        _create.Click += async (_, _) =>
        {
            var state = workflow.State;
            PendingCommand =
                workflow.CreateBeatAsync(state.Document.LifecycleId, state.Generation, shell["Beat.DefaultName"]);
            await PendingCommand;
            if (!_disposed)
            {
                workspace.Open(WorkspaceState.BeatId);
                Refresh();
            }
        };
        _items.Bind(MarginProperty, this.GetResourceObservable("Navigation.ScrollGutter"));
        var scroll = new ScrollViewer
        {
            Content = _items, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Margin = new(0, 10, 0, 0)
        };
        SetRow(scroll, 1);
        Children.Add(scroll);
        workflow.Changed += WorkflowChanged;
        shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }

    private void WorkflowChanged(WorkflowState state) => Dispatcher.UIThread.Post(() =>
    {
        if (!_disposed) Refresh();
    });

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args) => Refresh();

    private void Refresh()
    {
        if (_disposed) return;
        var observation = _workflow.State;
        var state = observation.Snapshot.State;
        var basis = (observation.Document.LifecycleId, observation.Generation, observation.Selected, observation.Busy,
            observation.IsClosed, _shell.Preferences);
        if (_rendered == basis) return;
        _rendered = basis;
        _title.Text = _shell["Navigator.Patterns"];
        ToolTip.SetTip(_create, _shell["Beat.Create"]);
        AutomationProperties.SetName(_create, _shell["Beat.Create"]);
        _create.IsEnabled = !observation.Busy && !observation.IsClosed &&
                            state.Settings.Meter is { Numerator: 4, Denominator: 4 };
        foreach (var old in _choices.Keys.Except(observation.Occurrences.Select(c => c.Id)).ToArray())
        {
            _items.Children.Remove(_choices[old]);
            _choices.Remove(old);
        }

        int index = 0;
        foreach (var choice in observation.Occurrences)
        {
            var attachment = state.GraphAttachments.Single(a => a.Id == choice.Id);
            var placement = state.Placements.FirstOrDefault(p => p.ItemContextId == attachment.ContextId);
            var pattern = state.Patterns.FirstOrDefault(p => p.Id == placement?.PatternId);
            if (!_choices.TryGetValue(choice.Id, out var button))
            {
                button = new Button
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    MinHeight = 62, Padding = new(7, 4)
                };
                var id = choice.Id;
                AutomationProperties.SetAutomationId(button, "Navigator.Pattern." + id);
                button.Click += async (_, _) =>
                {
                    if (_selecting is not null) return;
                    _selecting = id;
                    try
                    {
                        PendingCommand = _selectionWork = _workflow.SelectAsync(id);
                        await PendingCommand;
                    }
                    finally { _selecting = null; _rendered = null; if (!_disposed) Refresh(); }
                };
                button.DoubleTapped += async (_, e) =>
                {
                    e.Handled = true;
                    PendingCommand = OpenEditorAsync(id);
                    await PendingCommand;
                };
                button.KeyDown += async (_, e) =>
                {
                    if (e.Key != Key.Enter || e.KeyModifiers != KeyModifiers.None) return;
                    e.Handled = true;
                    PendingCommand = OpenEditorAsync(id);
                    await PendingCommand;
                };
                button.ContextRequested += (_, e) =>
                {
                    e.Handled = true;
                    OpenMenu(button, id);
                };
                _choices.Add(choice.Id, button);
                _items.Children.Add(button);
            }

            // Keep the clicked card and its visual target alive through async selection so the
            // second native press can complete DoubleTapped. Other busy commands stay gated.
            button.IsEnabled = (!observation.Busy || _selecting == choice.Id) && !observation.IsClosed;
            button.Classes.Set("primary", observation.Selected == choice.Id);
            var contentBasis = (pattern, choice.Label, index);
            if (Equals(button.Tag, contentBasis)) { index++; continue; }
            button.Tag = contentBasis;
            var body = new Grid { RowDefinitions = new("Auto,*"), RowSpacing = 3 };
            body.Children.Add(new TextBlock
            {
                Text = pattern?.VariationOf is not null ? "↗ " + pattern.Name : pattern?.Name ?? choice.Label, FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.Wrap, MaxLines = 2, TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (pattern is not null)
            {
                var preview = new PatternThumbnail(_shell, pattern, index % 8) { Height = 17 };
                SetRow(preview, 1);
                body.Children.Add(preview);
            }

            button.Content = body;
            AutomationProperties.SetName(button, pattern?.Name ?? choice.Label);
            ToolTip.SetTip(button, choice.Label + "\n" + _shell["Navigator.Hint"]);
            index++;
        }
    }

    private async Task OpenEditorAsync(Id<GraphAttachment> target)
    {
        var basis = _workflow.State;
        await _selectionWork;
        var current = _workflow.State;
        if (_disposed || current.Document.LifecycleId != basis.Document.LifecycleId ||
            current.Generation != basis.Generation) return;
        await _openEditor(target, null, null);
    }

    private void OpenMenu(Button button, Id<GraphAttachment> id)
    {
        var basis = _workflow.State;
        var state = basis.Snapshot.State;
        var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == id);
        var placement = state.Placements.SingleOrDefault(p => p.ItemContextId == attachment?.ContextId);
        var pattern = state.Patterns.SingleOrDefault(p => p.Id == placement?.PatternId);
        if (basis.Busy || basis.IsClosed || pattern is null) return;
        var menu = new ContextMenu();
        MenuItem Item(string label, string? pane, Id<MusicalPart>? part = null)
        {
            var item = new MenuItem { Header = label };
            item.Click += async (_, _) =>
            {
                var current = _workflow.State;
                if (_disposed || current.Document.LifecycleId != basis.Document.LifecycleId ||
                    current.Generation != basis.Generation || current.Busy) return;
                PendingCommand = _openEditor(id, pane, part);
                await PendingCommand;
            };
            return item;
        }

        menu.Items.Add(Item(_shell["Navigator.Edit"], null));
        menu.Items.Add(Item(_shell["Pane.Beat"], WorkspaceState.BeatId));
        var piano = new MenuItem { Header = _shell["Piano.Open"] };
        foreach (var part in pattern.Parts)
        {
            var item = Item(part.Name, WorkspaceState.PianoId, part.Id);
            item.IsEnabled = PianoNotes.Refusal(state, pattern, part) is null;
            piano.Items.Add(item);
        }
        piano.IsEnabled = piano.Items.Cast<MenuItem>().Any(i => i.IsEnabled);
        menu.Items.Add(piano);
        button.ContextMenu = menu;
        menu.Open(button);
    }

    public void Dispose()
    {
        _disposed = true;
        _workflow.Changed -= WorkflowChanged;
        _shell.PropertyChanged -= PreferencesChanged;
    }
}

internal sealed class PatternThumbnail(ShellSession shell, Pattern pattern, int color) : Control
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || pattern.Length.Ticks <= 0) return;
        var surface = HostVisual.Brush(this, shell.Preferences.Theme, "Surface.Workspace");
        context.DrawRectangle(surface, null, new Rect(Bounds.Size), 2, 2);
        var grid = new Pen(HostVisual.Brush(this, shell.Preferences.Theme, "Graph.Grid"), 1);
        for (int i = 1; i < 4; i++)
            context.DrawLine(grid, new(i * Bounds.Width / 4, 0), new(i * Bounds.Width / 4, Bounds.Height));
        double height = Bounds.Height / Math.Max(1, pattern.Parts.Length);
        for (int row = 0; row < pattern.Parts.Length; row++)
            foreach (var note in pattern.Parts[row].Notes)
            {
                double x = Math.Clamp((double)note.Position.Ticks / pattern.Length.Ticks, 0, 1) * Bounds.Width;
                double width = Math.Min(Bounds.Width - x, Math.Max(2, Bounds.Width / 32));
                context.DrawRectangle(HostVisual.Brush(this, shell.Preferences.Theme, $"Rack.Channel{color}"), null,
                    new Rect(x, row * height + 1, width, Math.Max(1, height - 2)), 1, 1);
            }
    }
}
