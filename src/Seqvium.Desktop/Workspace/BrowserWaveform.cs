// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workspace;

internal sealed class BrowserWaveform : Control
{
    private readonly ShellSession _shell;
    internal BrowserWaveform(ShellSession shell)
    {
        _shell = shell;
        ((IResourceHost)this).ResourcesChanged += (_, _) => InvalidateVisual();
    }
    internal AudioWaveform? Waveform { get; set; }
    public override void Render(DrawingContext context)
    {
        base.Render(context);
        IBrush Brush(string key) => HostVisual.Brush(this, _shell.Preferences.Theme, key);
        var rect = new Rect(Bounds.Size);
        context.DrawRectangle(Brush("Surface.Workspace"), new Pen(Brush("Border.Default")), rect);
        if (Waveform is not { } wave || Bounds.Width <= 0) return;
        double height = Bounds.Height / wave.Channels;
        var pen = new Pen(Brush("Accent.Action"), 1);
        for (int channel = 0; channel < wave.Channels; channel++)
        {
            double center = height * (channel + .5);
            context.DrawLine(new Pen(Brush("Border.Default")), new(0, center), new(Bounds.Width, center));
            for (int x = 0; x < (int)Bounds.Width; x++)
            {
                var (min, max) = wave.Range((long)(x / Bounds.Width * wave.Frames),
                    (long)((x + 1) / Bounds.Width * wave.Frames), channel);
                double scale = (height / 2 - 5) / Math.Max(.001f, wave.Peak);
                context.DrawLine(pen, new(x, center - max * scale), new(x, center - min * scale));
            }
        }
    }
}
