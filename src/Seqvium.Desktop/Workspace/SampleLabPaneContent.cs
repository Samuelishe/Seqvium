// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Seqvium.Core;
using Seqvium.Desktop.Extensions;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Sound-first retained exploration surface. Every musical change goes through the workflow owner.</summary>
internal sealed class SampleLabPaneContent : Grid, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly ComboBox _family = new() { MinWidth = 115 };
    private readonly ListBox _history = new() { MinHeight = 100 };
    private readonly BrowserWaveform _wave;
    private readonly TextBlock _identity = new() { Classes = { "section" }, TextWrapping = TextWrapping.Wrap },
        _recipe = new() { Classes = { "caption", "numeric" }, TextWrapping = TextWrapping.Wrap },
        _reference = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap },
        _destination = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap },
        _status = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap },
        _audible = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap },
        _retention = new() { Classes = { "caption", "secondary" }, TextWrapping = TextWrapping.Wrap };
    private readonly Dictionary<string, Button> _buttons = [];
    private readonly Dictionary<string, TextBlock> _captions = [];
    private readonly StackPanel _controls = new() { Classes = { "tight" } };
    private readonly Expander _shape = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly Dictionary<string, (CheckBox Lock, TextBlock Label, NumericUpDown Value, TextBlock Unit)> _fields = [];
    private readonly Grid _body = new() { ColumnDefinitions = new("*,250"), ColumnSpacing = 12, RowSpacing = 8 };
    private readonly ScrollViewer _sound;
    private readonly Grid _recent;
    private bool _refreshing, _disposed, _initialized;
    private string? _fieldFamily;
    private Guid[] _listed = [];
    private string? _feedback;
    internal Task Pending { get; private set; } = Task.CompletedTask;

    internal SampleLabPaneContent(ShellSession shell, DesktopWorkflow workflow, string configurationDirectory, Action<string> openPane)
    {
        _shell = shell; _workflow = workflow;
        RowDefinitions = new("Auto,*,Auto,Auto");
        this.Bind(MarginProperty, this.GetResourceObservable("Padding.Content"));
        this.Bind(RowSpacingProperty, this.GetResourceObservable("Space.Medium"));
        var generation = new WrapPanel { Orientation = Orientation.Horizontal };
        generation.Children.Add(_family);
        generation.Children.Add(Button("Lab.Random", () => workflow.GenerateLabAsync(SampleExplorationKind.Random), "primary"));
        generation.Children.Add(Button("Lab.Similar", () => workflow.GenerateLabAsync(SampleExplorationKind.Similar)));
        generation.Children.Add(Button("Lab.Cancel", workflow.CancelLabAsync));
        generation.Children.Add(Button("Lab.Extensions", () => { openPane(WorkspaceState.BrowserId); return Task.FromResult(true); }));
        Children.Add(generation);
        _wave = new(shell) { Height = 112, IsHitTestVisible = false };
        var sound = new StackPanel { Classes = { "spaced" } };
        sound.Children.Add(_identity); sound.Children.Add(_recipe); sound.Children.Add(_wave);
        var audition = new WrapPanel();
        audition.Children.Add(Button("Lab.Raw", () => Audition(false)));
        audition.Children.Add(Button("Lab.Context", () => Audition(true)));
        audition.Children.Add(Button("Lab.Original", workflow.RestoreLabSoundAsync));
        sound.Children.Add(audition); sound.Children.Add(_audible);
        var referenceRow = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "compact" } };
        referenceRow.Children.Add(Button("Lab.UseReference", () => _workflow.ConfigureLabAsync(reference: SelectedId())));
        _reference.VerticalAlignment = VerticalAlignment.Center; referenceRow.Children.Add(_reference); sound.Children.Add(referenceRow);
        sound.Children.Add(_destination);
        _shape.Content = _controls; sound.Children.Add(_shape);
        var acceptance = new WrapPanel();
        acceptance.Children.Add(Button("Lab.Keep", () => Accept(LabAcceptance.Resource)));
        acceptance.Children.Add(Button("Lab.Rack", () => Accept(LabAcceptance.Rack)));
        acceptance.Children.Add(Button("Lab.Playlist", () => Accept(LabAcceptance.Playlist)));
        acceptance.Children.Add(Button("Lab.ReplaceShared", () => Accept(LabAcceptance.SharedSound)));
        acceptance.Children.Add(Button("Lab.Standalone", () => workflow.SetLabTargetAsync()));
        SetRow(acceptance, 2); Children.Add(acceptance);
        _sound = new() { Content = sound, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        _recent = new() { RowDefinitions = new("Auto,*,Auto"), RowSpacing = 6 };
        var recentTitle = new TextBlock { Classes = { "section" } };
        _recent.Children.Add(recentTitle); SetRow(_history, 1); _recent.Children.Add(_history);
        SetRow(_retention, 2); _recent.Children.Add(_retention);
        _body.Children.Add(_sound); SetColumn(_recent, 1); _body.Children.Add(_recent);
        SetRow(_body, 1); Children.Add(_body); SetRow(_status, 3); Children.Add(_status);
        AutomationProperties.SetAutomationId(_family, "Lab.Family");
        AutomationProperties.SetAutomationId(_history, "Lab.History");
        _history.ItemTemplate = new FuncDataTemplate<LabEntry>((entry, _) =>
        {
            var label = new TextBlock { TextWrapping = TextWrapping.Wrap };
            if (entry is not null)
            {
                label.Text = entry.Name + (entry.Accepted > 0 ? "  ✓" : "") +
                    (entry.Recipe.Reference is { } parent ? "\n← " + ReferenceName(parent) : "");
                ToolTip.SetTip(label, entry.Accepted > 0 ? _shell["Lab.Saved"] : _shell["Lab.Temporary"]);
            }
            return label;
        });
        _history.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _history.SelectedItem is LabEntry entry) Run(() => workflow.ConfigureLabAsync(selection: entry.Id));
        };
        _family.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _family.SelectedItem is string family) Run(() => workflow.ConfigureLabAsync(family: family));
        };
        SizeChanged += (_, _) =>
        {
            bool compact = Bounds.Width < 680;
            _body.ColumnDefinitions = new(compact ? "*" : "*,250");
            _body.RowDefinitions = new(compact ? "*,150" : "*");
            SetColumn(_recent, compact ? 0 : 1); SetRow(_recent, compact ? 1 : 0);
        };
        workflow.Changed += WorkflowChanged; shell.PropertyChanged += PreferencesChanged;
        foreach (var (key, button) in _buttons)
        {
            string? icon = key switch
            {
                "Lab.Cancel" or "Lab.Standalone" => "Close", "Lab.Extensions" => "More", "Lab.UseReference" => "Reference",
                "Lab.Raw" => "Play", "Lab.Original" => "Undo", "Lab.Keep" => "Save", _ => null
            };
            bool iconOnly = key is "Lab.Cancel" or "Lab.Standalone" or "Lab.Extensions" or "Lab.UseReference";
            if (iconOnly)
            { button.Classes.Add("command"); button.Content = HostIcons.Create(icon!, button, 16); }
            else
            {
                var caption = new TextBlock(); _captions.Add(key, caption);
                var content = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "compact" } };
                if (icon is not null) content.Children.Add(HostIcons.Create(icon, button, 16));
                content.Children.Add(caption); button.Content = content;
            }
        }
        AttachedToVisualTree += (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            Run(async () => { await workflow.InitializeExtensionsAsync(configurationDirectory); return await workflow.RefreshLabGeneratorAsync(); });
        };
        Refresh();
        recentTitle.Text = shell["Lab.Recent"];
    }
    private static string F(decimal value, string format = "0.####") => value.ToString(format, CultureInfo.InvariantCulture);
    private Guid? SelectedId() => _workflow.State.Lab?.Selected;
    private Task<bool> Audition(bool contextual) => SelectedId() is { } id ? _workflow.AuditionLabAsync(id, contextual) : Task.FromResult(false);
    private Task<bool> Accept(LabAcceptance scope)
    {
        var state = _workflow.State;
        return SelectedId() is { } id ? _workflow.AcceptLabAsync(state.Document.LifecycleId, id, scope,
            scope == LabAcceptance.Rack ? state.Selected : null) : Task.FromResult(false);
    }
    private Button Button(string key, Func<Task<bool>> action, string? style = null)
    {
        var button = new Button();
        if (style is not null) button.Classes.Add(style);
        button.Bind(MarginProperty, button.GetResourceObservable("Padding.Input"));
        AutomationProperties.SetAutomationId(button, key);
        button.Click += (_, _) => Run(action);
        _buttons.Add(key, button); return button;
    }
    private void Run(Func<Task<bool>> action)
    {
        _feedback = null;
        Pending = ExecuteAsync();
        async Task ExecuteAsync()
        {
            try { await action(); }
            catch (Exception error) { if (!_disposed) { _feedback = "Lab.Failed"; ToolTip.SetTip(_status, error.Message); } }
            finally { if (!_disposed) Refresh(); }
        }
    }
    private void BuildControls(SampleFamily family)
    {
        _controls.Children.Clear(); _fields.Clear(); _fieldFamily = family.Id;
        foreach (var descriptor in family.Controls)
        {
            var row = new Grid { ColumnDefinitions = new("Auto,*,120,Auto"), Classes = { "parameterRow" }, ColumnSpacing = 6 };
            var held = new CheckBox(); var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            var value = new NumericUpDown { Minimum = descriptor.Minimum, Maximum = descriptor.Maximum,
                Increment = descriptor.Maximum <= 1 ? .01m : 1, FormatString = "0.####",
                ShowButtonSpinner = false, NumberFormat = CultureInfo.InvariantCulture.NumberFormat };
            var unit = new TextBlock { Text = descriptor.Unit, MinWidth = 24, VerticalAlignment = VerticalAlignment.Center, Classes = { "caption" } };
            AutomationProperties.SetAutomationId(held, "Lab.Lock." + descriptor.Id);
            AutomationProperties.SetAutomationId(value, "Lab.Value." + descriptor.Id);
            row.Children.Add(held); SetColumn(label, 1); row.Children.Add(label); SetColumn(value, 2); row.Children.Add(value); SetColumn(unit, 3); row.Children.Add(unit);
            _fields.Add(descriptor.Id, (held, label, value, unit)); _controls.Children.Add(row);
            held.IsCheckedChanged += (_, _) =>
            {
                if (!_refreshing) Run(() => _workflow.ConfigureLabAsync(control: descriptor.Id,
                    held: held.IsChecked == true ? value.Value ?? descriptor.Default : null, changeLock: true));
            };
            value.ValueChanged += (_, _) =>
            {
                if (!_refreshing && held.IsChecked == true) Run(() => _workflow.ConfigureLabAsync(control: descriptor.Id, held: value.Value, changeLock: true));
            };
        }
    }
    private void WorkflowChanged(WorkflowState state) => Dispatcher.UIThread.Post(() => { if (!_disposed) Refresh(); });
    private string ReferenceName(Guid id) => _workflow.State.Lab?.Entries.SingleOrDefault(e => e.Id == id)?.Name ?? _shell["Lab.Reference"];
    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args) { _listed = []; Refresh(); }
    private void Refresh()
    {
        if (_disposed) return;
        var workflow = _workflow.State;
        var state = workflow.Lab;
        _refreshing = true;
        try
        {
            foreach (var (key, button) in _buttons)
            {
                if (_captions.TryGetValue(key, out var caption)) caption.Text = _shell[key];
                AutomationProperties.SetName(button, _shell[key]); ToolTip.SetTip(button, _shell[key]);
            }
            _shape.Header = _shell["Lab.Shape"] + (state?.Locks.Count > 0 ? " · " + _shell["Lab.Held"] : "");
            _retention.Text = _shell["Lab.Temporary"]; ToolTip.SetTip(_retention, _shell["Lab.Retention"]);
            ((TextBlock)_recent.Children[0]).Text = _shell["Lab.Recent"];
            bool available = _workflow.Extensions.Status.Any(p => p.Package.Identity == PercussionGenerator.Package && p.Availability == ExtensionAvailability.Available);
            _buttons["Lab.Random"].IsEnabled = available && !workflow.Busy;
            _buttons["Lab.Similar"].IsEnabled = available && !workflow.Busy && state?.Reference is not null;
            _buttons["Lab.Cancel"].IsEnabled = state?.Busy == true;
            var selected = state?.Entries.SingleOrDefault(e => e.Id == state.Selected);
            foreach (string key in new[] { "Lab.Raw", "Lab.Context", "Lab.UseReference", "Lab.Keep", "Lab.Rack", "Lab.Playlist", "Lab.ReplaceShared" })
                _buttons[key].IsEnabled = selected is not null && !workflow.Busy;
            _buttons["Lab.UseReference"].IsEnabled = selected?.Recipe.Family == state?.Family && selected is not null;
            _buttons["Lab.Context"].IsEnabled &= state?.TargetValid == true;
            _buttons["Lab.ReplaceShared"].IsEnabled &= state?.TargetValid == true;
            _buttons["Lab.Raw"].IsEnabled &= workflow.Execution?.Execution.Playing != true;
            _family.IsEnabled = available;
            if (state is null) return;
            if (!_family.Items.Cast<string>().SequenceEqual(state.Families.Select(f => f.Id))) _family.ItemsSource = state.Families.Select(f => f.Id).ToArray();
            _family.SelectedItem = state.Family;
            if (!_listed.SequenceEqual(state.Entries.Select(e => e.Id)) || state.Entries.Any(e =>
                    _history.Items.OfType<LabEntry>().Any(old => old.Id == e.Id && old.Accepted != e.Accepted)))
            { _listed = state.Entries.Select(e => e.Id).ToArray(); _history.ItemsSource = state.Entries.Reverse().ToArray(); }
            _history.SelectedItem = _history.Items.OfType<LabEntry>().SingleOrDefault(e => e.Id == state.Selected);
            _identity.Text = selected?.Name ?? _shell["Lab.Start"];
            _recipe.Text = selected is null ? "" : F((decimal)selected.Frames / selected.Rate, "0.00") + " s";
            ToolTip.SetTip(_recipe, selected is null ? null : selected.Rate + " Hz · mono");
            _wave.Waveform = selected?.Waveform; _wave.InvalidateVisual();
            var reference = state.Entries.SingleOrDefault(e => e.Id == state.Reference);
            _reference.Text = reference?.Name ?? _shell["Lab.Reference"];
            ToolTip.SetTip(_reference, _shell["Lab.Reference"]);
            _destination.Text = state.Target is null ? "" : state.Target + (state.TargetValid ? "" : " · " + _shell["Lab.TargetStale"]);
            _destination.IsVisible = state.Target is not null;
            _buttons["Lab.Standalone"].IsVisible = state.Target is not null;
            _buttons["Lab.Context"].IsVisible = state.Target is not null;
            _buttons["Lab.ReplaceShared"].IsVisible = state.Target is not null;
            ToolTip.SetTip(_buttons["Lab.ReplaceShared"], _shell["Lab.SharedHint"]);
            var audible = state.Entries.SingleOrDefault(e => e.Id == state.Audible);
            _audible.Text = audible is null ? "" : "▶ " + audible.Name + " · " + _shell[state.Contextual ? "Lab.Context" : "Lab.Raw"];
            _audible.IsVisible = audible is not null;
            _buttons["Lab.Raw"].Classes.Set("active", audible is not null && !state.Contextual);
            _buttons["Lab.Context"].Classes.Set("active", audible is not null && state.Contextual);
            bool quiet = state.Message is "Lab.Start" or "Lab.Ready" or "Lab.Accepted" or "Lab.ReferenceSet" or "Lab.FamilyChanged" or "Lab.Original" or "Lab.RawPlaying" or "Lab.InContext" or "Workflow.Cancelled";
            _status.IsVisible = _feedback is not null || !available || !quiet;
            _status.Text = _shell[_feedback ?? (available ? state.Message : "Lab.Unavailable")];
            var family = state.Families.SingleOrDefault(f => f.Id == state.Family);
            if (family is not null)
            {
                if (_fieldFamily != family.Id) BuildControls(family);
                foreach (var descriptor in family.Controls)
                {
                    var field = _fields[descriptor.Id];
                    bool locked = state.Locks.TryGetValue(descriptor.Id, out var value);
                    field.Label.Text = _shell["Lab.Control." + descriptor.Id];
                    AutomationProperties.SetName(field.Lock, _shell["Lab.Held"] + " " + field.Label.Text);
                    ToolTip.SetTip(field.Lock, _shell["Lab.LockHint"]);
                    field.Lock.IsChecked = locked;
                    field.Value.Value = locked ? value : selected?.Recipe.Family == family.Id ? selected.Recipe.Values[descriptor.Id] : descriptor.Default;
                    field.Value.IsEnabled = locked;
                }
            }
        }
        finally { _refreshing = false; }
    }
    public void Dispose()
    { _disposed = true; _workflow.Changed -= WorkflowChanged; _shell.PropertyChanged -= PreferencesChanged; }
    internal void Suspend()
    {
        if (_workflow.State.Lab?.Audible is not null || _workflow.State.Lab?.Contextual == true)
            Run(_workflow.RestoreLabSoundAsync);
    }
}
