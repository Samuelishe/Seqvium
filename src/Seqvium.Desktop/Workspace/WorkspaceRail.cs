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
    internal Button BrowserToggle => _buttons[WorkspaceState.BrowserId];

    internal WorkspaceRail(ShellSession shell, WorkspaceHost host, Action<string>? openPane = null)
    {
        _shell = shell;
        _host = host;
        this.Bind(WidthProperty, this.GetResourceObservable("Rail.Width"));
        this.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Raised"));
        this.Bind(BorderBrushProperty, this.GetResourceObservable("Border.Default"));
        BorderThickness = new(0, 0, 1, 0);
        var body = new StackPanel { Margin = new(7, 10), Classes = { "spaced" } };
        Child = body;
        var sidebars = new StackPanel { Classes = { "tight" } };
        AutomationProperties.SetAutomationId(sidebars, "Sidebar.Toggles");
        body.Children.Add(sidebars);
        var divider = new Border { Margin = new(3, 4, 3, 4) };
        divider.Bind(HeightProperty, this.GetResourceObservable("Border.Width"));
        divider.Bind(BackgroundProperty, this.GetResourceObservable("Border.Default"));
        body.Children.Add(divider);
        NavigationToggle.Content = HostIcons.Create("Patterns", NavigationToggle, 20);
        AutomationProperties.SetAutomationId(NavigationToggle, "Navigator.Toggle");
        foreach (var (id, icon) in new[]
                 {
                     (WorkspaceState.BrowserId, "Browser"),
                     (WorkspaceState.SampleLabId, "SampleLab"),
                     (WorkspaceState.BeatId, "Rack"), (WorkspaceState.PianoId, "Piano"),
                     (WorkspaceState.ArrangementId, "Playlist"), (WorkspaceState.MixerId, "Mixer"),
                     (WorkspaceState.GraphId, "Graph"),
                     (WorkspaceState.NodeEditorId, "Sound"), (WorkspaceState.InspectorId, "Inspector")
                 })
        {
            var button = new Button { Classes = { "rail" } };
            button.Content = HostIcons.Create(icon, button, 20);
            AutomationProperties.SetAutomationId(button, id + ".open");
            button.Click += (_, _) =>
            {
                if (id == WorkspaceState.BrowserId && host.State.Get(id).Visibility == PaneVisibility.Visible)
                    host.Action(id, () => host.State.Collapse(id), false);
                else if (openPane is not null) openPane(id);
                else host.Open(id);
            };
            if (id == WorkspaceState.BrowserId)
            {
                sidebars.Children.Add(button); sidebars.Children.Add(NavigationToggle);
            }
            else body.Children.Add(button);
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
            button.Classes.Set("active", id == WorkspaceState.BrowserId
                ? _host.State.Get(id).Visibility == PaneVisibility.Visible : _host.State.ActivePaneId == id);
        }
    }

    public void Dispose()
    {
        _host.State.Changed -= Refresh;
        _shell.PropertyChanged -= PreferencesChanged;
    }
}
