// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Seqvium.VisualCheck;

internal static class FieldAlignment
{
    /// <summary>Measure the actual editable text visual, not just the containing control's Height.</summary>
    public static object Verify(Control field, TextBlock label, Control root, string name)
    {
        var presenter = field.GetVisualDescendants().OfType<TextPresenter>().Single();
        double Center(Control control) => (control.TranslatePoint(default, root) ?? throw new InvalidOperationException("Detached field.")).Y + control.Bounds.Height / 2;
        double difference = Math.Abs(Center(presenter) - Center(label));
        Program.Require(difference <= 1, $"{name}: rendered text and label are misaligned by {difference} DIP; presenter={presenter.Bounds}, label={label.Bounds}.");
        var textBox = field as TextBox ?? field.GetVisualDescendants().OfType<TextBox>().Single();
        Program.Require(textBox.VerticalContentAlignment == VerticalAlignment.Center, name + ": template did not inherit centered text.");
        Program.Require(presenter.Bounds.Height <= field.Bounds.Height && field.Bounds.Height >= 24, name + ": rendered field clips its text/hit target.");
        return new { Name = name, DifferenceDip = difference, Field = field.Bounds, RenderedText = presenter.Bounds,
            Label = label.Bounds, textBox.VerticalContentAlignment };
    }
}
