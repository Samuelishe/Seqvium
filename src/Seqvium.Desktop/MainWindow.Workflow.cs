// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop;

internal sealed partial class MainWindow
{
    private DesktopWorkflow _workflow = null!;
    private WorkflowControls _workflowControls = null!;
    private Task _workflowWork = Task.CompletedTask;
    private bool _workflowActionBusy;
    private CancellationTokenSource? _importCancellation;
    private Guid _observedRevision;
    private string? _observedPath;
    private bool _observedDirty;

    private void InitializeWorkflow()
    {
        _session.HasWorkflowOwner = true;
        _workflow = new(_session.Document);
        _workflowControls = new(_session, _workflow, WorkflowActionAsync);
        WorkflowRegion.Child = _workflowControls;
        WorkflowStatusRegion.Child = _workflowControls.StatusControl;
        PaneMenuAction.Content = Presentation.HostIcons.Create("Panes", PaneMenuAction);
        ThemeAction.Content = Presentation.HostIcons.Create("Theme", ThemeAction);
        AboutAction.Content = Presentation.HostIcons.Create("Info", AboutAction);
        MinimizeAction.Content = Presentation.HostIcons.Create("Minimize", MinimizeAction, 16);
        CloseAction.Content = Presentation.HostIcons.Create("Close", CloseAction, 16);
        _workflow.Changed += WorkflowChanged;
        _session.PropertyChanged += (_, _) => _workflowControls.Refresh(_closing || _workflowActionBusy);
        AddHandler(KeyDownEvent, WorkflowKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private void WorkflowChanged(WorkflowState state) => Dispatcher.UIThread.Post(() =>
    {
        if (_shutdownComplete) return;
        // Read latest observation; do not replay stale queued states after a document replacement.
        var latest = _workflow.State;
        _session.ObserveAudio(WorkflowPresentation.CompactStatus(latest));
        if (_observedRevision != latest.Snapshot.Revision || _observedPath != latest.SavedPath ||
            _observedDirty != latest.IsDirty)
        {
            _observedRevision = latest.Snapshot.Revision; _observedPath = latest.SavedPath;
            _observedDirty = latest.IsDirty;
            _session.ObserveDocument(latest.Document, latest.Snapshot, latest.SavedPath, latest.IsDirty, latest.HasSaved);
        }
        _workflowControls.Refresh(_closing || _workflowActionBusy);
    });

    private Task WorkflowActionAsync(string action)
    {
        if (_closing) return Task.CompletedTask;
        if (action is "Stop" or "Panic") return _workflow.StopAsync(action == "Panic");
        if (action == "CancelImport") { _importCancellation?.Cancel(); return Task.CompletedTask; }
        if (_workflowActionBusy) return Task.CompletedTask;
        _workflowWork = RunWorkflowActionAsync(action);
        return _workflowWork;
    }

    private async Task RunWorkflowActionAsync(string action)
    {
        _workflowActionBusy = true;
        _workflowControls.Refresh(true);
        try
        {
            switch (action)
            {
                case "Import":
                    var wav = await PickOpenAsync(true);
                    if (wav is null || _closing) break;
                    _importCancellation = new();
                    _workflowControls.CanCancelImport = true;
                    _workflowControls.Refresh(true);
                    await _workflow.ImportAsync(wav, _importCancellation.Token);
                    break;
                case "Open":
                    var path = await PickOpenAsync(false);
                    if (path is null || _closing) break;
                    var decision = await ReplacementDecisionAsync();
                    await _workflow.OpenAsync(path, decision.Decision, decision.SavePath);
                    break;
                case "Save":
                case "SaveAs":
                    var destination = action == "Save" ? _workflow.State.SavedPath : null;
                    destination ??= await PickSaveAsync();
                    if (destination is not null) await _workflow.SaveAsync(destination);
                    break;
                case "Play": await _workflow.PlayAsync(); break;
                case "Undo": await _workflow.UndoAsync(); break;
                case "Redo": await _workflow.UndoAsync(true); break;
            }
        }
        catch (Exception error) { await _workflow.ReportFailureAsync(error.Message); }
        finally
        {
            _importCancellation?.Dispose(); _importCancellation = null;
            _workflowControls.CanCancelImport = false;
            _workflowActionBusy = false;
            _workflowControls.Refresh(_closing);
            if (!_closing) PaneMenuAction.Focus();
        }
    }

    private async Task<string?> PickOpenAsync(bool wav)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new()
        {
            Title = _session[wav ? "Workflow.Import" : "Workflow.Open"], AllowMultiple = false,
            FileTypeFilter = [new(wav ? "WAV" : "Seqvium") { Patterns = [wav ? "*.wav" : "*.seqvium"] }]
        });
        using var file = files.FirstOrDefault();
        if (file is null) return null;
        return file.TryGetLocalPath() ?? throw new IOException(_session["Workflow.LocalFileRequired"]);
    }

    private async Task<string?> PickSaveAsync()
    {
        using var file = await StorageProvider.SaveFilePickerAsync(new()
        {
            Title = _session["Workflow.SaveAs"], DefaultExtension = "seqvium",
            SuggestedFileName = Path.GetFileName(_workflow.State.SavedPath ?? "Project.seqvium"),
            FileTypeChoices = [new("Seqvium") { Patterns = ["*.seqvium"] }], ShowOverwritePrompt = true
        });
        return file is null ? null : file.TryGetLocalPath() ??
            throw new IOException(_session["Workflow.LocalFileRequired"]);
    }

    private async Task<(UnsavedDecision Decision, string? SavePath)> ReplacementDecisionAsync()
    {
        if (!_workflow.State.IsDirty) return (UnsavedDecision.Discard, null);
        var dialog = new UnsavedProjectDialog(_session);
        var decision = await dialog.ShowDialog<UnsavedDecision?>(this) ?? UnsavedDecision.Cancel;
        if (!_closing) PaneMenuAction.Focus();
        if (decision != UnsavedDecision.Save) return (decision, null);
        var path = _workflow.State.SavedPath ?? await PickSaveAsync();
        return path is null ? (UnsavedDecision.Cancel, null) : (decision, path);
    }

    private async void WorkflowKeyDown(object? sender, KeyEventArgs args)
    {
        if (_closing || args.Handled) return;
        string? action = (args.Key, args.KeyModifiers) switch
        {
            (Key.O, KeyModifiers.Control) => "Open", (Key.I, KeyModifiers.Control) => "Import",
            (Key.S, KeyModifiers.Control) => "Save",
            (Key.S, KeyModifiers.Control | KeyModifiers.Shift) => "SaveAs",
            (Key.Z, KeyModifiers.Control) => "Undo", (Key.Y, KeyModifiers.Control) => "Redo",
            (Key.Escape, KeyModifiers.Control) => "Panic", _ => null
        };
        if (args.Key == Key.F5 && args.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift)
            action = args.KeyModifiers == KeyModifiers.Shift ? "Stop" : "Play";
        if (action is null) return;
        args.Handled = true; await WorkflowActionAsync(action);
    }
}
