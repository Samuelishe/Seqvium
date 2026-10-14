// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Seqvium.Desktop.Presentation;

/// <summary>Original, low-contrast machined grain. Never placed over a music grid or signal plot.</summary>
internal sealed class HostChassisTexture : Control
{
    internal HostChassisTexture()
    {
        IsHitTestVisible = false;
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        var highlight = HostVisual.Brush(this, HostTheme.Dark, "Material.Highlight");
        var shade = HostVisual.Brush(this, HostTheme.Dark, "Material.Shade");
        using (context.PushOpacity(.045))
        {
            var lightPen = new Pen(highlight, .5);
            var darkPen = new Pen(shade, .5);
            // Bounded independent of workspace height; no bitmap or borrowed texture resource.
            for (int line = 0; line < Math.Min(512, Bounds.Height / 4); line++)
            {
                double y = line * 4 + 1.5;
                double inset = line % 5 * 7;
                context.DrawLine(lightPen, new(inset, y), new(Math.Max(inset, Bounds.Width - inset), y));
                context.DrawLine(darkPen, new(0, y + 1), new(Bounds.Width, y + 1));
            }
        }
    }
}
