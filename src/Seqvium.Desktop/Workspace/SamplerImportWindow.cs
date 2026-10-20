// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Input;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

/// <summary>One complete mapping draft; files/resources enter the document only after acceptance.</summary>
internal sealed class SamplerImportWindow : Window
{
    internal ImmutableArray<SamplerZone> ReviewedZones { get; private set; }

    internal SamplerImportWindow(ShellSession shell, string[] paths, decimal?[] roots,
        SoundDefinition? sound = null, string[]? existingNames = null)
    {
        Title = shell["Sampler.AddSources"];
        Width = 460;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        var existing = sound is null ? ImmutableArray<SamplerZone>.Empty : SamplerKeymap.Zones(sound);
        var zones = existing.Concat(paths.Select((_, i) => new SamplerZone(Id<SamplerZone>.New(),
            Id<ResourceDescriptor>.New(), roots[i] ?? 60, 0, 127, ImmutableDictionary<string, decimal>.Empty))).ToArray();
        var confirmed = existing.Select(z => (decimal?)z.RootPitch).Concat(roots).ToArray();
        var body = new StackPanel();
        body.Bind(MarginProperty, this.GetResourceObservable("Padding.Dialog"));
        body.Bind(StackPanel.SpacingProperty, this.GetResourceObservable("Space.Gap"));
        var sources = new ComboBox
        {
            ItemsSource = (existingNames ?? existing.Select(z => PianoNotes.Name(z.RootPitch)).ToArray())
                .Concat(paths.Select(Path.GetFileNameWithoutExtension)).ToArray(), MaxDropDownHeight = 280
        };
        AutomationProperties.SetAutomationId(sources, "Sampler.ImportSources");
        var fields = new Dictionary<string, TextBox>();
        foreach (string key in new[] { "Root", "Low", "High", "LowVelocity", "HighVelocity" })
        {
            var field = new TextBox { Classes = { "numeric" } };
            var row = new Grid { ColumnDefinitions = new("160,*"), Classes = { "parameterRow" } };
            row.Children.Add(new TextBlock { Text = shell["Sampler." + key], VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center });
            row.Children.Add(field);
            Grid.SetColumn(field, 1);
            body.Children.Add(row);
            fields[key] = field;
            AutomationProperties.SetAutomationId(field, "Sampler.Import" + key);
            AutomationProperties.SetName(field, shell["Sampler." + key]);
        }
        var root = fields["Root"];
        root.PlaceholderText = shell["Sampler.RootRequired"];
        var warning = new TextBlock { Classes = { "caption", "warning" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var add = new Button { Content = shell["Sampler.AddSources"] };
        var cancel = new Button { Content = shell["Workflow.Cancel"] };
        int selected = -1;
        bool refreshing = false;
        bool[] valid = Enumerable.Repeat(true, zones.Length).ToArray();

        void Refresh()
        {
            bool missing = confirmed.Any(r => r is null);
            bool accepted = !missing && valid.All(v => v);
            if (accepted)
            {
                var basis = sound ?? new SoundDefinition(Id<SoundDefinition>.New(), "Draft", PcmSampler.Algorithm,
                    ImmutableDictionary<string, decimal>.Empty.SetItem("rootPitch", 60).SetItem("releaseMilliseconds", 5), [], null);
                try
                {
                    SamplerKeymap.Validate(basis with
                    {
                        SamplerZones = [.. zones], ResourceIds = [.. zones.Select(z => z.ResourceId).Distinct()],
                        Parameters = basis.Parameters.RemoveRange(SamplerPlayback.Keys)
                    });
                }
                catch (ArgumentException) { accepted = false; }
            }
            add.IsEnabled = accepted;
            warning.IsVisible = !accepted;
            warning.Text = shell[missing ? "Sampler.RootRequired" : "Sampler.MapInvalid"];
        }

        void Fill()
        {
            refreshing = true;
            var z = zones[selected];
            fields["Root"].Text = confirmed[selected]?.ToString(CultureInfo.InvariantCulture) ?? "";
            fields["Low"].Text = PianoNotes.Name(z.LowKey);
            fields["High"].Text = PianoNotes.Name(z.HighKey);
            fields["LowVelocity"].Text = (z.LowVelocity * 100).ToString("0.############################", CultureInfo.InvariantCulture);
            fields["HighVelocity"].Text = (z.HighVelocity * 100).ToString("0.############################", CultureInfo.InvariantCulture);
            valid[selected] = true;
            refreshing = false;
            Refresh();
        }

        void Read()
        {
            if (refreshing || selected < 0) return;
            confirmed[selected] = SamplerPitchText.TryRead(root.Text, out var pitch) ? pitch : null;
            if (confirmed[selected] is { } rootPitch)
                zones[selected] = zones[selected] with { RootPitch = rootPitch };
            if (selected >= existing.Length) roots[selected - existing.Length] = confirmed[selected];
            valid[selected] = confirmed[selected] is not null &&
                SamplerPitchText.TryRead(fields["Low"].Text, out var low) && low == decimal.Truncate(low) &&
                SamplerPitchText.TryRead(fields["High"].Text, out var high) && high == decimal.Truncate(high) &&
                SamplerVelocityText.TryRead(fields["LowVelocity"].Text, out _) &&
                SamplerVelocityText.TryRead(fields["HighVelocity"].Text, out _);
            if (valid[selected])
            {
                SamplerPitchText.TryRead(fields["Low"].Text, out low);
                SamplerPitchText.TryRead(fields["High"].Text, out high);
                SamplerVelocityText.TryRead(fields["LowVelocity"].Text, out var lo);
                SamplerVelocityText.TryRead(fields["HighVelocity"].Text, out var hi);
                zones[selected] = zones[selected] with
                {
                    RootPitch = confirmed[selected]!.Value, LowKey = (int)low, HighKey = (int)high,
                    LowVelocity = lo, HighVelocity = hi
                };
            }
            Refresh();
        }

        sources.SelectionChanged += (_, _) => { selected = sources.SelectedIndex; if (selected >= 0) Fill(); };
        foreach (var field in fields.Values)
            field.PropertyChanged += (_, e) => { if (e.Property == TextBox.TextProperty) Read(); };
        var distribution = new WrapPanel();
        foreach (bool layers in new[] { false, true })
        {
            var button = new Button { Content = shell[layers ? "Sampler.SplitLayers" : "Sampler.Distribute"] };
            AutomationProperties.SetAutomationId(button, layers ? "Sampler.ImportLayers" : "Sampler.ImportDistribute");
            button.Click += (_, _) =>
            {
                if (confirmed.Any(r => r is null)) { Refresh(); return; }
                try
                {
                    var mapped = layers ? SamplerKeymap.Layer(zones) : SamplerKeymap.Distribute(zones);
                    zones = zones.Select(z => mapped.Single(m => m.Id == z.Id)).ToArray();
                    Array.Fill(valid, true);
                    Fill();
                }
                catch (ArgumentException) { warning.Text = shell["Sampler.DistributeUnavailable"]; warning.IsVisible = true; }
            };
            distribution.Children.Add(button);
        }
        add.Click += (_, _) =>
        {
            if (!add.IsEnabled) return;
            ReviewedZones = [.. zones];
            Close(true);
        };
        cancel.Click += (_, _) => Close(false);
        KeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; Close(false); } };
        AutomationProperties.SetAutomationId(add, "Sampler.ImportAdd");
        AutomationProperties.SetAutomationId(cancel, "Sampler.ImportCancel");
        body.Children.Insert(0, sources);
        body.Children.Add(distribution);
        body.Children.Add(warning);
        var actions = new WrapPanel();
        actions.Children.Add(add);
        actions.Children.Add(cancel);
        body.Children.Add(actions);
        Content = body;
        // Retain the established pitch-only automatic distribution when it is unambiguous.
        if (confirmed.All(r => r is not null))
        {
            try
            {
                var mapped = SamplerKeymap.Distribute(zones);
                zones = zones.Select(z => mapped.Single(m => m.Id == z.Id)).ToArray();
            }
            catch (ArgumentException) { }
        }
        sources.SelectedIndex = 0;
    }
}
