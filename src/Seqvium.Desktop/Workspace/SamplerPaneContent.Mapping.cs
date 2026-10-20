// SPDX-License-Identifier: Apache-2.0

using System.Collections.Immutable;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Seqvium.Core;
using Seqvium.Desktop.Presentation;
using Seqvium.Desktop.Workflow;

namespace Seqvium.Desktop.Workspace;

internal sealed partial class SamplerPaneContent
{
    private SamplerKeyboard _keyboard = null!;
    private SamplerZone? _zone;
    private Id<SamplerZone>? _selectedZone;

    private readonly ComboBox _sources = new()
    {
        MinWidth = 80, HorizontalAlignment = HorizontalAlignment.Stretch,
        HorizontalContentAlignment = HorizontalAlignment.Left, MaxDropDownHeight = 320
    };

    private readonly Dictionary<string, TextBox> _keys = [];
    private readonly Dictionary<string, TextBlock> _keyLabels = [];
    private bool _keyDraft, _mappingRefresh;
    private Button _removeSource = null!;

    private readonly Avalonia.Controls.Primitives.ScrollBar _keyPan = new()
    {
        Orientation = Orientation.Horizontal, Minimum = 0, Maximum = 92, Value = 48, ViewportSize = 36,
        VerticalAlignment = VerticalAlignment.Center
    };

    private readonly TextBlock _keyView = new()
        { Classes = { "caption" }, VerticalAlignment = VerticalAlignment.Center };

    private bool _viewRefresh;
    private string[]? _sourceNames;

    private void BuildMapping(StackPanel body)
    {
        var deck = new StackPanel { Classes = { "compact" } };
        var chassis = new Border { Child = deck };
        chassis.Bind(Border.BackgroundProperty, this.GetResourceObservable("Material.Panel"));
        chassis.Bind(Border.BoxShadowProperty, this.GetResourceObservable("Material.Recess"));
        body.Children.Add(chassis);
        _keyboard = new(_shell);
        Identify(_keyboard, "Keyboard");
        _keyboard.Begin = () => Ready;
        _keyboard.Selection += SelectZone;
        _keyboard.NotePressed += pitch =>
        {
            _previewMode.SelectedIndex = 1;
            Run(() => HoldPreviewAsync(pitch));
        };
        _keyboard.Preview += zones => FillKeys(zones.Single(z => z.Id == _selectedZone));
        _keyboard.Completed += zones => Run(() => CommitMapAsync(zones));
        _keyboard.Cancelled += () =>
        {
            _keyDraft = false;
            Refresh();
        };
        var navigation = new Grid { ColumnDefinitions = new("Auto,*,Auto") };
        navigation.Bind(Grid.ColumnSpacingProperty, this.GetResourceObservable("Space.Small"));
        Identify(_keyPan, "KeyPan");
        navigation.Children.Add(_keyView);
        navigation.Children.Add(_keyPan);
        Grid.SetColumn(_keyPan, 1);
        var fit = Button("KeyFit", () =>
        {
            _keyboard.Fit();
            return Task.CompletedTask;
        });
        navigation.Children.Add(fit);
        Grid.SetColumn(fit, 2);
        _keyboard.ViewChanged += () =>
        {
            _viewRefresh = true;
            _keyPan.Maximum = 128 - _keyboard.VisibleKeys;
            _keyPan.ViewportSize = _keyboard.VisibleKeys;
            _keyPan.Value = _keyboard.FirstKey;
            _keyView.Text = PianoNotes.Name((decimal)_keyboard.FirstKey) + " – " +
                            PianoNotes.Name((decimal)(_keyboard.FirstKey + _keyboard.VisibleKeys - 1));
            _viewRefresh = false;
        };
        _keyPan.PropertyChanged += (_, e) =>
        {
            if (!_viewRefresh && e.Property == Avalonia.Controls.Primitives.ScrollBar.ValueProperty)
                _keyboard.Pan(_keyPan.Value);
        };
        _keyboard.Pan(48);
        var commands = new Grid { ColumnDefinitions = new("*,Auto,Auto") };
        commands.Bind(Grid.ColumnSpacingProperty, this.GetResourceObservable("Space.Gap"));
        Identify(_sources, "Sources");
        commands.Children.Add(_sources);
        var add = Button("AddSources", ImportSourcesAsync);
        commands.Children.Add(add);
        Grid.SetColumn(add, 1);
        _removeSource = Button("RemoveSource",
            () => _sound is null
                ? Task.CompletedTask
                : CommitMapAsync([.. SamplerKeymap.Zones(_sound).Where(z => z.Id != _selectedZone)]));
        var distribute = Button("Distribute", () =>
        {
            if (_sound is null) return Task.CompletedTask;
            try { return CommitMapAsync(SamplerKeymap.Distribute(SamplerKeymap.Zones(_sound))); }
            catch (ArgumentException)
            {
                _status.Text = _shell["Sampler.DistributeUnavailable"];
                _status.IsVisible = true;
                return Task.CompletedTask;
            }
        });
        var actions = new StackPanel();
        actions.Children.Add(distribute);
        actions.Children.Add(_removeSource);
        var more = Button("MapActions", () => Task.CompletedTask);
        more.Flyout = new Flyout { Content = actions };
        commands.Children.Add(more);
        Grid.SetColumn(more, 2);
        _sources.SelectionChanged += (_, _) =>
        {
            if (!_mappingRefresh && _sound is not null && _sources.SelectedIndex >= 0)
                SelectZone(SamplerKeymap.Zones(_sound).OrderBy(z => z.LowKey).ElementAt(_sources.SelectedIndex).Id);
        };
        var exact = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (string key in new[] { "Root", "Low", "High", "LowVelocity", "HighVelocity" })
        {
            var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            var field = new TextBox { Width = 72, Classes = { "numeric" } };
            field.Bind(TextBox.BackgroundProperty, this.GetResourceObservable("Surface.Workspace"));
            field.Bind(TextBox.BorderBrushProperty,
                this.GetResourceObservable(key == "Root" ? "Accent.Action" : "Border.Default"));
            if (key == "Root") label.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("Accent.Action"));
            var row = new Grid
                { ColumnDefinitions = new(key.Contains("Velocity") ? "104,72" : key == "Root" ? "60,72" : "28,72"), Classes = { "parameterRow" } };
            row.Bind(MarginProperty, this.GetResourceObservable("Padding.Parameter"));
            row.Children.Add(label);
            row.Children.Add(field);
            Grid.SetColumn(field, 1);
            exact.Children.Add(row);
            _keys[key] = field;
            _keyLabels[key] = label;
            Identify(field, key);
            Identify(label, "Label." + key);
            field.PropertyChanged += (_, e) =>
            {
                if (!_mappingRefresh && e.Property == TextBox.TextProperty) _keyDraft = true;
            };
            field.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    e.Handled = true;
                    Run(CommitKeysAsync);
                }

