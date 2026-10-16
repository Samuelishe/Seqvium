// SPDX-License-Identifier: Apache-2.0

using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Seqvium.Core;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class ArrangementPaneContent
{
    private Flyout? _audioFlyout;
    internal Control? AudioPropertiesContent => _audioFlyout?.Content as Control;

    internal Task ImportAudioAsync(string[] paths) => _canvas.ImportAudioFilesAsync(paths, _canvas.PositionTicks,
        _workflow.State.Snapshot.State.AudioClips.SingleOrDefault(c => c.Id == _canvas.SelectedAudio)?.Lane ??
        Math.Min(31, _workflow.State.Snapshot.State.ArrangementClips.Select(c => c.Lane + 1).DefaultIfEmpty(0).Max()));

    private void OpenAudioProperties()
    {
        var state = _workflow.State;
        var clip = state.Snapshot.State.AudioClips.SingleOrDefault(c => c.Id == _canvas.SelectedAudio);
        if (clip is null) return;
        var sound = state.Snapshot.State.Sounds.Single(s => s.Id == clip.SoundId);
        int rate = AudioClipSource.Rate(sound);
        var basis = ArrangementEditBasis.From(state);
        var body = new StackPanel { MinWidth = 300, MaxWidth = 360, Classes = { "spaced" } };
        body.Children.Add(new TextBlock { Text = sound.Name, Classes = { "section" } });
        var duration = new TextBlock { Classes = { "caption" } };
        body.Children.Add(duration);
        var fields = new Dictionary<string, TextBox>();
        foreach (var (key, value, unit) in new (string, decimal, string)[]
                 {
                     ("Offset", (decimal)clip.OffsetFrames / rate, "s"),
                     ("Length", (decimal)clip.LengthFrames / rate, "s"),
                     ("Stretch", clip.StretchRatio * 100, "%"),
                     ("FadeIn", (decimal)clip.FadeInFrames / rate * 1000, "ms"),
                     ("FadeOut", (decimal)clip.FadeOutFrames / rate * 1000, "ms"),
                     ("Gain", clip.GainDb, "dB")
                 })
        {
            var row = new Grid { ColumnDefinitions = new("*,100,34"), Classes = { "parameterRow" } };
            row.Children.Add(new TextBlock
                { Text = _shell["AudioClip." + key], VerticalAlignment = VerticalAlignment.Center });
            var field = new TextBox
                { Text = value.ToString("0.#########", CultureInfo.InvariantCulture), Classes = { "numeric" } };
            AutomationProperties.SetAutomationId(field, "AudioClip." + key);
            AutomationProperties.SetName(field, _shell["AudioClip." + key]);
            fields.Add(key, field);
            if (key == "Stretch") ToolTip.SetTip(field, _shell["AudioClip.StretchHint"]);
            Grid.SetColumn(field, 1);
            row.Children.Add(field);
            var units = new TextBlock { Text = unit, VerticalAlignment = VerticalAlignment.Center, Margin = new(6, 0) };
            Grid.SetColumn(units, 2);
            row.Children.Add(units);
            body.Children.Add(row);
        }

        void UpdateDuration()
        {
            if (decimal.TryParse(fields["Stretch"].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var percent) &&
                decimal.TryParse(fields["Length"].Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var length) &&
                percent is >= 50 and <= 200 && length > 0 && length <= (decimal)AudioClipSource.Frames(sound) / rate)
                duration.Text = _shell["AudioClip.Playback"] + " · " +
                    (length * percent / 100).ToString("0.###", CultureInfo.InvariantCulture) + " s";
            else duration.Text = _shell["AudioClip.StretchRange"];
        }
        fields["Stretch"].TextChanged += (_, _) => UpdateDuration();
        fields["Length"].TextChanged += (_, _) => UpdateDuration();
        UpdateDuration();

        var routing = new ComboBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        var channels = state.Snapshot.State.Mixer?.Channels.ToArray() ?? [];
        routing.ItemsSource = new[] { _shell["Mixer.Master"] }.Concat(channels.Select(c => c.Name)).ToArray();
        routing.SelectedIndex = Array.FindIndex(channels, c => c.SoundIds.Contains(sound.Id)) + 1;
        AutomationProperties.SetAutomationId(routing, "AudioClip.Route");
        AutomationProperties.SetName(routing, _shell["AudioClip.Route"]);
        body.Children.Add(new TextBlock { Text = _shell["AudioClip.Route"], Classes = { "caption" } });
        body.Children.Add(routing);
        var routeHint = new TextBlock
        {
            Text = _shell["AudioClip.SharedRoute"], TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Classes = { "caption" }
        };
        ToolTip.SetTip(routing, routeHint.Text);
        var error = new TextBlock { Classes = { "error" }, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        body.Children.Add(error);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        var apply = new Button { Content = _shell["AudioClip.Apply"], Classes = { "primary" } };
        var create = new Button { Content = _shell["AudioClip.NewChannel"] };
        AutomationProperties.SetAutomationId(apply, "AudioClip.Apply");
        AutomationProperties.SetAutomationId(create, "AudioClip.NewChannel");
        buttons.Children.Add(apply);
        buttons.Children.Add(create);
        body.Children.Add(buttons);
        var flyout = new Flyout { Content = body };
        _audioFlyout?.Hide();
        _audioFlyout = flyout;
        apply.Click += async (_, _) =>
        {
            try
            {
                decimal Read(string key) => decimal.Parse(fields[key].Text ?? "", NumberStyles.Float,
                    CultureInfo.InvariantCulture);

                long Frame(string key, decimal scale = 1) =>
                    checked((long)decimal.Round(Read(key) * rate / scale, 0, MidpointRounding.AwayFromZero));

                var updated = clip with
                {
                    OffsetFrames = Frame("Offset"), LengthFrames = Frame("Length"),
                    FadeInFrames = Frame("FadeIn", 1000), FadeOutFrames = Frame("FadeOut", 1000), GainDb = Read("Gain"),
                    StretchRatio = Read("Stretch") / 100
                };
                if (updated.OffsetFrames != clip.OffsetFrames || updated.LengthFrames != clip.LengthFrames ||
                    updated.FadeInFrames != clip.FadeInFrames || updated.FadeOutFrames != clip.FadeOutFrames)
                    updated = updated with { EnvelopeOffsetFrames = 0, EnvelopeLengthFrames = 0 };
                if (updated.StretchRatio is < .5m or > 2m || updated.StretchRatio != 1 &&
                    (AudioClipSource.EnvelopeLength(updated) < rate / 10 ||
                     AudioClipSource.EnvelopeLength(updated) > rate * 60L))
                {
                    error.Text = _shell["AudioClip.StretchRange"];
                    return;
                }
                var route = routing.SelectedIndex > 0
                    ? channels[routing.SelectedIndex - 1].Id
                    : (Id<MixerChannel>?)null;
                PendingCommand = _workflow.ConfigureAudioClipAsync(basis, updated, route);
                await PendingCommand;
                if (!_workflow.State.StatusKey.EndsWith("Failed", StringComparison.Ordinal)) flyout.Hide();
                else error.Text = _workflow.State.Detail;
            }
            catch (Exception failure) when (failure is FormatException or OverflowException)
            {
                error.Text = _shell["AudioClip.Invalid"];
            }
        };
        create.Click += async (_, _) =>
        {
            PendingCommand = _workflow.CreateAudioMixerChannelAsync(basis, sound.Id);
            await PendingCommand;
            flyout.Hide();
        };
        flyout.ShowAt(_buttons["AudioProperties"]);
    }
}
