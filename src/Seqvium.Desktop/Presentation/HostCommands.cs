// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;
using Avalonia.Input;

namespace Seqvium.Desktop.Presentation;

public sealed record HostCommand(string Id, string LabelKey, string GroupKey, string DefaultBinding);

/// <summary>Application commands only. Editor gestures and native/text navigation keep their owners.</summary>
public static class HostCommands
{
    public static ImmutableArray<HostCommand> All { get; } =
    [
        new("New", "Workflow.New", "Settings.Project", "Ctrl+N"),
        new("Open", "Workflow.Open", "Settings.Project", "Ctrl+O"),
        new("Save", "Workflow.Save", "Settings.Project", "Ctrl+S"),
        new("SaveAs", "Workflow.SaveAs", "Settings.Project", "Ctrl+Shift+S"),
        new("Import", "Workflow.Import", "Settings.Project", "Ctrl+I"),
        new("Undo", "Workflow.Undo", "Settings.Editing", "Ctrl+Z"),
        new("Redo", "Workflow.Redo", "Settings.Editing", "Ctrl+Y"),
        new("Play", "Workflow.Play", "Settings.Transport", "F5"),
        new("Stop", "Workflow.Stop", "Settings.Transport", "Shift+F5"),
        new("Panic", "Workflow.Panic", "Settings.Transport", "Ctrl+Escape"),
        new("Browser", "Pane.Browser", "Settings.Workspace", "Ctrl+B"),
        new("Graph", "Pane.Graph", "Settings.Workspace", "Ctrl+G"),
        new("Settings", "Settings.Title", "Settings.Workspace", "Ctrl+OemComma")
    ];

    public static bool IsAssignable(Key key, KeyModifiers modifiers)
    {
        if ((modifiers & ~(KeyModifiers.Control | KeyModifiers.Shift)) != 0) return false;
        if (key is >= Key.F1 and <= Key.F12)
            return key is not (Key.F4 or Key.F6 or Key.F10) && modifiers is KeyModifiers.None or KeyModifiers.Shift;
        if (modifiers is not (KeyModifiers.Control or (KeyModifiers.Control | KeyModifiers.Shift))) return false;
        // These chords are owned by text editing, editor selection/clipboard/duplicate/search,
        // Browser refresh, and pane close. Reserving them also avoids focus-dependent conflicts.
        return key is >= Key.A and <= Key.Z && key is not (Key.A or Key.C or Key.V or Key.X or Key.D or Key.F or Key.R or Key.W)
               || key == Key.OemComma || key == Key.Escape && modifiers == KeyModifiers.Control;
    }

    public static string? Canonical(string? binding)
    {
        if (binding is null) return null;
        var gesture = KeyGesture.Parse(binding);
        if (!IsAssignable(gesture.Key, gesture.KeyModifiers)) throw new FormatException("Reserved gesture.");
        return new KeyGesture(gesture.Key, gesture.KeyModifiers).ToString();
    }

    public static string Display(string? binding) => binding?.Replace("OemComma", ",", StringComparison.Ordinal) ?? "";

    public static string? Resolve(ControlPreferences preferences, Key key, KeyModifiers modifiers, bool textInput)
    {
        // Native/local text operations must never dispatch musical or document commands.
        if (textInput && key is Key.Z or Key.Y or Key.A or Key.C or Key.V or Key.X) return null;
        string? action = All.FirstOrDefault(command => preferences.Binding(command.Id) is { } binding &&
            KeyGesture.Parse(binding) is { } gesture && gesture.Key == key && gesture.KeyModifiers == modifiers)?.Id;
        return textInput && action is "Undo" or "Redo" ? null : action;
    }
}

public sealed record ControlPreferences(bool AutoPreview = false,
    ImmutableDictionary<string, string?>? Overrides = null)
{
    public string? Binding(string id) => Overrides is not null && Overrides.TryGetValue(id, out var value)
        ? value : HostCommands.All.Single(command => command.Id == id).DefaultBinding;

    public bool IsCustomized(string id) => HostCommands.Canonical(Binding(id)) !=
        HostCommands.Canonical(HostCommands.All.Single(command => command.Id == id).DefaultBinding);

    public string? Conflict(string id, string? binding) => binding is null ? null : HostCommands.All
        .FirstOrDefault(command => command.Id != id && HostCommands.Canonical(Binding(command.Id)) == HostCommands.Canonical(binding))?.Id;

    public ControlPreferences Assign(string id, string? binding, bool replace = false)
    {
        _ = HostCommands.All.Single(command => command.Id == id);
        binding = HostCommands.Canonical(binding);
        var conflict = Conflict(id, binding);
        if (conflict is not null && !replace) throw new InvalidOperationException("Shortcut conflict.");
        var values = Overrides ?? ImmutableDictionary<string, string?>.Empty;
        if (conflict is not null) values = values.SetItem(conflict, null);
        return this with { Overrides = values.SetItem(id, binding) };
    }
}