                if (e.Key == Key.Escape)
                {
                    e.Handled = true;
                    _keyDraft = false;
                    Refresh();
                }
            };
        }

        exact.Children.Add(Button("ApplyKeys", CommitKeysAsync));
        var identity = new Grid { ColumnDefinitions = new("*"), RowDefinitions = new("Auto,Auto") };
        identity.Bind(Grid.ColumnSpacingProperty, this.GetResourceObservable("Space.Section"));
        identity.Bind(Grid.RowSpacingProperty, this.GetResourceObservable("Space.Gap"));
        identity.Children.Add(commands);
        identity.Children.Add(exact);
        Grid.SetRow(exact, 1);
        bool wide = false;
        identity.SizeChanged += (_, _) =>
        {
            bool next = identity.Bounds.Width >= 1120;
            if (next == wide) return;
            wide = next;
            identity.ColumnDefinitions = new(wide ? "*,Auto" : "*");
            identity.RowDefinitions = new(wide ? "Auto" : "Auto,Auto");
            Grid.SetRow(exact, wide ? 0 : 1);
            Grid.SetColumn(exact, wide ? 1 : 0);
        };
        deck.Children.Add(identity);
        deck.Children.Add(_keyboard);
        deck.Children.Add(navigation);
        var audition = new WrapPanel();
        _previewMode.ItemsSource = new[] { _shell["Sampler.SelectedWav"], _shell["Sampler.Instrument"] };
        _previewMode.SelectedIndex = 0;
        Identify(_previewMode, "PreviewMode");
        Identify(_previewVelocity, "PreviewVelocity");
        _previewVelocityLabel.Text = _shell["Sampler.Velocity"];
        var velocityRow = new Grid { ColumnDefinitions = new("Auto,64"), Classes = { "parameterRow" } };
        velocityRow.Bind(MarginProperty, this.GetResourceObservable("Padding.Parameter"));
        velocityRow.Children.Add(_previewVelocityLabel);
        velocityRow.Children.Add(_previewVelocity);
        Grid.SetColumn(_previewVelocity, 1);
        audition.Children.Add(_previewMode);
        audition.Children.Add(velocityRow);
        _previewVelocity.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty && SamplerVelocityText.TryRead(_previewVelocity.Text, out var v))
            {
                _keyboard.PreviewVelocity = v;
                _keyboard.InvalidateVisual();
            }
        };
        deck.Children.Add(audition);
    }

    private void SelectZone(Id<SamplerZone> id)
    {
        if (_selectedZone == id) return;
        _wave.Cancel();
        _keyboard.Cancel();
        _draft = null;
        _textDraft = false;
        _keyDraft = false;
        _selectedZone = id;
        if (_sound is not null) _keyboard.Reveal(SamplerKeymap.Zones(_sound).Single(z => z.Id == id).RootPitch);
        // Selection is observation only; stop the old solo source before another preview can start.
        if (_held || _workflow.SamplerPreviewPlaying)
            Run(async () =>
            {
                await _workflow.StopSamplerAsync();
                _held = false;
                Refresh();
            });
        Observe(_target);
    }

    private void FillKeys(SamplerZone z)
    {
        _mappingRefresh = true;
        var modes = new[] { _shell["Sampler.SelectedWav"], _shell["Sampler.Instrument"] };
        if (_previewMode.ItemsSource is not string[] current || !current.SequenceEqual(modes))
        {
            int selected = _previewMode.SelectedIndex;
            _previewMode.ItemsSource = modes;
            _previewMode.SelectedIndex = Math.Max(0, selected);
        }
        _previewVelocityLabel.Text = _shell["Sampler.Velocity"];
        AutomationProperties.SetName(_previewVelocity, _shell["Sampler.Velocity"]);
        AutomationProperties.SetName(_previewMode, _shell["Sampler.PreviewMode"]);
        _keys["Root"].Text = z.RootPitch == decimal.Truncate(z.RootPitch)
            ? PianoNotes.Name(z.RootPitch)
            : z.RootPitch.ToString(CultureInfo.InvariantCulture);
        _keys["Low"].Text = PianoNotes.Name(z.LowKey);
        _keys["High"].Text = PianoNotes.Name(z.HighKey);
        _keys["LowVelocity"].Text = (z.LowVelocity * 100).ToString("0.############################", CultureInfo.InvariantCulture);
        _keys["HighVelocity"].Text = (z.HighVelocity * 100).ToString("0.############################", CultureInfo.InvariantCulture);
        _mappingRefresh = false;
    }

    private void RefreshMapping()
    {
        if (_sound is null || _zone is null) return;
        _mappingRefresh = true;
        var zones = SamplerKeymap.Zones(_sound).OrderBy(z => z.LowKey).ToArray();
        var names = zones.Select(z => PianoNotes.Name(z.RootPitch) + " · " +
                                      $"{z.LowVelocity * 100:0.##}–{z.HighVelocity * 100:0.##}% · " +
                                      _workflow.State.Snapshot.State.Resources.Single(r => r.Id == z.ResourceId).Name)
            .ToArray();
        if (_sourceNames is null || !_sourceNames.SequenceEqual(names))
        {
            _sourceNames = names;
            _sources.ItemsSource = names.Select(name =>
            {
                var pieces = name.Split("__", StringSplitOptions.RemoveEmptyEntries);
                string display = pieces.Length >= 3
                    ? name[..(name.IndexOf('·') + 1)] + " " + pieces[^2].Replace('_', ' ')
                    : name;
                var item = new TextBlock
                {
                    Text = display,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                item.Bind(TextBlock.ForegroundProperty, this.GetResourceObservable("Text.Primary"));
                ToolTip.SetTip(item, name);
                return item;
            }).ToArray();
        }

        _sources.SelectedIndex = Array.FindIndex(zones, z => z.Id == _selectedZone);
        AutomationProperties.SetName(_sources, _shell["Sampler.Sources"]);
        ToolTip.SetTip(_sources,
            _workflow.State.Snapshot.State.Resources.Single(r => r.Id == _zone.ResourceId).Name + "\n" +
            _shell["Sampler.SharedTip"]);
        if (!_keyboard.HasGesture) _keyboard.Zones = [.. zones];
        _keyboard.SourceNames = zones.ToDictionary(z => z.Id, z =>
            _workflow.State.Snapshot.State.Resources.Single(r => r.Id == z.ResourceId).Name);
        _keyboard.Selected = _selectedZone;
        _keyboard.InvalidateVisual();
        foreach (var (key, field) in _keys)
        {
            _keyLabels[key].Text = _shell["Sampler." + key];
            AutomationProperties.SetName(field, _shell["Sampler." + key]);
            ToolTip.SetTip(field, _shell[key.Contains("Velocity") ? "Sampler.VelocityTip" : "Sampler.PitchTip"]);
            field.IsEnabled = !_workflow.State.Busy;
        }

        if (!_keyDraft && !_keyboard.HasGesture) FillKeys(_zone);
        _mappingRefresh = false;
        _removeSource.IsEnabled = zones.Length > 1 && !_workflow.State.Busy;
    }

    private async Task CommitKeysAsync()
    {
        if (_sound is null || _zone is null || !SamplerPitchText.TryRead(_keys["Root"].Text, out var root) ||
            !SamplerPitchText.TryRead(_keys["Low"].Text, out var low) || low != decimal.Truncate(low) ||
            !SamplerPitchText.TryRead(_keys["High"].Text, out var high) || high != decimal.Truncate(high) ||
            !SamplerVelocityText.TryRead(_keys["LowVelocity"].Text, out var lowVelocity) ||
            !SamplerVelocityText.TryRead(_keys["HighVelocity"].Text, out var highVelocity))
        {
            MapError();
            return;
        }

        await CommitMapAsync([
            .. SamplerKeymap.Zones(_sound).Select(z => z.Id == _zone.Id
                ? z with { RootPitch = root, LowKey = (int)low, HighKey = (int)high,
                    LowVelocity = lowVelocity, HighVelocity = highVelocity }
                : z)
        ]);
    }

    private async Task CommitMapAsync(ImmutableArray<SamplerZone> zones)
    {
        if (_target is null || _sound is null) return;
        try
        {
            SamplerKeymap.Validate(_sound with
            {
                SamplerZones = zones, ResourceIds = [.. zones.Select(z => z.ResourceId).Distinct()]
            });
            if (zones.IsEmpty)
            {
                MapError();
                return;
            }

            bool changed = await _workflow.SetSamplerMapAsync(_target, _generation, zones);
            _keyDraft = false;
            _draft = null;
            Observe(_target);
            if (!changed && _workflow.State.Detail is not null) MapError();
        }
        catch (ArgumentException)
        {
            _keyboard.Cancel();
            MapError();
        }
    }

    private void MapError()
    {
        _status.Text = _shell["Sampler.MapInvalid"];
        _status.IsVisible = true;
    }

    private async Task ImportSourcesAsync()
    {
        if (_target is not { } target || TopLevel.GetTopLevel(this) is not Window owner) return;
        long generation = _generation;
        var files = await owner.StorageProvider.OpenFilePickerAsync(new()
        {
            Title = _shell["Sampler.AddSources"], AllowMultiple = true,
            FileTypeFilter = [new("WAV") { Patterns = ["*.wav"] }]
        });
        try
        {
            if (files.Count == 0) return;
            if (files.Count + SamplerKeymap.Zones(_sound!).Length > SamplerKeymap.MaximumZones)
            {
                MapError();
                return;
            }

            var paths = files.Select(f => f.TryGetLocalPath() ?? throw new IOException("Local WAV required."))
                .ToArray();
            var roots = await Task.Run(async () =>
            {
                var result = new decimal?[paths.Length];
                for (int i = 0; i < paths.Length; i++)
                {
                    await using var file = File.OpenRead(paths[i]);
                    result[i] = SampleRootMetadata.ReadKnown(paths[i], await WavDecoder.ReadBytesAsync(file));
                }

                return result;
            });
            var names = SamplerKeymap.Zones(_sound!).Select(z =>
                _workflow.State.Snapshot.State.Resources.Single(r => r.Id == z.ResourceId).Name).ToArray();
            var review = new SamplerImportWindow(_shell, paths, roots, _sound, names);
            var accepted = await review.ShowDialog<bool>(owner);
            if (!accepted || _disposed || _target != target || _workflow.State.Generation != generation) return;
            bool changed = await _workflow.AddSamplerSourcesAsync(target, generation,
                paths.Select((path, i) => (path, roots[i]!.Value)).ToArray(), review.ReviewedZones);
            Observe(_target);
            if (!changed) MapError();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException
                                      or NotSupportedException)
        {
            MapError();
        }
        finally
        {
            foreach (var file in files) file.Dispose();
        }
    }
}

