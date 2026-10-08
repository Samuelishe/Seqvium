// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;

namespace Seqvium.Desktop.Workspace;

public enum PaneVisibility
{
    Hidden,
    Visible,
    Collapsed
}

public enum PaneDock
{
    Floating,
    Left,
    Right
}

public readonly record struct PaneBounds(double X, double Y, double Width, double Height);

public sealed record PaneDefinition(
    string InstanceId,
    string TypeId,
    string TitleKey,
    double MinimumWidth,
    double MinimumHeight,
    PaneBounds DefaultBounds);

public sealed record PanePlacement(
    string InstanceId,
    string TypeId,
    PaneVisibility Visibility,
    PaneBounds FloatingBounds,
    PaneDock Dock,
    bool AllowDocking)
{
    // Independent of the retained floating rectangle; old version-1 layouts omit this field.
    public double? DockedWidth { get; init; }
}

public sealed record WorkspaceLayout(
    double Width,
    double Height,
    string? ActivePaneId,
    ImmutableArray<PanePlacement> Panes);

/// <summary>
/// Owner-thread geometry/visibility only. Ordered entries run back to front; content, focus,
/// document selection and semantic editing targets are deliberately absent.
/// </summary>
public sealed class WorkspaceState : IDisposable
{
    public const string InspectorId = "project-inspector.main";
    public const string AppearanceId = "appearance.main";

    public static ImmutableArray<PaneDefinition> Definitions { get; } =
    [
        new(InspectorId, "project-inspector", "Pane.Inspector", 280, 210, new(34, 30, 340, 280)),
        new(AppearanceId, "appearance", "Pane.Appearance", 280, 170, new(270, 145, 320, 220))
    ];

    private readonly List<PanePlacement> _panes = Definitions.Select(definition =>
        new PanePlacement(definition.InstanceId, definition.TypeId, PaneVisibility.Hidden,
            definition.DefaultBounds, PaneDock.Floating, true)).ToList();

    private WorkspaceLayout? _restored;
    public double Width { get; private set; } = 1;
    public double Height { get; private set; } = 1;
    public string? ActivePaneId { get; private set; }
    public bool IsDisposed { get; private set; }
    public ImmutableArray<PanePlacement> Panes => [.. _panes];
    public event Action? Changed;

    public WorkspaceState(WorkspaceLayout? restored = null) => _restored = restored;

    public PanePlacement Get(string id) => _panes.First(pane => pane.InstanceId == id);
    public static PaneDefinition Definition(string id) => Definitions.First(pane => pane.InstanceId == id);

    public void Reflow(double width, double height)
    {
        CheckAvailable();
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0) return;
        if (Width == width && Height == height && _restored is null) return;
        Width = width;
        Height = height;
        if (_restored is { } layout)
        {
            _restored = null;
            var entries = new List<PanePlacement>();
            foreach (var saved in layout.Panes)
            {
                var definition = Definitions.FirstOrDefault(item => item.InstanceId == saved.InstanceId &&
                                                                    item.TypeId == saved.TypeId);
                if (definition is null || entries.Any(item => item.InstanceId == saved.InstanceId)) continue;
                var bounds = saved.FloatingBounds;
                // Scale placement within the previous usable area; retain readable DIP sizes.
                bounds = bounds with
                {
                    X = bounds.X * width / Math.Max(1, layout.Width),
                    Y = bounds.Y * height / Math.Max(1, layout.Height)
                };
                var dock = saved.AllowDocking ? saved.Dock : PaneDock.Floating;
                if (saved.Visibility == PaneVisibility.Visible && dock != PaneDock.Floating && entries.Any(item =>
                        item.Dock == dock &&
                        item.Visibility == PaneVisibility.Visible))
                    dock = PaneDock.Floating;
                entries.Add(saved with { FloatingBounds = Clamp(bounds, definition), Dock = dock });
            }

            entries.AddRange(_panes.Where(pane => entries.All(item => item.InstanceId != pane.InstanceId)));
            _panes.Clear();
            _panes.AddRange(entries);
            ActivePaneId = _panes.Any(pane => pane.InstanceId == layout.ActivePaneId &&
                                              pane.Visibility == PaneVisibility.Visible)
                ? layout.ActivePaneId
                : null;
            if (ActivePaneId is not null) BringForward(ActivePaneId);
            SelectAvailable();
        }

        for (var index = 0; index < _panes.Count; index++)
        {
            var pane = _panes[index];
            _panes[index] = pane with
            {
                FloatingBounds = Clamp(pane.FloatingBounds, Definition(pane.InstanceId)),
                DockedWidth = pane.DockedWidth is { } dockedWidth
                    ? ClampDockWidth(pane.InstanceId, dockedWidth)
                    : pane.Dock != PaneDock.Floating
                        ? ClampDockWidth(pane.InstanceId, pane.FloatingBounds.Width)
                        : null
            };
        }

