// SPDX-License-Identifier: Apache-2.0
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Workflow;

/// <summary>Content-sized first-party modal. No system titlebar or duplicate internal header.
/// Native close/Escape and initial Enter target Cancel; only an explicit choice permits replacement.</summary>
internal sealed class UnsavedProjectDialog : Window
{
    internal UnsavedProjectDialog(ShellSession shell)
    {
        Title = shell["Workflow.UnsavedTitle"];
        WindowDecorations = WindowDecorations.None;
        CanResize = false; ShowInTaskbar = false; Width = 410; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        this.Bind(BackgroundProperty, this.GetResourceObservable("Surface.Raised"));
        var frame = new Border { BorderThickness = new(1), Padding = new(16) };
        frame.Bind(Border.BorderBrushProperty, frame.GetResourceObservable("Border.Default"));
        var content = new StackPanel { Spacing = 16 };
        KeyboardNavigation.SetTabNavigation(content, KeyboardNavigationMode.Cycle);
        content.Children.Add(new TextBlock { Text = shell["Workflow.UnsavedPrompt"],
            TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        var buttons = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 8,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right };
        Button? cancel = null;
        foreach (var choice in new[] { UnsavedDecision.Save, UnsavedDecision.Discard, UnsavedDecision.Cancel })
        {
            var button = new Button { Content = shell["Workflow." + choice], Classes = { "shell" }, MinHeight = 28, Height = 28 };
            button.Classes.Add(choice == UnsavedDecision.Save ? "primary" : choice == UnsavedDecision.Discard ? "critical" : "secondary");
            AutomationProperties.SetAutomationId(button, "Unsaved." + choice);
            button.Click += (_, _) => Close(choice);
            buttons.Children.Add(button);
            if (choice == UnsavedDecision.Cancel) cancel = button;
        }
        content.Children.Add(buttons); frame.Child = content; Content = frame;
        Opened += (_, _) => cancel?.Focus();
        KeyDown += (_, args) =>
        {
            if (args.Key != Key.Escape) return;
            args.Handled = true; Close(UnsavedDecision.Cancel);
        };
    }
}
