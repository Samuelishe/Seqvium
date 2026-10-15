// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

/// <summary>Read-only exact pitch/duration/velocity projection, never editable step state.</summary>
internal sealed class PartNoteThumbnail(ShellSession shell, Pattern pattern, MusicalPart part, int color) : Control
{
    protected override void OnInitialized()
    {
        base.OnInitialized();
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        IBrush Brush(string role) => HostVisual.Brush(this, shell.Preferences.Theme, role);
        context.DrawRectangle(Brush("Surface.Workspace"), null, new Rect(Bounds.Size), 3, 3);
        for (int i = 1; i < 16; i++)
        {
            double x = i * Bounds.Width / 16;
            context.DrawLine(new Pen(Brush("Graph.Grid")), new(x, 0), new(x, Bounds.Height));
        }

        if (part.Notes.IsEmpty) return;
        decimal low = part.Notes.Min(n => n.Pitch), high = part.Notes.Max(n => n.Pitch);
        foreach (var note in part.Notes)
        {
            double x = note.Position.Ticks / (double)pattern.Length.Ticks * Bounds.Width;
            double width = Math.Max(2, note.Duration.Ticks / (double)pattern.Length.Ticks * Bounds.Width);
            double y = 3 + (double)((high - note.Pitch) / Math.Max(12, high - low)) * Math.Max(1, Bounds.Height - 10);
            using var opacity = context.PushOpacity(0.4 + (double)note.Intensity * 0.6);
            context.DrawRectangle(Brush($"Rack.Channel{color % 8}"), null, new Rect(x, y, width, 4), 1, 1);
        }
    }
}
