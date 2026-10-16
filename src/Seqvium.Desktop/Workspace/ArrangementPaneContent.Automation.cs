// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class ArrangementPaneContent
{
    private sealed record AutomationChoice(AutomationTarget Target, string Label)
    {
        public override string ToString() => Label;
    }

    private Flyout? _automationFlyout;
    internal Control? AutomationPropertiesContent { get; private set; }

    private void OpenAutomation(bool create = false)
    {
        _canvas.Cancel();
        var observation = _workflow.State;
        var state = observation.Snapshot.State;
        var basis = ArrangementEditBasis.From(observation);
        var body = new StackPanel { MinWidth = 260, MaxWidth = 320, Classes = { "spaced" } };
        AutomationPropertiesContent = body;
        _automationFlyout?.Hide();
        var flyout = new Flyout { Content = body };
        _automationFlyout = flyout;
        var lane = create ? null : state.AutomationLanes.SingleOrDefault(l => l.Id == _canvas.SelectedAutomation);
        var error = new TextBlock { Classes = { "error" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        AutomationProperties.SetAutomationId(error, "Automation.Error");
        if (lane is null)
        {
            var choices = state.Sounds.Where(SynthVoice.Supports).Select(s => new AutomationChoice(
                    new(AutomationParameter.SynthCutoff, s.Id), s.Name + " · " + _shell["Automation.Cutoff"]))
                .Concat((state.Mixer?.Channels ?? []).Select(c => new AutomationChoice(
                    new(AutomationParameter.MixerVolume, ChannelId: c.Id),
                    c.Name + " · " + _shell["Automation.Volume"])))
                .Where(c => !state.AutomationLanes.Any(l => l.Target == c.Target)).ToArray();
            var picker = new ComboBox
            {
                ItemsSource = choices, SelectedIndex = choices.Length > 0 ? 0 : -1,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            AutomationProperties.SetAutomationId(picker, "Automation.Target");
            AutomationProperties.SetName(picker, _shell["Automation.Target"]);
            body.Children.Add(picker);
            if (choices.Length == 0) error.Text = _shell["Automation.NoTarget"];
            var add = new Button
                { Content = _shell["Automation.Add"], IsEnabled = choices.Length > 0, Classes = { "primary" } };
            AutomationProperties.SetAutomationId(add, "Automation.Add");
            body.Children.Add(add);
            add.Click += async (_, _) =>
            {
                if (picker.SelectedItem is not AutomationChoice choice) return;
                PendingCommand = _workflow.AddAutomationAsync(basis, choice.Target);
                await PendingCommand;
                if (_workflow.State.StatusKey.EndsWith("Failed", StringComparison.Ordinal))
                {
                    error.Text = _shell["Automation.Invalid"];
                    return;
                }

                if (_workflow.State.Snapshot.State.AutomationLanes.SingleOrDefault(l => l.Target == choice.Target) is
                    { } created)
                {
                    _canvas.Adopt(_workflow.State);
                    _canvas.SelectAutomation(created.Id, created.Points[0].Id);
                    _scroll.Offset = new(_scroll.Offset.X,
                        Math.Max(0, created.Lane * HostVisual.Metric(_canvas, "Playlist.LaneHeight") - 50));
                }

                flyout.Hide();
                Refresh();
            };
        }
        else
        {
            string name = lane.Target.Parameter == AutomationParameter.SynthCutoff
                ? state.Sounds.SingleOrDefault(s => s.Id == lane.Target.SoundId)?.Name ?? lane.TargetName
                : state.Mixer?.Channels.SingleOrDefault(c => c.Id == lane.Target.ChannelId)?.Name ?? lane.TargetName;
            var header = new Grid { ColumnDefinitions = new("*,Auto") };
            var identity = new StackPanel { Classes = { "tight" } };
            identity.Children.Add(new TextBlock { Text = name, Classes = { "section" },
                TextTrimming = Avalonia.Media.TextTrimming.CharacterEllipsis, MaxWidth = 220 });
            ToolTip.SetTip(identity, name);
            identity.Children.Add(new TextBlock
            {
                Text =
                       _shell[
                           lane.Target.Parameter == AutomationParameter.SynthCutoff
                               ? "Automation.Cutoff"
                               : "Automation.Volume"],
                Classes = { "caption" }
            });
            header.Children.Add(identity);
            var enabled = new ToggleButton { IsChecked = lane.Enabled, Classes = { "command" }, VerticalAlignment = VerticalAlignment.Center };
            enabled.Content = HostIcons.Create("Power", enabled);
            enabled.Bind(WidthProperty, enabled.GetResourceObservable("Control.IconWidth"));
            ToolTip.SetTip(enabled, _shell["Automation.Enabled"]);
            AutomationProperties.SetName(enabled, _shell["Automation.Enabled"]);
            AutomationProperties.SetAutomationId(enabled, "Automation.Enabled");
            SetColumn(enabled, 1); header.Children.Add(enabled); body.Children.Add(header);
            if (!ParameterAutomation.IsResolved(state, lane.Target)) error.Text = _shell["Automation.Unavailable"];
            var point = lane.Points.SingleOrDefault(p => p.Id == _canvas.SelectedAutomationPoint);
            var fields = new Dictionary<string, TextBox>();
            var transition = new ComboBox
            {
                ItemsSource = new[] { _shell["Automation.Linear"], _shell["Automation.Smooth"] },
                SelectedIndex = (int)(point?.Transition ?? AutomationTransition.Linear),
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            if (point is not null)
            {
                long bar = state.Settings.Meter.TicksPerBar, beat = state.Settings.Meter.TicksPerBeat;
                AddField("Time", 0, text: $"{point.Position.Ticks / bar + 1}:{point.Position.Ticks % bar / beat + 1}:{point.Position.Ticks % beat}");
                ToolTip.SetTip(fields["Time"], _shell["Automation.TimeHint"]);
                AddField("Value", (decimal)ParameterAutomation.Physical(lane.Target.Parameter, (double)point.Value),
                    lane.Target.Parameter == AutomationParameter.SynthCutoff ? "Hz" : "dB");
                AutomationProperties.SetAutomationId(transition, "Automation.Transition");
                AutomationProperties.SetName(transition, _shell["Automation.Transition"]);
                body.Children.Add(transition);
            }

            var actions = new Grid { ColumnDefinitions = new("Auto,*,Auto,Auto") };
            var apply = new Button { Content = _shell["AudioClip.Apply"], Classes = { "primary" } };
            AutomationProperties.SetAutomationId(apply, "Automation.Apply");
            actions.Children.Add(apply);
            var cancel = new Button { Classes = { "command" } };
            cancel.Content = HostIcons.Create("Close", cancel);
            ToolTip.SetTip(cancel, _shell["Automation.Cancel"]); AutomationProperties.SetName(cancel, _shell["Automation.Cancel"]);
            cancel.Click += (_, _) => flyout.Hide(); SetColumn(cancel, 2); actions.Children.Add(cancel);
            var remove = new Button { Classes = { "command", "danger" } };
            remove.Content = HostIcons.Create("Delete", remove);
            ToolTip.SetTip(remove, _shell["Automation.DeleteLane"]); AutomationProperties.SetName(remove, _shell["Automation.DeleteLane"]);
            AutomationProperties.SetAutomationId(remove, "Automation.DeleteLane");
            SetColumn(remove, 3); actions.Children.Add(remove);
            body.Children.Add(actions);
            remove.Click += async (_, _) =>
            {
                PendingCommand = _workflow.DeleteAutomationAsync(basis, lane.Id);
                await PendingCommand;
                flyout.Hide();
            };
            apply.Click += async (_, _) =>
            {
                try
                {
                    decimal Read(string key) => decimal.Parse(fields[key].Text ?? "", NumberStyles.Float,
                        CultureInfo.InvariantCulture);

                    long Integer(string input)
                    {
                        decimal value = decimal.Parse(input, NumberStyles.None, CultureInfo.InvariantCulture);
                        if (value != decimal.Truncate(value)) throw new FormatException();
                        return checked((long)value);
                    }

                    var updated = lane with { Enabled = enabled.IsChecked == true };
                    if (point is not null)
                    {
                        var position = (fields["Time"].Text ?? "").Split(':');
                        if (position.Length != 3) throw new FormatException();
                        long bar = Integer(position[0]), beat = Integer(position[1]), tick = Integer(position[2]);
                        decimal physical = Read("Value");
                        if (bar is < 1 or > 1000001 || beat < 1 || beat > state.Settings.Meter.Numerator ||
                            tick < 0 || tick >= state.Settings.Meter.TicksPerBeat ||
                            (lane.Target.Parameter == AutomationParameter.SynthCutoff
                                ? physical is < 20 or > 18000
                                : physical is < -90 or > 12))
                            throw new FormatException();
                        long time = checked((bar - 1) * state.Settings.Meter.TicksPerBar +
                                            (beat - 1) * state.Settings.Meter.TicksPerBeat + tick);
                        if (lane.Points.Any(p => p.Id != point.Id && p.Position.Ticks == time))
                            throw new FormatException();
                        // Preserve untouched high-precision normalized values across physical display rounding.
                        decimal value = fields["Value"].Text ==
                                        ((decimal)ParameterAutomation.Physical(lane.Target.Parameter,
                                            (double)point.Value)).ToString("0.######", CultureInfo.InvariantCulture)
                            ? point.Value
                            : ParameterAutomation.Normalize(lane.Target.Parameter, (double)physical);
                        updated = updated with
                        {
                            Points =
                            [
                                .. lane.Points.Select(p => p.Id == point.Id
                                    ? p with
                                    {
                                        Position = new(time), Value = value,
                                        Transition = (AutomationTransition)transition.SelectedIndex
                                    }
                                    : p).OrderBy(p => p.Position.Ticks)
                            ]
                        };
                    }

                    PendingCommand = _workflow.EditAutomationAsync(basis, updated);
                    await PendingCommand;
                    if (_workflow.State.StatusKey.EndsWith("Failed", StringComparison.Ordinal))
                        error.Text = _shell["Automation.Invalid"];
                    else flyout.Hide();
                }
                catch (Exception failure) when (failure is FormatException or OverflowException)
                {
                    error.Text = _shell["Automation.Invalid"];
                }
            };

            void AddField(string key, decimal value, string unit = "", string? text = null)
            {
                var row = new Grid { ColumnDefinitions = new("*,132,30"), Classes = { "parameterRow" } };
                row.Children.Add(new TextBlock
                    { Text = _shell["Automation." + key], VerticalAlignment = VerticalAlignment.Center });
                var field = new TextBox
                    { Text = text ?? value.ToString("0.######", CultureInfo.InvariantCulture), Classes = { "numeric" } };
                fields.Add(key, field);
                AutomationProperties.SetAutomationId(field, "Automation." + key);
                AutomationProperties.SetName(field, _shell["Automation." + key]);
                Grid.SetColumn(field, 1);
                row.Children.Add(field);
                var units = new TextBlock
                    { Text = unit, VerticalAlignment = VerticalAlignment.Center, Margin = new(6, 0) };
                Grid.SetColumn(units, 2);
                row.Children.Add(units);
                body.Children.Add(row);
            }
        }

        error.IsVisible = !string.IsNullOrEmpty(error.Text);
        error.PropertyChanged += (_, e) => { if (e.Property == TextBlock.TextProperty) error.IsVisible = !string.IsNullOrEmpty(error.Text); };
        body.Children.Add(error);
        flyout.ShowAt(_buttons["Automation"]);
    }
}
