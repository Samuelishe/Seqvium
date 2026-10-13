// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Direct access to real retained work areas, independent of musical selection.</summary>
internal sealed class WorkspaceRail : Border, IDisposable
{
    private readonly ShellSession _shell;
    private readonly WorkspaceHost _host;
    private readonly Dictionary<string, Button> _buttons = [];
    internal Button NavigationToggle { get; } = new() { Classes = { "rail" } };

    internal WorkspaceRail(ShellSession shell, WorkspaceHost host)
    {
        _shell = shell;
        _host = host;
        this.Bind(WidthProperty, this.GetResourceObservable("Rail.Width"));
        this.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Raised"));
        this.Bind(BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        BorderThickness = new(0, 0, 1, 0);
        var body = new StackPanel { Margin = new(7, 10), Classes = { "spaced" } };
        Child = body;
        NavigationToggle.Content = HostIcons.Create("More", NavigationToggle, 20);
        AutomationProperties.SetAutomationId(NavigationToggle, "Navigator.Toggle");
        body.Children.Add(NavigationToggle);
        foreach (var (id, icon) in new[]
                 {
                     (WorkspaceState.BeatId, "Rack"), (WorkspaceState.PianoId, "Piano"),
                     (WorkspaceState.ArrangementId, "Playlist"), (WorkspaceState.GraphId, "Graph"),
                     (WorkspaceState.NodeEditorId, "Sound"), (WorkspaceState.InspectorId, "Inspector"),
                     (WorkspaceState.AppearanceId, "Theme")
                 })
        {
            var button = new Button { Classes = { "rail" } };
            button.Content = HostIcons.Create(icon, button, 20);
            AutomationProperties.SetAutomationId(button, id + ".open");
            button.Click += (_, _) => host.Open(id);
            body.Children.Add(button);
            _buttons.Add(id, button);
        }

        host.State.Changed += Refresh;
        shell.PropertyChanged += PreferencesChanged;
        Refresh();
    }

    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args) => Refresh();

    private void Refresh()
    {
        AutomationProperties.SetName(NavigationToggle, _shell["Navigator.Toggle"]);
        ToolTip.SetTip(NavigationToggle, _shell["Navigator.Toggle"]);
        foreach (var (id, button) in _buttons)
        {
            string label = _shell[WorkspaceState.Definition(id).TitleKey];
            AutomationProperties.SetName(button, label);
            ToolTip.SetTip(button, label);
            button.Classes.Set("active", _host.State.ActivePaneId == id);
        }
    }

    public void Dispose()
    {
        _host.State.Changed -= Refresh;
        _shell.PropertyChanged -= PreferencesChanged;
    }
}
