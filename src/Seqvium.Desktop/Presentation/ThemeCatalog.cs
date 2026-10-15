// SPDX-License-Identifier: Apache-2.0
using System.Collections.Immutable;

namespace Seqvium.Desktop.Presentation;

internal sealed record ThemeChoice(HostTheme Theme, string LabelKey, string Icon);

/// <summary>Both settings and quick selection enumerate this catalog; neither assumes a theme cycle.</summary>
internal static class ThemeCatalog
{
    internal static ImmutableList<ThemeChoice> All { get; } =
    [new(HostTheme.Dark, "Preference.Dark", "Moon"), new(HostTheme.Light, "Preference.Light", "Sun")];
}