        Changed?.Invoke();
    }

    public PaneBounds Bounds(string id)
    {
        var pane = Get(id);
        if (pane.Dock == PaneDock.Floating) return pane.FloatingBounds;
        var width = pane.DockedWidth!.Value;
        return new(pane.Dock == PaneDock.Left ? 0 : Width - width, 0, width, Height);
    }

    private PaneBounds Clamp(PaneBounds bounds, PaneDefinition definition)
    {
        static double Valid(double value, double fallback) => double.IsFinite(value) ? value : fallback;
        var width = Math.Clamp(Valid(bounds.Width, definition.DefaultBounds.Width),
            Math.Min(definition.MinimumWidth, Width), Width);
        var height = Math.Clamp(Valid(bounds.Height, definition.DefaultBounds.Height),
            Math.Min(definition.MinimumHeight, Height), Height);
        return new(Math.Clamp(Valid(bounds.X, 0), 0, Width - width),
            Math.Clamp(Valid(bounds.Y, 0), 0, Height - height), width, height);
    }

    private double ClampDockWidth(string id, double width) => Math.Clamp(
        double.IsFinite(width) ? width : Definition(id).DefaultBounds.Width,
        Math.Min(Definition(id).MinimumWidth, Width / 2), Width / 2);

    public void Open(string id)
    {
        CheckAvailable();
        var pane = Get(id);
        Replace(pane with { Visibility = PaneVisibility.Visible });
        ResolveDockConflict(id);
        Activate(id);
    }

    public void Activate(string id)
    {
        CheckAvailable();
        if (Get(id).Visibility != PaneVisibility.Visible) return;
        if (ActivePaneId == id && _panes[^1].InstanceId == id) return;
        ActivePaneId = id;
        BringForward(id);
        Changed?.Invoke();
    }

    public string? SwitchPane(bool backwards = false)
    {
        CheckAvailable();
        // Traverse stable capability order, independent of front-order changes.
        var available = Definitions.Where(item => Get(item.InstanceId).Visibility != PaneVisibility.Hidden).ToArray();
        if (available.Length == 0) return null;
        var index = Array.FindIndex(available, item => item.InstanceId == ActivePaneId);
        if (index < 0 && backwards) index = 0;
        index = (index + (backwards ? available.Length - 1 : 1)) % available.Length;
        Open(available[index].InstanceId);
        return ActivePaneId;
    }

    public void Collapse(string id) => SetVisibility(id, PaneVisibility.Collapsed);
    public void Hide(string id) => SetVisibility(id, PaneVisibility.Hidden);

    private void SetVisibility(string id, PaneVisibility visibility)
    {
        CheckAvailable();
        Replace(Get(id) with { Visibility = visibility });
        SelectAvailable();
        Changed?.Invoke();
    }

    public void SetBounds(string id, PaneBounds bounds)
    {
        CheckAvailable();
        var pane = Get(id);
        if (pane.Visibility != PaneVisibility.Visible) return;
        var updated = pane.Dock == PaneDock.Floating
            ? pane with { FloatingBounds = Clamp(bounds, Definition(id)) }
            : pane with { DockedWidth = ClampDockWidth(id, bounds.Width) };
        if (updated == pane) return;
        Replace(updated);
        Changed?.Invoke();
    }

    internal void RestorePlacement(PanePlacement placement)
    {
        CheckAvailable();
        if (Get(placement.InstanceId) == placement) return;
        Replace(placement);
        Changed?.Invoke();
    }

    public void Dock(string id, PaneDock dock)
    {
        CheckAvailable();
        if (!Enum.IsDefined(dock)) throw new ArgumentOutOfRangeException(nameof(dock));
        var pane = Get(id);
        if (pane.Visibility != PaneVisibility.Visible || (!pane.AllowDocking && dock != PaneDock.Floating)) return;
        Replace(pane with
        {
            Dock = dock,
            DockedWidth = dock == PaneDock.Floating
                ? pane.DockedWidth
                : ClampDockWidth(id, pane.DockedWidth ?? pane.FloatingBounds.Width)
        });
        ResolveDockConflict(id);
        Activate(id);
        Changed?.Invoke();
    }

    public void SetDockingAllowed(string id, bool allowed)
    {
        CheckAvailable();
        Replace(Get(id) with { AllowDocking = allowed, Dock = allowed ? Get(id).Dock : PaneDock.Floating });
        Changed?.Invoke();
    }

    private void ResolveDockConflict(string id)
    {
        var requested = Get(id);
        if (requested.Dock == PaneDock.Floating) return;
        for (var index = 0; index < _panes.Count; index++)
            if (_panes[index].InstanceId != id && _panes[index].Dock == requested.Dock &&
                _panes[index].Visibility == PaneVisibility.Visible)
                _panes[index] = _panes[index] with { Dock = PaneDock.Floating };
    }

    private void SelectAvailable()
    {
        if (_panes.Any(pane => pane.InstanceId == ActivePaneId && pane.Visibility == PaneVisibility.Visible)) return;
        ActivePaneId = _panes.LastOrDefault(pane => pane.Visibility == PaneVisibility.Visible)?.InstanceId;
    }

    private void BringForward(string id)
    {
        var pane = Get(id);
        _panes.Remove(pane);
        _panes.Add(pane);
    }

    private void Replace(PanePlacement pane) =>
        _panes[_panes.FindIndex(item => item.InstanceId == pane.InstanceId)] = pane;

    public WorkspaceLayout Capture()
    {
        CheckAvailable();
        // An immediate close before first arrange must not overwrite a loaded layout with startup defaults.
        if (_restored is not null) return _restored;
        return new(Width, Height, ActivePaneId,
        [
            .. _panes.Select(pane =>
                pane with { FloatingBounds = Clamp(pane.FloatingBounds, Definition(pane.InstanceId)) })
        ]);
    }

    private void CheckAvailable() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    public void Dispose()
    {
        IsDisposed = true;
        ActivePaneId = null;
        Changed = null;
    }
}