internal static class SamplerVelocityText
{
    internal static bool TryRead(string? text, out decimal velocity)
    {
        velocity = 0;
        if (!decimal.TryParse(text?.Trim().TrimEnd('%').Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var percent) || percent is < 0 or > 100) return false;
        velocity = percent / 100;
        return true;
    }
}

internal static class SamplerPitchText
{
    internal static bool TryRead(string? text, out decimal pitch)
    {
        pitch = 0;
        text = text?.Trim();
        if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out pitch))
            return pitch is >= 0 and <= 127;
        if (string.IsNullOrEmpty(text) || text.Length < 2) return false;
        int note = char.ToUpperInvariant(text[0]) switch
        {
            'C' => 0, 'D' => 2, 'E' => 4, 'F' => 5, 'G' => 7, 'A' => 9, 'B' => 11, _ => -1000
        };
        int offset = 1;
        if (text.Length > 2 && text[1] is '#' or '♯' or 'b' or '♭')
        {
            note += text[1] is '#' or '♯' ? 1 : -1;
            offset++;
        }

        if (!int.TryParse(text[offset..], NumberStyles.Integer, CultureInfo.InvariantCulture, out int octave))
            return false;
        pitch = (octave + 1m) * 12 + note;
        return pitch is >= 0 and <= 127;
    }
}
