// SPDX-License-Identifier: Apache-2.0

using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>Compact batch review: unconfirmed roots remain empty and cannot enter a key map.</summary>
internal sealed class SamplerImportWindow : Window
{
    internal SamplerImportWindow(ShellSession shell, string[] paths, decimal?[] roots)
    {
        Title = shell["Sampler.AddSources"];
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var body = new StackPanel();
        body.Bind(MarginProperty, this.GetResourceObservable("Padding.Dialog"));
        body.Bind(StackPanel.SpacingProperty, this.GetResourceObservable("Space.Gap"));
        var sources = new ComboBox { ItemsSource = paths.Select(Path.GetFileNameWithoutExtension).ToArray() };
        var root = new TextBox { PlaceholderText = shell["Sampler.RootRequired"], Classes = { "numeric" } };
        var warning = new TextBlock { Text = shell["Sampler.RootRequired"], Classes = { "caption", "warning" } };
        var next = new Button { Content = shell["Sampler.ConfirmRoot"] };
        var add = new Button { Content = shell["Sampler.AddSources"] };
        var cancel = new Button { Content = shell["Workflow.Cancel"] };
        int selected = -1;

        void Refresh()
        {
            int missing = roots.Count(r => r is null);
            add.IsEnabled = missing == 0 && roots.Distinct().Count() == roots.Length;
            warning.IsVisible = !add.IsEnabled;
            warning.Text = shell[missing > 0 ? "Sampler.RootRequired" : "Sampler.DuplicateRoots"];
        }

        sources.SelectionChanged += (_, _) =>
        {
            selected = sources.SelectedIndex;
            root.Text = selected >= 0 && roots[selected] is { } value
                ? value.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : "";
            Refresh();
        };
        root.PropertyChanged += (_, e) =>
        {
            if (e.Property != TextBox.TextProperty || selected < 0) return;
            roots[selected] = SamplerPitchText.TryRead(root.Text, out var value) ? value : null;
            Refresh();
        };
        next.Click += (_, _) =>
        {
            if (selected < 0 || roots[selected] is null) return;
            int missing = Array.FindIndex(roots, r => r is null);
            sources.SelectedIndex = missing >= 0 ? missing : Math.Min(selected + 1, roots.Length - 1);
        };
        add.Click += (_, _) =>
        {
            if (add.IsEnabled) Close(true);
        };
        cancel.Click += (_, _) => Close(false);
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape)
            {
                e.Handled = true;
                Close(false);
            }
        };
        AutomationProperties.SetAutomationId(root, "Sampler.ImportRoot");
        AutomationProperties.SetAutomationId(add, "Sampler.ImportAdd");
        AutomationProperties.SetAutomationId(cancel, "Sampler.ImportCancel");
        AutomationProperties.SetName(root, shell["Sampler.Root"]);
        body.Children.Add(sources);
        body.Children.Add(new TextBlock { Text = shell["Sampler.Root"] });
        body.Children.Add(root);
        body.Children.Add(warning);
        var actions = new WrapPanel();
        actions.Children.Add(next);
        actions.Children.Add(add);
        actions.Children.Add(cancel);
        body.Children.Add(actions);
        Content = body;
        sources.SelectedIndex = 0;
    }
}