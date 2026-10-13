// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
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
    private readonly TextBlock _title = new() { Classes = { "caption" } };
    private readonly Button _create = new() { Classes = { "command" } };
    private readonly StackPanel _items = new() { Classes = { "compact" } };
    private readonly Dictionary<Id<GraphAttachment>, Button> _choices = [];
    private bool _disposed;

    private (Guid Lifecycle, long Generation, Id<GraphAttachment>? Selected, bool Busy, bool Closed, HostPreferences
        Preferences)? _rendered;

    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    internal PatternNavigator(ShellSession shell, DesktopWorkflow workflow, WorkspaceHost workspace)
    {
        _shell = shell;
        _workflow = workflow;
        _workspace = workspace;
        Width = 156;
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
                    Height = 74, Padding = new(10, 8)
                };
                var id = choice.Id;
                AutomationProperties.SetAutomationId(button, "Navigator.Pattern." + id);
                button.Click += async (_, _) =>
                {
                    PendingCommand = _workflow.SelectAsync(id);
                    await PendingCommand;
                    if (!_disposed)
                    {
                        _workspace.Open(WorkspaceState.BeatId);
                        Refresh();
                    }
                };
                _choices.Add(choice.Id, button);
                _items.Children.Add(button);
            }

            button.IsEnabled = !observation.Busy && !observation.IsClosed;
            button.Classes.Set("primary", observation.Selected == choice.Id);
            var body = new Grid { RowDefinitions = new("Auto,*"), RowSpacing = 8 };
            body.Children.Add(new TextBlock
            {
                Text = pattern?.Name ?? choice.Label, FontWeight = FontWeight.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            });
            if (pattern is not null)
            {
                var preview = new PatternThumbnail(_shell, pattern, index % 8) { Height = 24 };
                SetRow(preview, 1);
                body.Children.Add(preview);
            }

            button.Content = body;
            AutomationProperties.SetName(button, pattern?.Name ?? choice.Label);
            ToolTip.SetTip(button, choice.Label);
            index++;
        }
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
