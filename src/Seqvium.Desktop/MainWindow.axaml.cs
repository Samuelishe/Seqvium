// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Seqvium.Desktop.Presentation;
using Seqvium.Core;
using Seqvium.Desktop.Workspace;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop;

internal sealed partial class MainWindow : Window
{
    private readonly ShellSession _session;
    private readonly App _app;
    private Task _preferenceWork = Task.CompletedTask;
    private bool _systemMenuRequested;
    private readonly WorkspaceHost _workspace;
    private readonly WorkspaceLayoutPersistence _layoutPersistence;
    private readonly bool _layoutFallback;
    private readonly GraphEditorSession _graphEditor = new();
    private readonly NodeEditorSession _nodeEditor = new();
    private GraphPaneContent? _graphPane;
    private PianoRollPaneContent? _pianoPane;
    private readonly PatternEditorNavigation _patternEditors = new();
    private bool _closing;
    private bool _shutdownComplete;
    private MainWindowPlacement? _normalWindow;
    private WorkspaceLayout? _musicLayout;

    private bool HasMusicalContent => !_workflow.State.Snapshot.State.Patterns.IsEmpty ||
                                      !_workflow.State.Snapshot.State.AudioClips.IsEmpty;

    private WorkspaceLayout CaptureLayout() => (!HasMusicalContent && _musicLayout is { } music
        ? music : _workspace.State.Capture()) with
    {
        Window = _normalWindow is { } normal
            ? normal with
            {
                Maximized = WindowState == WindowState.Maximized ||
                            WindowState == WindowState.Minimized && normal.Maximized
            }
            : null
    };

    private void RememberWindow()
    {
        if (WindowState == WindowState.Normal && IsVisible)
            _normalWindow = new(Position.X, Position.Y, Width, Height, false);
        else if (WindowState == WindowState.Maximized && _normalWindow is { } normal)
            _normalWindow = normal with { Maximized = true };
    }

    private void RestoreWindow(MainWindowPlacement? saved)
    {
        if (saved is null)
        {
            _normalWindow = new(Position.X, Position.Y, Width, Height, true);
            WindowState = WindowState.Maximized;
            return;
        }

        var screen = Screens.ScreenFromPoint(new(saved.X, saved.Y)) ?? Screens.Primary;
        if (screen is not null)
        {
            var area = screen.WorkingArea;
            double scale = screen.Scaling;
            Width = Math.Clamp(saved.Width, Math.Min(MinWidth, area.Width / scale), area.Width / scale);
            Height = Math.Clamp(saved.Height, Math.Min(MinHeight, area.Height / scale), area.Height / scale);
            Position = new((int)Math.Clamp(saved.X, area.X, area.Right - Width * scale),
                (int)Math.Clamp(saved.Y, area.Y, area.Bottom - Height * scale));
            WindowStartupLocation = WindowStartupLocation.Manual;
            _normalWindow = new(Position.X, Position.Y, Width, Height, saved.Maximized);
        }

        WindowState = saved.Maximized ? WindowState.Maximized : WindowState.Normal;
    }

    internal void ActivateExisting()
    {
        if (_closing) return;
        if (!IsVisible) Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate(); // One ordinary OS request; foreground restrictions remain authoritative.
    }

