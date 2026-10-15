// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Small host-owned Browser disclosure. All labels remain safe without an executable contributor.</summary>
internal sealed class ExtensionBrowserContent : StackPanel, IDisposable
{
    private readonly ShellSession _shell;
    private readonly DesktopWorkflow _workflow;
    private readonly ExtensionPreferencesStore _store;
    private readonly string _configurationDirectory;
    private readonly Action<ExtensionStatus?> _browse;
    private readonly ComboBox _packages = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private readonly Button _toggle = new(), _sounds = new();
    private readonly TextBlock _details = new() { Classes = { "caption" }, TextWrapping = TextWrapping.Wrap };
    private readonly HashSet<string> _disabled = new(StringComparer.Ordinal);
    private bool _refreshing, _disposed;
    private string? _message;
    internal Task Pending { get; private set; } = Task.CompletedTask;
    private sealed record Choice(ExtensionStatus Status, string Label) { public override string ToString() => Label; }

    internal ExtensionBrowserContent(ShellSession shell, DesktopWorkflow workflow, string configurationDirectory,
        Action<ExtensionStatus?> browse)
    {
        _shell = shell; _workflow = workflow; _browse = browse; _configurationDirectory = configurationDirectory;
        _store = new(Path.Combine(configurationDirectory, "extensions.json"));
        Classes.Add("tight");
        Children.Add(_packages); Children.Add(_details);
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(_sounds); actions.Children.Add(_toggle); Children.Add(actions);
        AutomationProperties.SetAutomationId(_packages, "Extension.Packages");
        AutomationProperties.SetAutomationId(_toggle, "Extension.Toggle");
        AutomationProperties.SetAutomationId(_sounds, "Extension.Sounds");
        _packages.SelectionChanged += (_, _) => { if (!_refreshing) { _message = null; RefreshDetails(); } };
        _sounds.Click += (_, _) => _browse((_packages.SelectedItem as Choice)?.Status);
        _toggle.Click += (_, _) => Run(ToggleAsync);
        shell.PropertyChanged += PreferencesChanged;
        AttachedToVisualTree += (_, _) => { if (!_initialized) { _initialized = true; Run(InitializeAsync); } };
    }
    private bool _initialized;
    private async Task InitializeAsync()
    {
        foreach (string id in await _store.LoadAsync()) _disabled.Add(id);
        await _workflow.InitializeExtensionsAsync(_configurationDirectory);
        Rebuild(); _browse(null);
    }
    private void Run(Func<Task> operation)
    {
        var previous = Pending;
        Pending = ExecuteAsync();
        async Task ExecuteAsync()
        {
            await Task.Yield(); await previous;
            if (_disposed) return;
            _toggle.IsEnabled = _sounds.IsEnabled = false;
            try { await operation(); }
            catch (Exception error) { _message = "Extension.OperationFailed"; ToolTip.SetTip(_details, error.Message); }
            finally { if (!_disposed) Rebuild(); }
        }
    }
    private async Task ToggleAsync()
    {
        if (_packages.SelectedItem is not Choice choice) return;
        string id = choice.Status.Package.Identity;
        bool disable = choice.Status.Availability == ExtensionAvailability.Available;
        if (disable) await _workflow.StopBrowserPreviewAsync();
        bool changed = await _workflow.SetExtensionEnabledAsync(id, !disable);
        if (!changed) { _message = disable ? "Extension.Busy" : "Extension.ActivationFailed"; return; }
        if (disable) _disabled.Add(id); else _disabled.Remove(id);
        await _store.SaveAsync(_disabled);
        // Clear contributed sources/selection before presenting the new availability.
        _browse(null);
    }
    private void Rebuild()
    {
        string? selected = (_packages.SelectedItem as Choice)?.Status.Package.Directory;
        _refreshing = true;
        var choices = _workflow.Extensions.Status.Select(p => new Choice(p, PackageName(p) + " · " + p.Package.Manifest?.Version)).ToArray();
        _packages.ItemsSource = choices;
        _packages.SelectedItem = choices.FirstOrDefault(p => p.Status.Package.Directory == selected) ?? choices.FirstOrDefault();
        _refreshing = false; RefreshDetails();
    }
    private string PackageName(ExtensionStatus status) => status.Package.Manifest is { } manifest
        ? manifest.DisplayName(_shell.Preferences.Language == HostLanguage.Russian)
        : status.Package.Identity;
    private void RefreshDetails()
    {
        var selected = (_packages.SelectedItem as Choice)?.Status;
        _toggle.Content = _shell[selected?.Availability == ExtensionAvailability.Available ? "Extension.Disable" : "Extension.Enable"];
        _sounds.Content = _shell["Extension.Sounds"];
        _toggle.IsEnabled = selected?.Availability is ExtensionAvailability.Available or ExtensionAvailability.Disabled or ExtensionAvailability.Failed;
        _sounds.IsEnabled = selected?.Availability == ExtensionAvailability.Available &&
            selected.Package.Manifest!.Capabilities.Any(c => !c.Content.IsEmpty);
        string status = selected is null ? _shell["Extension.None"] : _shell["Extension.State." + selected.Availability];
        _details.Text = _shell[_store.Failed ? "Extension.PreferencesFailed" : _message ??
            (selected is null ? "Extension.None" : "Extension.State." + selected.Availability)];
        ToolTip.SetTip(_details, selected is null ? status : selected.Package.Identity + "\n" +
            string.Join("\n", selected.Diagnostics.Select(d => _shell[d.Code] + (d.Detail is null ? "" : " · " + d.Detail))));
        ToolTip.SetTip(_toggle, _shell["Extension.DisableHint"]);
    }
    private void PreferencesChanged(object? sender, PropertyChangedEventArgs args) { if (!_disposed) Rebuild(); }
    public void Dispose() { _disposed = true; _shell.PropertyChanged -= PreferencesChanged; }
}
