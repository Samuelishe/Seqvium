// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Retained read-only first-party document projection.</summary>
internal sealed class FirstPartyPaneContent : ScrollViewer, IDisposable
{
    private readonly ShellSession _session;
    private readonly bool _inspector;
    private readonly List<(TextBlock Label, TextBlock Value, string Key)> _rows = [];
    public bool IsDisposed { get; private set; }

    public FirstPartyPaneContent(ShellSession session, PaneDefinition definition, Func<Task> language, Func<Task> theme)
    {
        _session = session;
        _inspector = definition.InstanceId == WorkspaceState.InspectorId;
        var body = new StackPanel();
        body.Bind(StackPanel.SpacingProperty, body.GetResourceObservable("Space.Medium"));
        body.Bind(MarginProperty, body.GetResourceObservable("Padding.Content"));
        Content = body;
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        if (_inspector)
        {
            foreach (var key in new[]
                     {
                         "Inspector.Project", "Inspector.Status", "Project.Tempo", "Project.Meter",
                         "Inspector.Patterns", "Inspector.Sounds", "Inspector.Resources"
                     })
            {
                var row = new Grid { ColumnDefinitions = new("Auto,*") };
                var label = new TextBlock { Classes = { "secondary" }, MinWidth = 100 };
                var value = new TextBlock { TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis };
                Grid.SetColumn(value, 1);
                row.Children.Add(label);
                row.Children.Add(value);
                body.Children.Add(row);
                _rows.Add((label, value, key));
            }
        }

        session.PropertyChanged += SessionChanged;
        Refresh();
    }

    private void SessionChanged(object? sender, PropertyChangedEventArgs args) => Refresh();

    public void Refresh()
    {
        if (IsDisposed || _session.IsClosed) return;
        if (_inspector)
        {
            var state = _session.Snapshot.State;
            foreach (var (label, value, key) in _rows)
            {
                label.Text = _session[key];
                value.Text = key switch
                {
                    "Inspector.Project" => _session.ProjectName, "Inspector.Status" => _session.ProjectStatus,
                    "Project.Tempo" => _session.Tempo + " BPM", "Project.Meter" => _session.Meter,
                    "Inspector.Patterns" => state.Patterns.Length.ToString(CultureInfo.InvariantCulture),
                    "Inspector.Sounds" => state.Sounds.Length.ToString(CultureInfo.InvariantCulture),
                    "Inspector.Resources" => state.Resources.Length.ToString(CultureInfo.InvariantCulture), _ => ""
                };
                ToolTip.SetTip(value, value.Text);
            }
        }

    }

    public void Dispose()
    {
        if (IsDisposed) return;
        IsDisposed = true;
        _session.PropertyChanged -= SessionChanged;
    }
}