    public MainWindow(ShellSession session, App app, WorkspaceLayoutStore layoutStore, WorkspaceLayoutLoad layout)
    {
        _session = session;
        _app = app;
        _musicLayout = layout.Layout;
        InitializeComponent();
        RestoreWindow(layout.Layout?.Window);
        DataContext = session;
        InitializeWorkflow();
        _layoutFallback = layout.UsedFallback;
        _layoutPersistence = new(layoutStore.SaveAsync);
        _workspace = new(session, new(layout.Layout), ToggleLanguageAsync, ToggleThemeAsync,
            CreatePaneContent);
        WorkspaceRegion.Child = _workspace;
        var navigation = new WorkspaceRail(session, _workspace, OpenPane);
        var patterns = new PatternNavigator(session, _workflow, _workspace, OpenPatternEditorAsync);
        var navigationBody = new Grid { ColumnDefinitions = new("Auto,Auto") };
        navigationBody.Children.Add(navigation);
        Grid.SetColumn(patterns, 1);
        navigationBody.Children.Add(patterns);
        NavigationRegion.Child = navigationBody;
        bool navigationCollapsed = false;

        void AdaptNavigation()
        {
            patterns.IsVisible = !navigationCollapsed && Bounds.Width >= 1000;
            patterns.Width = Math.Clamp(Bounds.Width * .15, 190, 288);
            navigationBody.ColumnDefinitions[1].Width = patterns.IsVisible ? GridLength.Auto : new GridLength(0);
            navigation.NavigationToggle.Classes.Set("active", patterns.IsVisible);
            _workflowControls.OccurrenceShownElsewhere = patterns.IsVisible;
            _workflowControls.Refresh();
        }

        navigation.NavigationToggle.Click += (_, _) =>
        {
            navigationCollapsed = !navigationCollapsed;
            if (navigationCollapsed && patterns.IsKeyboardFocusWithin) navigation.NavigationToggle.Focus();
            AdaptNavigation();
        };
        SizeChanged += (_, _) => Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (!_closing) AdaptNavigation();
        }, Avalonia.Threading.DispatcherPriority.Loaded);
        AdaptNavigation();
        // The first workspace starts at a musical tool without creating or modifying a Pattern.
        Opened += (_, _) =>
        {
            RememberWindow();
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (_closing) return;
                // Initial normal-size layout can precede the native maximized arrange.
                // Replay saved geometry against the settled workspace, before user interaction.
                if (layout.Layout is { } saved) _workspace.State.RestoreLayout(saved);
                if (_workflow.State.Snapshot.State.Patterns.IsEmpty &&
                    _workflow.State.Snapshot.State.AudioClips.IsEmpty) _workspace.State.StartEmptyDocument();
                else if (layout.Layout is null) _workspace.State.ArrangeMusic();
                _workspace.Commit();
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        };
        PositionChanged += (_, _) =>
        {
            RememberWindow();
            if (IsVisible) _workspace.Commit();
        };
        SizeChanged += (_, _) => RememberWindow();
        _workspace.State.Changed += RefreshGainHierarchy;
        RefreshGainHierarchy();
        _workspace.LeaveRequested += () => PaneMenuAction.Focus();
        _workspace.LayoutCommitted += () => _layoutPersistence.Request(CaptureLayout());
        _layoutPersistence.Completed += () => session.SetLayoutNotice(_layoutFallback ? "Workspace.LoadFallback" :
            _layoutPersistence.LastWriteSucceeded ? null : "Workspace.SaveFailed");
        if (_layoutFallback) session.SetLayoutNotice("Workspace.LoadFallback");
        AddHandler(KeyDownEvent, WindowKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, WindowKeyUp, RoutingStrategies.Tunnel);
        Deactivated += (_, _) =>
        {
            _systemMenuRequested = false;
            _workspace.CancelGestures();
        };
        if (OperatingSystem.IsWindows())
        {
            // Retain WS_SYSMENU/caption semantics while extending content through the title bar.
            WindowDecorations = WindowDecorations.Full;
            WindowDecorationsTheme = (Avalonia.Styling.ControlTheme?)Resources["HostChrome"];
            ExtendClientAreaToDecorationsHint = true;
            ExtendClientAreaTitleBarHeightHint = 0;
            WindowDecorationProperties.SetElementRole(TitleRegion, WindowDecorationsElementRole.TitleBar);
            WindowDecorationProperties.SetElementRole(MaximizeAction, WindowDecorationsElementRole.MaximizeButton);
        }
        else
        {
            // Keep native move/resize/keyboard behavior until each desktop's custom chrome is evidenced.
            WindowActions.IsVisible = false;
        }

        session.PropertyChanged += SessionChanged;
        RefreshChrome();
        Closing += OnClosing;
        Closed += (_, _) =>
        {
            session.PropertyChanged -= SessionChanged;
            _workspace.State.Changed -= RefreshGainHierarchy;
            navigation.Dispose();
            patterns.Dispose();
            _workspace.Dispose();
            _nodeEditor.Dispose();
            session.Dispose();
        };
    }

    private void SessionChanged(object? sender, PropertyChangedEventArgs args)
    {
        _app.ApplyTheme(_session.Preferences.Theme);
        RefreshChrome();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if ((change.Property == IsActiveProperty || change.Property == WindowStateProperty) &&
            DataContext is ShellSession)
            RefreshChrome();
    }

    private void RefreshChrome()
    {
        Title = $"{_session.ProjectName} — Seqvium";
        ToolTip.SetTip(TitleRegion, _session[IsActive ? "Window.Active" : "Window.Inactive"]);
        BrandName.Bind(TextBlock.ForegroundProperty,
            BrandName.GetResourceObservable(IsActive ? "Text.Primary" : "Text.Secondary"));
        Chrome.Bind(Border.BorderBrushProperty,
            Chrome.GetResourceObservable(IsActive ? "Focus.Active" : "Border.Default"));
        var label = _session[WindowState == WindowState.Maximized ? "Window.Restore" : "Window.Maximize"];
        MaximizeAction.Content = HostIcons.Create(WindowState == WindowState.Maximized ? "Restore" : "Maximize",
            MaximizeAction, 16);
        AutomationProperties.SetName(MaximizeAction, label);
        ToolTip.SetTip(MaximizeAction, label);
    }

    private void MinimizeClicked(object? sender, RoutedEventArgs args) => WindowState = WindowState.Minimized;

    private void MusicLayoutClicked(object? sender, RoutedEventArgs args)
    {
        PaneMenuAction.Flyout?.Hide();
        _workspace.CancelGestures();
        _workspace.State.ArrangeMusic();
        _workspace.Commit();
    }

    private void MaximizeClicked(object? sender, RoutedEventArgs args) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseClicked(object? sender, RoutedEventArgs args) => Close();

    private void WindowKeyDown(object? sender, KeyEventArgs args)
    {
        if (!_closing && ((args.Key == Key.F6 && args.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift) ||
                          (args.Key == Key.Tab &&
                           args.KeyModifiers is KeyModifiers.Control or (KeyModifiers.Control | KeyModifiers.Shift))))
        {
            _workspace.CancelGestures();
            var id = _workspace.State.SwitchPane(args.KeyModifiers.HasFlag(KeyModifiers.Shift));
            _workspace.FocusPane(id);
            _workspace.Commit();
            args.Handled = true;
        }

        if (!_closing && args.Key == Key.W && args.KeyModifiers == KeyModifiers.Control &&
            _workspace.IsKeyboardFocusWithin && _workspace.State.ActivePaneId is { } active)
        {
            _workspace.Action(active, () => _workspace.State.Hide(active));
            args.Handled = true;
        }

        if (OperatingSystem.IsWindows() && args.Key == Key.Space && args.KeyModifiers == KeyModifiers.Alt)
        {
            args.Handled = true;
            _systemMenuRequested = true;
        }
    }

    private void WindowKeyUp(object? sender, KeyEventArgs args)
    {
        if (!_systemMenuRequested) return;
        if (args.Key == Key.Space) args.Handled = true;
        if (args.Key is not (Key.LeftAlt or Key.RightAlt)) return;
        _systemMenuRequested = false;
        args.Handled = true;
        // Open after Alt is released: its key-up otherwise dismisses the native popup immediately.
        WindowsWindowMenu.Show(this);
    }

    private Task ToggleLanguageAsync()
    {
        if (_closing) return Task.CompletedTask;
        _preferenceWork = _session.ToggleLanguageAsync();
        return _preferenceWork;
    }

    private Task ToggleThemeAsync()
    {
        if (_closing) return Task.CompletedTask;
        _preferenceWork = _session.ToggleThemeAsync();
        return _preferenceWork;
    }

    private async void LanguageClicked(object? sender, RoutedEventArgs args) => await ToggleLanguageAsync();
    private async void ThemeClicked(object? sender, RoutedEventArgs args) => await ToggleThemeAsync();

    private void InspectorClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.InspectorId);
    private void AppearanceClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.AppearanceId);
    private void GraphClicked(object? sender, RoutedEventArgs args) => OpenGraph();
    private void NodeEditorClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.NodeEditorId);
    private void BeatClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.BeatId);
    private void PianoClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.PianoId);
    private void ArrangementClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.ArrangementId);
    private void MixerClicked(object? sender, RoutedEventArgs args) => OpenPane(WorkspaceState.MixerId);

    private Control CreatePaneContent(PaneDefinition definition)
    {
        if (definition.InstanceId == WorkspaceState.MixerId)
            return new MixerPaneContent(_session, _workflow);
        if (definition.InstanceId == WorkspaceState.ArrangementId)
            return new ArrangementPaneContent(_session, _workflow, WorkflowActionAsync,
                () => OpenSelectedPatternEditor());
        if (definition.InstanceId == WorkspaceState.BeatId)
        {
            var rack = new BeatPaneContent(_session, _workflow, ImportBeatWavAsync);
            rack.MixerRequested += sound =>
            {
                _workspace.Open(WorkspaceState.MixerId);
                this.GetVisualDescendants().OfType<MixerPaneContent>().Single().OpenSound(sound);
            };
            rack.SoundRequested += part =>
            {
                var state = _workflow.State.Snapshot.State;
                var attachment = state.GraphAttachments.SingleOrDefault(a => a.Id == _workflow.State.Selected);
                var source = attachment?.Sources.SingleOrDefault(s => s.PartId == part);
                if (source is not null)
                    OpenNodeEditor(new(_workflow.State.Document.LifecycleId,
                        attachment!.Id, attachment.GraphId, source.NodeId));
            };
            rack.PianoRequested += part =>
            {
                _workspace.Open(WorkspaceState.PianoId);
                _pianoPane?.OpenPart(part);
                _patternEditors.Remember(_workflow.State, WorkspaceState.PianoId, part);
            };
            return rack;
        }

        if (definition.InstanceId == WorkspaceState.PianoId)
        {
            _pianoPane = new(_session, _workflow, WorkflowActionAsync,
                () => OpenPane(WorkspaceState.BeatId));
            _pianoPane.PartOpened += part => _patternEditors.Remember(_workflow.State, WorkspaceState.PianoId, part);
            return _pianoPane;
        }
        if (definition.InstanceId == WorkspaceState.GraphId)
        {
            var content = new GraphPaneContent(_session, _workflow, _graphEditor, ImportGraphWavAsync);
            _graphPane = content;
            content.EditorRequested += OpenNodeEditor;
            content.SelectionChanged += RefreshGainHierarchy;
            return content;
        }

        return definition.InstanceId == WorkspaceState.NodeEditorId
            ? new NodeEditorPaneContent(_session, _workflow, _nodeEditor)
            : new FirstPartyPaneContent(_session, definition, ToggleLanguageAsync, ToggleThemeAsync);
    }

    private void OpenNodeEditor(NodeEditorTarget target)
    {
        bool differentTarget = _nodeEditor.Target != target;
        if (_closing || !_nodeEditor.Open(_workflow.State, target)) return;
        _workspace.Open(WorkspaceState.NodeEditorId);
        var sound = DrumEditorSession.Resolve(_workflow.State.Snapshot.State, target);
        if (differentTarget && sound is not null && DrumVoice.Supports(sound) &&
            _workspace.State.Get(WorkspaceState.NodeEditorId).Dock == PaneDock.Floating)
        {
            var bounds = _workspace.State.Bounds(WorkspaceState.NodeEditorId);
            if (bounds.Width < 640 || bounds.Height < 700)
            {
                _workspace.State.SetBounds(WorkspaceState.NodeEditorId, bounds with
                {
                    Width = Math.Max(720, bounds.Width), Height = Math.Max(740, bounds.Height)
                });
                _workspace.Commit();
            }
        }
    }

    private void RefreshGainHierarchy()
    {
        if (_shutdownComplete || _workspace is null) return;
        var visibility =
            GainSurfacePresentation.Resolve(_workflow.State, _graphEditor, _nodeEditor.Target, _workspace.State);
        _workflowControls.GainShownElsewhere = visibility.HideToolbarGain;
        _workflowControls.Refresh(_closing || _workflowActionBusy);
        if (_graphPane is { } graph && graph.GainShownInEditor != visibility.HideGraphGain)
        {
            graph.GainShownInEditor = visibility.HideGraphGain;
            graph.Refresh();
        }
    }

    private void OpenGraph()
    {
        if (_closing) return;
        bool same = _graphEditor.Target == _workflow.State.Selected &&
                    _graphEditor.Basis?.Lifecycle == _workflow.State.Document.LifecycleId;
        if (_workflow.State.Selected is { } target) _graphEditor.Open(_workflow.State, target);
        OpenPane(WorkspaceState.GraphId);
        if (!same)
            Avalonia.Threading.Dispatcher.UIThread.Post(_workspace.FrameGraph,
                Avalonia.Threading.DispatcherPriority.Loaded);
    }

    private void OpenPane(string id)
    {
        if (_closing) return;
        PaneMenuAction.Flyout?.Hide();
        _workspace.Open(id);
        if (id == WorkspaceState.BeatId) _patternEditors.Remember(_workflow.State, id);
        if (id == WorkspaceState.PianoId && _pianoPane?.Canvas.Part is { } part)
            _patternEditors.Remember(_workflow.State, id, part.Id);
    }

    private async Task OpenPatternEditorAsync(Id<GraphAttachment> target, string? pane, Id<MusicalPart>? part)
    {
        var lifecycle = _workflow.State.Document.LifecycleId;
        if (_closing || !await _workflow.SelectAsync(target) ||
            _workflow.State.Document.LifecycleId != lifecycle || _workflow.State.Selected != target) return;
        OpenSelectedPatternEditor(pane, part);
    }

    private void OpenSelectedPatternEditor(string? pane = null, Id<MusicalPart>? part = null)
    {
        if (_closing) return;
        var resolved = _patternEditors.Resolve(_workflow.State);
        pane ??= resolved.Pane;
        part ??= pane == WorkspaceState.PianoId ? resolved.Part : null;
        _workspace.Open(pane);
        if (pane == WorkspaceState.PianoId && part is { } target) _pianoPane?.OpenPart(target);
        _patternEditors.Remember(_workflow.State, pane, _pianoPane?.Canvas.Part?.Id);
    }

    private void ApplyOpenedWorkspace()
    {
        _workspace.CancelGestures();
        if (!HasMusicalContent) _workspace.State.StartEmptyDocument();
        else if (_musicLayout is { } layout) _workspace.State.RestoreLayout(layout);
        else if (_workflow.State.SongMode) _workspace.State.ArrangeMusic();
        else _workspace.Open(WorkspaceState.BeatId);
        _workspace.Commit();
    }

    private async void OnClosing(object? sender, WindowClosingEventArgs args)
    {
        if (_shutdownComplete) return;
        args.Cancel = true;
        if (_closing) return;
        _closing = true;
        _importCancellation?.Cancel();
        _workflowControls.Refresh(true);
        await _workflowWork;
        var decision = await ReplacementDecisionAsync();
        if (!await _workflow.CloseAsync(decision.Decision, decision.SavePath))
        {
            _closing = false;
            _workflowControls.Refresh();
            return;
        }

        _workflow.Changed -= WorkflowChanged;
        await _workflow.DisposeAsync();
        _workspace.PrepareShutdown();
        RememberWindow();
        _layoutPersistence.Request(CaptureLayout());
        await _layoutPersistence.ShutdownAsync();
        await _preferenceWork;
        _shutdownComplete = true;
        Close();
    }
}
