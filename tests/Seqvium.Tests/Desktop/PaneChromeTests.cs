// SPDX-License-Identifier: Apache-2.0

using Avalonia.Controls;
using Seqvium.Desktop.Workspace;
using Xunit;

namespace Seqvium.Tests;

public sealed class PaneChromeTests
{
    [Theory]
    [InlineData("×")]
    [InlineData("−")]
    [InlineData("⋯")]
    public void ChromeButtonAndItsNestedVisualsAreExcludedBeforeAnyMoveOrResizeCapture(string glyph)
    {
        var caption = new TextBlock { Text = glyph };
        var presenter = new Border { Child = caption };
        var button = new VisualButton(presenter);
        // Exercise the same source/ancestor guard used before Begin acquires the pointer or creates a gesture.
        Assert.True(WorkspacePane.IsActionSource(button));
        Assert.True(WorkspacePane.IsActionSource(presenter));
        Assert.True(WorkspacePane.IsActionSource(caption));
        Assert.False(WorkspacePane.IsActionSource(new TextBlock { Text = "Project Inspector" }));
    }

    private sealed class VisualButton : Button
    {
        public VisualButton(Control child) => VisualChildren.Add(child);
    }
}
