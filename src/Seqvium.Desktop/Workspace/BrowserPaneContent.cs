// SPDX-License-Identifier: Apache-2.0

using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Retained discovery presentation. Directory, selection and preview remain outside musical history.</summary>
internal sealed class BrowserPaneContent : Grid, IDisposable
{
    internal static readonly DataFormat<BrowserSource> DragFormat = DataFormat.CreateInProcessFormat<BrowserSource>("Seqvium.SampleSource");
    private sealed record Location(string Label, string? Path) { public override string ToString() => Label; }
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly BrowserLocationsStore _store;
    private readonly ExtensionBrowserContent _extensions;
    private readonly Expander _extensionDisclosure = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private const string PackPrefix = "seqvium-pack:";
    private bool IsPack => _currentPath?.StartsWith(PackPrefix, StringComparison.Ordinal) == true;
    private readonly Action<string> _openPane;
    private readonly ComboBox _locations = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly TextBox _path = new();
    private readonly TextBox _search = new();
    private readonly ListBox _list = new() { SelectionMode = SelectionMode.Single, Focusable = true };
    private readonly ObservableCollection<BrowserSource> _entries = [];
    private readonly Button _add = new() { Classes = { "command" } }, _up = new() { Classes = { "command" } },
        _back = new() { Classes = { "command" } }, _refresh = new() { Classes = { "command" } },
        _remove = new() { Classes = { "command" } }, _more = new(), _play = new() { Classes = { "command" } },
        _stop = new() { Classes = { "command" } }, _rack = new(), _playlist = new(), _cancel = new() { Classes = { "command" } };
    private readonly CheckBox _auto = new();
    private readonly TextBlock _title = new() { TextTrimming = TextTrimming.CharacterEllipsis },
        _metadata = new() { Classes = { "caption", "numeric" }, TextWrapping = TextWrapping.Wrap },
        _status = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap },
        _notice = new() { Classes = { "caption", "warning" }, TextWrapping = TextWrapping.Wrap };
    private readonly BrowserWaveform _wave;
    private readonly ScrollViewer _preview;
    private readonly DispatcherTimer _searchDelay = new() { Interval = TimeSpan.FromMilliseconds(220) };
    private BrowserLocations _saved = new([], [], null);
    private readonly Stack<string?> _history = [];
    private BrowserDirectory? _directory;
    private string? _currentPath;
    private BrowserSource? _selected;
    private CancellationTokenSource _cancellation = new();
    private bool _refreshing, _disposed, _using, _loaded;
    private Guid _lifecycle;
    private string _message = "Browser.Empty";
    internal Task PendingCommand { get; private set; } = Task.CompletedTask;

    internal BrowserPaneContent(ShellSession shell, DesktopWorkflow workflow, string configurationDirectory,
        Action<string> openPane)
    {
        _shell = shell; _workflow = workflow; _openPane = openPane;
        _store = new(Path.Combine(configurationDirectory, "browser-locations.json"));
        _wave = new(shell) { Height = 72, IsHitTestVisible = false };
        RowDefinitions = new("Auto,Auto,Auto,*,Auto,Auto");
        Margin = new(8);
        RowSpacing = 6;
        var locations = new Grid { ColumnDefinitions = new("*,Auto,Auto"), ColumnSpacing = 4 };
        locations.Children.Add(_locations); Add(locations, _add, 1); Add(locations, _remove, 2);
        _extensions = new(shell, workflow, configurationDirectory, package =>
        {
            RebuildLocations();
            if (package is not null) Navigate(PackPrefix + package.Package.Identity);
            else if (IsPack)
            {
                if (_using) Enqueue(() => { Navigate(null); return Task.CompletedTask; });
                else Navigate(null);
            }
        });
        var extensionScroll = new ScrollViewer
        {
            Content = _extensions, MaxHeight = 190, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        _extensionDisclosure.Content = extensionScroll;
        AutomationProperties.SetAutomationId(_extensionDisclosure, "Extension.Disclosure");
        var discovery = new StackPanel { Classes = { "tight" } };
        discovery.Children.Add(locations); discovery.Children.Add(_extensionDisclosure); Children.Add(discovery);
        var address = new Grid { ColumnDefinitions = new("Auto,Auto,*,Auto"), ColumnSpacing = 4 };
        address.Children.Add(_back); Add(address, _up, 1); Add(address, _path, 2); Add(address, _refresh, 3);
        SetRow(address, 1); Children.Add(address);
        SetRow(_search, 2); Children.Add(_search);
        _list.ItemsSource = _entries;
        _list.ItemTemplate = new FuncDataTemplate<BrowserSource>((entry, _) =>
        {
            var label = new TextBlock { Text = entry?.ToString(), TextTrimming = TextTrimming.CharacterEllipsis };
            ToolTip.SetTip(label, entry?.Path ?? entry?.Name);
            return label;
        });
        _list.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Workspace"));
        SetRow(_list, 3); Children.Add(_list);
        var feedback = new StackPanel { Classes = { "tight" } };
        feedback.Children.Add(_more); feedback.Children.Add(_status); feedback.Children.Add(_notice);
        SetRow(feedback, 4); Children.Add(feedback);
        var preview = new StackPanel { Classes = { "tight" } };
        _title.Classes.Add("section"); preview.Children.Add(_title); preview.Children.Add(_metadata);
        preview.Children.Add(_wave);
        var listening = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "compact" } };
        listening.Children.Add(_play); listening.Children.Add(_stop); listening.Children.Add(_auto); listening.Children.Add(_cancel);
        preview.Children.Add(listening);
        var use = new WrapPanel { Orientation = Orientation.Horizontal };
        _rack.Margin = new(0, 0, 4, 0); use.Children.Add(_rack); use.Children.Add(_playlist);
        preview.Children.Add(use);
        var previewScroll = new ScrollViewer
        {
            Content = preview, MaxHeight = 230, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        _preview = previewScroll;
        void FitDisclosures()
        {
            extensionScroll.MaxHeight = Math.Max(80, Math.Min(190, Bounds.Height * .28));
            previewScroll.MaxHeight = Math.Max(80, Math.Min(230, Bounds.Height *
                (_extensionDisclosure.IsExpanded && Bounds.Height < 650 ? .22 : .45)));
        }
        SizeChanged += (_, _) => FitDisclosures();
        _extensionDisclosure.PropertyChanged += (_, args) => { if (args.Property == Expander.IsExpandedProperty) FitDisclosures(); };
        SetRow(previewScroll, 5); Children.Add(previewScroll);
        Setup(_add, "Browser.AddFolder", "Open"); Setup(_remove, "Browser.RemoveFolder", "Close");
        Setup(_back, "Browser.Back", "Left"); Setup(_up, "Browser.Up", "Up");
        Setup(_refresh, "Browser.Refresh", "Refresh"); Setup(_play, "Browser.Preview", "Play");
        Setup(_stop, "Browser.Stop", "Stop"); Setup(_cancel, "Browser.Cancel", "Close");
        AutomationProperties.SetAutomationId(_list, "Browser.Files");
        AutomationProperties.SetAutomationId(_search, "Browser.Search");
        AutomationProperties.SetAutomationId(_path, "Browser.Path");
        AutomationProperties.SetAutomationId(_locations, "Browser.Locations");
        AutomationProperties.SetAutomationId(_rack, "Browser.UseRack");
        AutomationProperties.SetAutomationId(_playlist, "Browser.UsePlaylist");
        AutomationProperties.SetAutomationId(_auto, "Browser.AutoPreview");
        _add.Click += (_, _) => Enqueue(AddFolderAsync);
        _remove.Click += (_, _) => Enqueue(async () =>
        {
            if (_currentPath is null) return;
            _saved = _saved with { Roots = _saved.Roots.Where(p => p != _currentPath).ToArray(),
                Recent = _saved.Recent.Where(p => p != _currentPath).ToArray(), Last = null };
            await SaveLocationsAsync(); RebuildLocations(); Navigate(null);
        });
        _locations.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _locations.SelectedItem is Location location) Navigate(location.Path);
        };
        _path.KeyDown += (_, e) => { if (e.Key == Key.Enter) { Navigate(_path.IsReadOnly ? _currentPath : _path.Text); e.Handled = true; } };
        _up.Click += (_, _) => Navigate(!IsPack && _currentPath is { } p ? Directory.GetParent(p)?.FullName : null);
        _back.Click += (_, _) => { if (_history.TryPop(out var p)) Navigate(p, false); };
        _refresh.Click += (_, _) => Navigate(_currentPath, false);
        _more.Click += (_, _) => Enqueue(() => LoadPageAsync(_cancellation.Token));
        _search.TextChanged += (_, _) => { if (!_refreshing) { _searchDelay.Stop(); _searchDelay.Start(); } };
        _searchDelay.Tick += (_, _) => { _searchDelay.Stop(); Navigate(_currentPath, false); };
        _list.SelectionChanged += (_, _) => { if (!_refreshing) SelectSource(); };
        _list.DoubleTapped += (_, _) => ActivateSelected();
        _play.Click += (_, _) => Preview();
        _stop.Click += (_, _) => StopPreview();
        _cancel.Click += (_, _) => { _cancellation.Cancel(); StopPreview(); };
        _rack.Click += (_, _) => UseSelected(true);
        _playlist.Click += (_, _) => UseSelected(false);
        _auto.IsCheckedChanged += async (_, _) =>
        {
            if (_refreshing || _auto.IsChecked == _shell.Controls.AutoPreview) return;
            bool enabled = _auto.IsChecked == true;
            var save = _shell.SetControlsAsync(_shell.Controls with { AutoPreview = enabled });
            if (enabled) Preview();
            await save;
        };
        _list.AddHandler(PointerPressedEvent, (_, e) =>
        {
            _drag = null;
            if (e.GetCurrentPoint(_list).Properties.IsLeftButtonPressed &&
                e.Source is Visual visual && visual.GetVisualAncestors().Prepend(visual)
                    .OfType<ListBoxItem>().FirstOrDefault() is { } row &&
                _list.ItemFromContainer(row) is BrowserSource { Directory: false } source)
                _drag = (e, e.GetPosition(_list), source);
        }, RoutingStrategies.Tunnel, handledEventsToo: true);
        _list.AddHandler(PointerMovedEvent, DragMoved, RoutingStrategies.Tunnel, handledEventsToo: true);
        _list.PointerReleased += (_, _) => _drag = null;
        AddHandler(KeyDownEvent, BrowserKeyDown, RoutingStrategies.Tunnel);
        shell.PropertyChanged += PreferencesChanged;
        workflow.Changed += WorkflowChanged;
        _lifecycle = workflow.State.Document.LifecycleId;
        AttachedToVisualTree += (_, _) =>
        {
            if (_loaded) return; _loaded = true;
            Enqueue(async () =>
            {
                _saved = await _store.LoadAsync(); RebuildLocations();
                Navigate(_saved.Last, false);
            });
        };
        Refresh();
    }

    private static void Add(Grid grid, Control control, int column) { SetColumn(control, column); grid.Children.Add(control); }
    private void Setup(Button button, string key, string icon)
    { button.Content = HostIcons.Create(icon, button, 16); AutomationProperties.SetAutomationId(button, key); }

    private void Enqueue(Func<Task> operation)
    {
        if (_disposed) return;
        var previous = PendingCommand;
        PendingCommand = RunAsync();
        async Task RunAsync()
        {
            await Task.Yield(); // Publish the task before an operation can enqueue a dependent action.
            await previous;
            if (_disposed) return;
            try { await operation(); }
            catch (OperationCanceledException) { _message = "Workflow.Cancelled"; }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException or
                NotSupportedException or ArgumentException)
            {
                bool localized = HostLocalizer.English.ContainsKey(error.Message);
                _message = localized ? error.Message : "Browser.SourceUnavailable";
                ToolTip.SetTip(_status, localized ? _shell[error.Message] : error.Message);
            }
            finally { if (!_disposed) Refresh(); }
        }
    }

    private void RenewCancellation()
    { _cancellation.Cancel(); _cancellation.Dispose(); _cancellation = new(); }

    internal void Navigate(string? path, bool remember = true)
    {
        if (_using || _disposed) return;
        RenewCancellation();
        var cancellation = _cancellation.Token;
        string query = _search.Text ?? "";
        Enqueue(async () =>
        {
            cancellation.ThrowIfCancellationRequested();
            string? resolved = string.IsNullOrWhiteSpace(path) ? null : path.StartsWith(PackPrefix, StringComparison.Ordinal) ? path : Path.GetFullPath(path);
            if (remember && resolved != _currentPath) _history.Push(_currentPath);
            _directory?.Dispose(); _directory = null;
            _currentPath = resolved;
            // Virtual locations display labels, never editable filesystem addresses.
            _path.IsReadOnly = resolved is null || IsPack;
            _refreshing = true;
            _selected = null; _entries.Clear(); _wave.Waveform = null; _wave.InvalidateVisual();
            _path.Text = resolved ?? _shell["Browser.Project"]; _refreshing = false;
            _more.IsVisible = false;
            if (IsPack)
            {
                string id = resolved![PackPrefix.Length..];
                var package = _workflow.Extensions.Status.SingleOrDefault(p => p.Package.Identity == id && p.Availability == ExtensionAvailability.Available)
                    ?? throw new InvalidOperationException("Extension.Unavailable");
                _path.Text = package.Package.Manifest!.Name;
                foreach (var capability in package.Package.Manifest.Capabilities.Where(c => c.Category == ExtensionCategory.ContentPack))
                    foreach (var item in capability.Content.Where(c => c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                        _entries.Add(new(item.Name, Path: ExtensionDiscovery.ResolveContent(package.Package.Directory, item.File),
                            SamplerCompatible: true, Package: id, Capability: capability.Id, Content: item.Id));
                _message = _entries.Count == 0 ? "Browser.NoMatches" : "Browser.Ready";
            }
            else if (resolved is null)
            {
                var state = _workflow.State;
                foreach (var resource in state.Snapshot.State.Resources.Where(r => r.Name.Contains(query, StringComparison.OrdinalIgnoreCase)))
                    _entries.Add(new(resource.Name, Resource: resource.Id, Lifecycle: state.Document.LifecycleId,
                        SamplerCompatible: resource.ManagedLocator?.StartsWith("wav/", StringComparison.Ordinal) == true));
                _message = _entries.Count == 0 ? "Browser.NoResources" : "Browser.Ready";
            }
            else
            {
                _directory = new(resolved, query);
                await LoadPageAsync(cancellation);
                _saved = _saved with { Recent = new[] { resolved }.Concat(_saved.Recent).Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToArray(), Last = resolved };
            }
            if (resolved is null || IsPack) _saved = _saved with { Last = null };
            RebuildLocations(); await SaveLocationsAsync();
        });
    }

    private async Task LoadPageAsync(CancellationToken cancellation)
    {
        if (_directory is null) return;
        _message = "Browser.Loading"; Refresh();
        var page = await _directory.NextAsync(cancellation);
        cancellation.ThrowIfCancellationRequested();
        foreach (var entry in page.Entries) _entries.Add(entry);
        _more.IsVisible = page.HasMore;
        _message = page.HasMore ? "Browser.MoreHint" : _entries.Count == 0 ? "Browser.NoMatches" : "Browser.Ready";
    }

    private void SelectSource()
    {
        if (_using) return;
        RenewCancellation();
        var cancellation = _cancellation.Token;
        _selected = _list.SelectedItem as BrowserSource;
        _wave.Waveform = null; _wave.InvalidateVisual(); _metadata.Text = "";
        Enqueue(() => _workflow.StopBrowserPreviewAsync());
        Refresh();
        if (_selected is not { Directory: false } source) return;
        Enqueue(async () =>
        {
            cancellation.ThrowIfCancellationRequested();
            _message = "Browser.Loading"; Refresh();
            var info = await _workflow.InspectBrowserSourceAsync(source, cancellation);
            cancellation.ThrowIfCancellationRequested();
            string format = source.Path is { } file ? Path.GetExtension(file).TrimStart('.').ToUpperInvariant() : "WAV";
            _metadata.Text = FormattableString.Invariant($"{info.Frames / (double)info.Rate:0.00} s · {(info.Channels == 1 ? "Mono" : "Stereo")}");
            ToolTip.SetTip(_metadata, FormattableString.Invariant($"{format} · {info.Rate:N0} Hz"));
            _wave.Waveform = info.Waveform; _wave.InvalidateVisual();
            _message = "Browser.Ready";
            if (_auto.IsChecked == true) await _workflow.PreviewBrowserSourceAsync(source, cancellation);
        });
    }

    private void ActivateSelected()
    { if (_selected is { Directory: true } folder) Navigate(folder.Path); else Preview(); }

    private void Preview()
    {
        if (_selected is not { Directory: false } source || _using) return;
        RenewCancellation();
        var cancellation = _cancellation.Token;
        Enqueue(async () => { await _workflow.PreviewBrowserSourceAsync(source, cancellation); });
    }

    private void StopPreview()
    { RenewCancellation(); Enqueue(() => _workflow.StopBrowserPreviewAsync()); }

    private void UseSelected(bool rack)
    {
        if (_selected is not { Directory: false } source || _using) return;
        RenewCancellation();
        var cancellation = _cancellation.Token;
        var state = _workflow.State;
        var basis = ArrangementEditBasis.From(state);
        _using = true; Refresh();
        Enqueue(async () =>
        {
            try
            {
                bool accepted = rack
                    ? await _workflow.UseBrowserRackAsync(basis, state.Selected, source, cancellation)
                    : await _workflow.UseBrowserAudioAsync(basis, source, new(state.SongCursor),
                        FirstFreeLane(state), cancellation);
                if (accepted) _openPane(rack ? WorkspaceState.BeatId : WorkspaceState.ArrangementId);
                else _message = _workflow.State.StatusKey;
            }
            finally { _using = false; }
        });
    }

    private static int FirstFreeLane(WorkflowState state) => Enumerable.Range(0, 32).Where(lane =>
        !state.Snapshot.State.AudioClips.Any(c => c.Lane == lane) &&
        !state.Snapshot.State.ArrangementClips.Any(c => c.Lane == lane)).Select(lane => (int?)lane).FirstOrDefault()
        ?? throw new InvalidOperationException("Browser.NoFreeLane");

    private async Task AddFolderAsync()
    {
        if (TopLevel.GetTopLevel(this) is not { } top) return;
        var folders = await top.StorageProvider.OpenFolderPickerAsync(new() { Title = _shell["Browser.AddFolder"], AllowMultiple = false });
        try
        {
            if (folders.FirstOrDefault()?.TryGetLocalPath() is not { } path) return;
            _saved = _saved with { Roots = new[] { path }.Concat(_saved.Roots).Distinct(StringComparer.OrdinalIgnoreCase).Take(16).ToArray() };
            await SaveLocationsAsync(); RebuildLocations(); Navigate(path);
        }
        finally { foreach (var folder in folders) folder.Dispose(); }
    }

    private async Task SaveLocationsAsync() { await _store.SaveAsync(_saved); }
    private void RebuildLocations()
    {
        _refreshing = true;
        var locations = new[] { new Location(_shell["Browser.Project"], null) }
            .Concat(_workflow.Extensions.Status.Where(p => p.Availability == ExtensionAvailability.Available &&
                p.Package.Manifest!.Capabilities.Any(c => !c.Content.IsEmpty)).Select(p => new Location(
                    "∿ " + p.Package.Manifest!.DisplayName(_shell.Preferences.Language == HostLanguage.Russian), PackPrefix + p.Package.Identity)))
            .Concat(_saved.Roots.Select(p => new Location("▸ " + Path.GetFileName(Path.TrimEndingDirectorySeparator(p)), p)))
            .Concat(_saved.Recent.Where(p => !_saved.Roots.Contains(p, StringComparer.OrdinalIgnoreCase))
                .Select(p => new Location("↶ " + Path.GetFileName(Path.TrimEndingDirectorySeparator(p)), p))).ToArray();
        _locations.ItemsSource = locations;
        _locations.SelectedItem = locations.FirstOrDefault(l => l.Path == _currentPath);
        _refreshing = false;
    }

    private (PointerPressedEventArgs Event, Point Point, BrowserSource Source)? _drag;
    private async void DragMoved(object? sender, PointerEventArgs e)
    {
        if (_drag is not { } drag || _using ||
            !e.GetCurrentPoint(_list).Properties.IsLeftButtonPressed ||
            Math.Abs(e.GetPosition(_list).X - drag.Point.X) + Math.Abs(e.GetPosition(_list).Y - drag.Point.Y) < 6) return;
        _drag = null;
        using var transfer = new DataTransfer(); transfer.Add(DataTransferItem.Create(DragFormat, drag.Source));
        try { await DragDrop.DoDragDropAsync(drag.Event, transfer, DragDropEffects.Copy); }
        catch (Exception error) when (error is InvalidOperationException or NotSupportedException)
        { _message = "Browser.SourceUnavailable"; Refresh(); }
    }

    private void BrowserKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { StopPreview(); e.Handled = true; }
        else if (e.Key == Key.F && e.KeyModifiers == KeyModifiers.Control) { _search.Focus(); e.Handled = true; }
        else if (e.Key == Key.Left && e.KeyModifiers == KeyModifiers.Alt && _history.TryPop(out var path)) { Navigate(path, false); e.Handled = true; }
        else if (e.Key == Key.R && e.KeyModifiers == KeyModifiers.Control)
        { Navigate(_currentPath, false); e.Handled = true; }
        else if (_list.IsKeyboardFocusWithin && e.Key is Key.Enter or Key.Space) { ActivateSelected(); e.Handled = true; }
        else if (_list.IsKeyboardFocusWithin && e.Key == Key.Back) { Navigate(!IsPack && _currentPath is { } p ? Directory.GetParent(p)?.FullName : null); e.Handled = true; }
    }

    private void WorkflowChanged(WorkflowState state) => Dispatcher.UIThread.Post(() =>
    {
        if (_disposed) return;
        if (_lifecycle != state.Document.LifecycleId)
        { _lifecycle = state.Document.LifecycleId; if (!_using) Navigate(_currentPath, false); }
        else if (_currentPath is null && !_using && !_entries.Select(e => e.Resource).SequenceEqual(
            state.Snapshot.State.Resources.Where(r => r.Name.Contains(_search.Text ?? "", StringComparison.OrdinalIgnoreCase)).Select(r => (Id<ResourceDescriptor>?)r.Id)))
            Navigate(null, false);
        Refresh();
    });
    private void PreferencesChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_disposed) return;
        RebuildLocations();
        if (_currentPath is null) _path.Text = _shell["Browser.Project"];
        _wave.InvalidateVisual(); Refresh();
    }

    private void Refresh()
    {
        _extensionDisclosure.Header = _shell["Extension.Title"];
        foreach (var (button, key) in new[] { (_add, "Browser.AddFolder"), (_remove, "Browser.RemoveFolder"), (_back, "Browser.Back"),
            (_up, "Browser.Up"), (_refresh, "Browser.Refresh"), (_play, "Browser.Preview"), (_stop, "Browser.Stop"), (_cancel, "Browser.Cancel") })
        { AutomationProperties.SetName(button, _shell[key]); ToolTip.SetTip(button, _shell[key] + (button == _refresh ? " · Ctrl+R" : "")); }
        _search.PlaceholderText = _shell["Browser.Search"];
        AutomationProperties.SetName(_search, _shell["Browser.Search"]);
        AutomationProperties.SetName(_locations, _shell["Browser.Locations"]);
        AutomationProperties.SetName(_path, _shell["Browser.Path"]);
        _auto.Content = _shell["Browser.AutoPreview"];
        _auto.IsChecked = _shell.Controls.AutoPreview;
        _more.Content = _shell["Browser.More"];
        string message = _workflow.State.BrowserPreviewPlaying ? "Browser.Previewing" : _message;
        _status.Text = message is "Browser.Ready" or "Browser.MoreHint" ? "" : _shell[message];
        _status.IsVisible = _status.Text.Length > 0;
        _notice.Text = _store.Failed ? _shell["Browser.LocationSaveFailed"] : "";
        _notice.IsVisible = _notice.Text.Length > 0;
        _title.Text = _selected?.Name ?? "";
        ToolTip.SetTip(_title, _selected?.Path ?? _selected?.Name);
        _rack.Content = _shell["Browser.UseRack"]; _playlist.Content = _shell["Browser.UsePlaylist"];
        ToolTip.SetTip(_rack, (_selected?.SamplerCompatible == false ? _shell["Browser.RackWavOnly"] : _shell["Browser.RackHint"]) +
            " · " + (_workflow.State.Occurrences.FirstOrDefault(o => o.Id == _workflow.State.Selected)?.Label ?? _shell["Beat.New"]));
        ToolTip.SetTip(_playlist, _shell["Browser.PlaylistHint"]);
        bool selected = _selected is { Directory: false };
        _preview.IsVisible = selected;
        _rack.IsEnabled = selected && _selected!.SamplerCompatible && !_using && !_workflow.State.Busy;
        _playlist.IsEnabled = selected && !_using && !_workflow.State.Busy;
        _play.IsEnabled = selected && !_using && _workflow.State.Execution?.Execution.Playing != true;
        _back.IsEnabled = _history.Count > 0 && !_using;
        _up.IsEnabled = _currentPath is not null && !_using;
        _remove.IsEnabled = _currentPath is not null && !IsPack && !_using;
        _cancel.IsVisible = _using || !PendingCommand.IsCompleted;
        _list.IsEnabled = !_using; _locations.IsEnabled = !_using; _search.IsEnabled = !_using;
        _more.IsEnabled = !_using && PendingCommand.IsCompleted;
    }

    internal void FocusBrowser() => _list.Focus();
    internal void Suspend()
    {
        if (_disposed || _suspended) return;
        _suspended = true; _searchDelay.Stop(); StopPreview();
    }
    private bool _suspended;
    internal void Resume() => _suspended = false;
    internal async Task CancelPendingAsync()
    {
        _cancellation.Cancel(); _searchDelay.Stop(); await PendingCommand;
        RenewCancellation();
    }
    internal async Task ShutdownAsync()
    { await _extensions.Pending; await CancelPendingAsync(); _directory?.Dispose(); _directory = null; }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true; _cancellation.Cancel(); _searchDelay.Stop();
        _shell.PropertyChanged -= PreferencesChanged; _workflow.Changed -= WorkflowChanged;
        _extensions.Dispose();
        // MainWindow joins pending operations before disposing retained content.
        _directory?.Dispose(); _cancellation.Dispose();
    }
}
