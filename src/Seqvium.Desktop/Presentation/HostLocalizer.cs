// SPDX-License-Identifier: Apache-2.0

using System.Collections.Frozen;

namespace Seqvium.Desktop.Presentation;

/// <summary>Stable shell keys; individual unusable entries fall back to English, then an essential host label.</summary>
public sealed class HostLocalizer
{
    private readonly IReadOnlyDictionary<string, string> _english;
    private readonly IReadOnlyDictionary<string, string> _russian;

    public HostLocalizer() : this(English, Russian)
    {
    }

    public HostLocalizer(IReadOnlyDictionary<string, string> english, IReadOnlyDictionary<string, string> russian)
    {
        _english = english.ToFrozenDictionary(StringComparer.Ordinal);
        _russian = russian.ToFrozenDictionary(StringComparer.Ordinal);
    }

    public string Get(string key, HostLanguage language)
    {
        if (language == HostLanguage.Russian && _russian.TryGetValue(key, out var requested) && Usable(requested))
            return requested;
        if (_english.TryGetValue(key, out var fallback) && Usable(fallback)) return fallback;
        // Essential actions stay understandable even if both supplied catalogs are damaged.
        return key switch
        {
            "Window.Minimize" => "Minimize", "Window.Maximize" => "Maximize", "Window.Restore" => "Restore",
            "Window.Close" => "Close", "Project.Unnamed" => "Untitled project",
            "Preference.Language" => "Language", "Preference.Theme" => "Theme", "Host.About" => "About Seqvium",
            _ => language == HostLanguage.Russian ? "Текст недоступен" : "Text unavailable"
        };
    }

    private static bool Usable(string? value) => !string.IsNullOrWhiteSpace(value) &&
                                                 value.Length <= 512 && !value.Any(char.IsControl);

    public static IReadOnlyDictionary<string, string> English { get; } = new Dictionary<string, string>
    {
        ["Window.Minimize"] = "Minimize", ["Window.Maximize"] = "Maximize", ["Window.Restore"] = "Restore",
        ["Window.Close"] = "Close", ["Window.Active"] = "Active window", ["Window.Inactive"] = "Inactive window",
        ["Project.Unnamed"] = "Untitled project",
        ["Project.Pristine"] = "New · not saved", ["Project.Modified"] = "Unsaved changes", ["Project.Saved"] = "Saved",
        ["Project.Tempo"] = "Tempo", ["Project.Meter"] = "Meter", ["Project.Bpm"] = "BPM",
        ["Preference.Language"] = "Language", ["Preference.Theme"] = "Theme",
        ["Preference.Dark"] = "Dark", ["Preference.Light"] = "Light",
        ["Preference.LoadFallback"] =
            "Preferences could not be fully read. Safe defaults are in use; existing files were preserved.",
        ["Preference.SaveFailed"] = "Preference was applied for this session but could not be saved.",
        ["Audio.Idle"] = "Audio inactive", ["Host.About"] = "About Seqvium",
        ["Workspace.Panes"] = "Panes",
        ["Workspace.KeyboardHint"] = "F6 / Ctrl+Tab: switch · Esc: leave · Ctrl+W: close",
        ["Pane.Inspector"] = "Project Inspector", ["Pane.Appearance"] = "Appearance",
        ["Pane.Actions"] = "Pane actions", ["Pane.Activate"] = "Activate", ["Pane.Restore"] = "Restore pane",
        ["Pane.Collapse"] = "Collapse pane", ["Pane.Close"] = "Close pane",
        ["Pane.DockLeft"] = "Dock left", ["Pane.DockRight"] = "Dock right", ["Pane.Float"] = "Float / undock",
        ["Pane.AllowDocking"] = "Allow docking",
        ["Pane.MoveHint"] = "Actions focused: Ctrl+Arrows move · +Shift resize",
        ["Inspector.Project"] = "Project", ["Inspector.Status"] = "Status", ["Inspector.Patterns"] = "Patterns",
        ["Inspector.Sounds"] = "Sounds", ["Inspector.Resources"] = "Resources",
        ["Workspace.LoadFallback"] =
            "Layout read incompletely; safe entries restored. Original file preserved; layout saving disabled this session.",
        ["Workspace.SaveFailed"] = "Workspace layout could not be saved; this session remains usable.",
        ["Host.Scope"] =
            "Internal Project Inspector and Appearance panes are available. Music editors, Browser, Graph, Sample Lab and Project Open / Save UI are deferred."
    }.ToFrozenDictionary(StringComparer.Ordinal);

    public static IReadOnlyDictionary<string, string> Russian { get; } = new Dictionary<string, string>
    {
        ["Window.Minimize"] = "Свернуть", ["Window.Maximize"] = "Развернуть", ["Window.Restore"] = "Восстановить",
        ["Window.Close"] = "Закрыть", ["Window.Active"] = "Активное окно", ["Window.Inactive"] = "Неактивное окно",
        ["Project.Unnamed"] = "Проект без названия",
        ["Project.Pristine"] = "Новый · не сохранён", ["Project.Modified"] = "Есть несохранённые изменения",
        ["Project.Saved"] = "Сохранён",
        ["Project.Tempo"] = "Темп", ["Project.Meter"] = "Размер", ["Project.Bpm"] = "BPM",
        ["Preference.Language"] = "Язык", ["Preference.Theme"] = "Тема",
        ["Preference.Dark"] = "Тёмная", ["Preference.Light"] = "Светлая",
        ["Preference.LoadFallback"] =
            "Настройки прочитаны не полностью. Используются безопасные значения; исходные файлы сохранены.",
        ["Preference.SaveFailed"] = "Настройка применена для этого сеанса, но сохранить её не удалось.",
        ["Audio.Idle"] = "Аудио не активно",
        ["Host.About"] = "О Seqvium",
        ["Workspace.Panes"] = "Панели",
        ["Workspace.KeyboardHint"] = "F6 / Ctrl+Tab: панели · Esc: выйти · Ctrl+W: закрыть",
        ["Pane.Inspector"] = "Инспектор проекта", ["Pane.Appearance"] = "Оформление",
        ["Pane.Actions"] = "Действия панели", ["Pane.Activate"] = "Активировать",
        ["Pane.Restore"] = "Восстановить панель",
        ["Pane.Collapse"] = "Свернуть панель", ["Pane.Close"] = "Закрыть панель",
        ["Pane.DockLeft"] = "Закрепить слева", ["Pane.DockRight"] = "Закрепить справа", ["Pane.Float"] = "Открепить",
        ["Pane.AllowDocking"] = "Разрешить закрепление",
        ["Pane.MoveHint"] = "Фокус на действиях: Ctrl+стрелки — сдвиг · +Shift — размер",
        ["Inspector.Project"] = "Проект", ["Inspector.Status"] = "Состояние", ["Inspector.Patterns"] = "Паттерны",
        ["Inspector.Sounds"] = "Звуки", ["Inspector.Resources"] = "Ресурсы",
        ["Workspace.LoadFallback"] =
            "Макет прочитан не полностью; допустимые панели восстановлены. Исходный файл сохранён; запись макета отключена в этом сеансе.",
        ["Workspace.SaveFailed"] = "Сохранить макет не удалось; панели доступны в текущем сеансе.",
        ["Host.Scope"] =
            "Доступны внутренние панели инспектора проекта и оформления. Музыкальные редакторы, Browser, Graph, Sample Lab и открытие / сохранение через UI отложены."
    }.ToFrozenDictionary(StringComparer.Ordinal);
}
