// SPDX-License-Identifier: Apache-2.0
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Seqvium.Desktop.Presentation;

namespace Seqvium.Desktop.Settings;

/// <summary>One global preference surface. Pages retain focus; changes apply immediately through the session owner.</summary>
internal sealed class SettingsWindow : Window
{
    private readonly ShellSession _shell;
    private readonly Func<HostPreferences, Task> _applyAppearance;
    private readonly Dictionary<string, Button> _navigation = [];
    private readonly Dictionary<string, Control> _pages = [];
    private readonly List<Action> _localize = [];
    private readonly ContentControl _page = new();
    private readonly ComboBox _language = new(), _theme = new();
    private readonly CheckBox _auto = new();
    private readonly TextBox _search = new();
    private readonly StackPanel _commands = new();
    private readonly StackPanel _capture = new() { Classes = { "spaced" } };
    private readonly TextBlock _captureTitle = new() { Classes = { "section" } };
    private readonly TextBlock _captureHint = new() { TextWrapping = TextWrapping.Wrap };
    private readonly Button _assign = new(), _cancel = new();
    private readonly Expander _fixedKeys = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
    private string? _editing, _candidate;
    private bool _hasCandidate, _refreshing;
    private HostLanguage? _renderedLanguage;
    private ControlPreferences? _renderedControls;
    internal Task PendingChange { get; private set; } = Task.CompletedTask;

    internal SettingsWindow(ShellSession shell, Func<HostPreferences, Task> applyAppearance)
    {
        _shell = shell; _applyAppearance = applyAppearance;
        Width = 720; Height = 570; MinWidth = 500; MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        using (var iconStream = AssetLoader.Open(new Uri("avares://Seqvium.Desktop/Assets/Brand/seqvium.ico")))
            Icon = new WindowIcon(iconStream);
        var root = new Grid { ColumnDefinitions = new("136,*"), RowDefinitions = new("*,Auto") };
        var nav = new StackPanel { Classes = { "spaced" } };
        nav.Bind(MarginProperty, nav.GetResourceObservable("Padding.Content"));
        var navSurface = new Border { Child = nav, BorderThickness = new(0, 0, 1, 0) };
        navSurface.Bind(Border.BackgroundProperty, navSurface.GetResourceObservable("Material.Panel"));
        navSurface.Bind(Border.BorderBrushProperty, navSurface.GetResourceObservable("Border.Default"));
        root.Children.Add(navSurface);
        foreach (string section in new[] { "General", "Controls", "About" })
        {
            var button = Button("Settings." + section, "Settings." + section);
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.Click += (_, _) => SelectPage(section);
            nav.Children.Add(button); _navigation.Add(section, button);
        }
        _page.Bind(MarginProperty, _page.GetResourceObservable("Padding.Dialog"));
        Grid.SetColumn(_page, 1); root.Children.Add(_page);
        var footer = new Grid { ColumnDefinitions = new("*,Auto"), Margin = new(12, 6) };
        var notice = new TextBlock { Classes = { "caption", "warning" }, TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center };
        _localize.Add(() => { notice.Text = shell.Notice; notice.IsVisible = shell.HasNotice; });
        footer.Children.Add(notice);
        var close = Button("Settings.Close", "Settings.Close");
        close.Click += (_, _) => Close(); Grid.SetColumn(close, 1); footer.Children.Add(close);
        Grid.SetRow(footer, 1); Grid.SetColumnSpan(footer, 2); root.Children.Add(footer);
        Content = root;
        _pages.Add("General", GeneralPage());
        _pages.Add("Controls", ControlsPage());
        _pages.Add("About", AboutPage());
        shell.PropertyChanged += Changed;
        Closed += (_, _) => shell.PropertyChanged -= Changed;
        Deactivated += (_, _) => CancelCapture();
        AddHandler(KeyDownEvent, CaptureKey, RoutingStrategies.Tunnel);
        Refresh(); SelectPage("General");
    }

    private TextBlock Label(string key, string? style = null)
    {
        var label = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        if (style is not null) label.Classes.Add(style);
        _localize.Add(() => label.Text = _shell[key]);
        return label;
    }

    private Button Button(string key, string id)
    {
        var button = new Button();
        AutomationProperties.SetAutomationId(button, id);
        _localize.Add(() => { button.Content = _shell[key]; AutomationProperties.SetName(button, _shell[key]); });
        return button;
    }

    private static ScrollViewer Scroll(Control content) => new()
    {
        Content = content, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
    };

    private Control GeneralPage()
    {
        var body = new StackPanel { Classes = { "spaced" } };
        body.Children.Add(Label("Settings.General", "section"));
        body.Children.Add(Row("Preference.Language", _language));
        body.Children.Add(Row("Preference.Theme", _theme));
        AutomationProperties.SetAutomationId(_language, "Settings.Language");
        AutomationProperties.SetAutomationId(_theme, "Settings.Theme");
        _language.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _language.SelectedIndex >= 0)
                PendingChange = _applyAppearance(_shell.Preferences with { Language = (HostLanguage)_language.SelectedIndex });
        };
        _theme.SelectionChanged += (_, _) =>
        {
            if (!_refreshing && _theme.SelectedIndex >= 0)
                PendingChange = _applyAppearance(_shell.Preferences with { Theme = ThemeCatalog.All[_theme.SelectedIndex].Theme });
        };
        return Scroll(body);
    }

    private Control Row(string key, Control control)
    {
        var row = new Grid { ColumnDefinitions = new("*,190"), ColumnSpacing = 12 };
        row.Children.Add(Label(key)); Grid.SetColumn(control, 1); row.Children.Add(control);
        control.HorizontalAlignment = HorizontalAlignment.Stretch;
        _localize.Add(() => AutomationProperties.SetName(control, _shell[key]));
        return row;
    }

    private Control ControlsPage()
    {
        _commands.Bind(MarginProperty, _commands.GetResourceObservable("Navigation.ScrollGutter"));
        var body = new Grid { RowDefinitions = new("Auto,Auto,Auto,Auto,*,Auto"), RowSpacing = 8 };
        body.Children.Add(Label("Settings.Behavior", "section"));
        _localize.Add(() => _auto.Content = _shell["Browser.AutoPreview"]);
        AutomationProperties.SetAutomationId(_auto, "Settings.AutoPreview");
        _auto.IsCheckedChanged += (_, _) =>
        {
            if (!_refreshing) PendingChange = _shell.SetControlsAsync(_shell.Controls with { AutoPreview = _auto.IsChecked == true });
        };
        Grid.SetRow(_auto, 1); body.Children.Add(_auto);
        var heading = new Grid { ColumnDefinitions = new("*,Auto") };
        heading.Children.Add(Label("Settings.Shortcuts", "section"));
        var reset = Button("Settings.ResetAll", "Settings.ResetAll");
        reset.Click += (_, _) =>
        {
            CancelCapture(); PendingChange = _shell.SetControlsAsync(_shell.Controls with { Overrides = null });
        };
        Grid.SetColumn(reset, 1); heading.Children.Add(reset); Grid.SetRow(heading, 2); body.Children.Add(heading);
        _localize.Add(() => _search.PlaceholderText = _shell["Settings.Search"]);
        AutomationProperties.SetAutomationId(_search, "Settings.Search");
        _search.TextChanged += (_, _) => RefreshCommands();
        Grid.SetRow(_search, 3); body.Children.Add(_search);
        var list = Scroll(_commands); Grid.SetRow(list, 4); body.Children.Add(list);
        _capture.Children.Add(_captureTitle); _capture.Children.Add(_captureHint);
        var actions = new WrapPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(_assign); actions.Children.Add(_cancel);
        _assign.Margin = new(0, 0, 6, 0);
        AutomationProperties.SetAutomationId(_assign, "Settings.Assign");
        AutomationProperties.SetAutomationId(_cancel, "Settings.CancelCapture");
        _assign.Click += (_, _) =>
        {
            if (_editing is not { } id || !_hasCandidate) return;
            PendingChange = _shell.SetControlsAsync(_shell.Controls.Assign(id, _candidate, true));
            CancelCapture();
        };
        _cancel.Click += (_, _) => CancelCapture();
        _capture.Children.Add(actions);
        var bottom = new StackPanel();
        bottom.Children.Add(_capture);
        _localize.Add(() => _fixedKeys.Header = _shell["Settings.FixedKeys"]);
        var description = Label("Settings.FixedKeysDetail", "caption"); description.TextWrapping = TextWrapping.Wrap;
        var details = Scroll(description); details.MaxHeight = 96;
        _fixedKeys.Content = details;
        _fixedKeys.PropertyChanged += (_, args) =>
        {
            if (args.Property == Expander.IsExpandedProperty && _fixedKeys.IsExpanded) CancelCapture();
        };
        bottom.Children.Add(_fixedKeys);
        Grid.SetRow(bottom, 5); body.Children.Add(bottom);
        return body;
    }

    private Control AboutPage()
    {
        var body = new StackPanel { Classes = { "spaced" } };
        var brand = new StackPanel { Orientation = Orientation.Horizontal, Classes = { "spaced" } };
        using var logoStream = AssetLoader.Open(new Uri("avares://Seqvium.Desktop/Assets/Brand/Icons/seqvium-128.png"));
        var bitmap = new Bitmap(logoStream);
        var logo = new Image { Source = bitmap };
        Closed += (_, _) => bitmap.Dispose();
        logo.Bind(WidthProperty, logo.GetResourceObservable("Brand.AboutSize"));
        logo.Bind(HeightProperty, logo.GetResourceObservable("Brand.AboutSize"));
        brand.Children.Add(logo);
        var identity = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        var name = new TextBlock { Text = "Seqvium", Classes = { "brand" } };
        name.Bind(TextBlock.FontSizeProperty, name.GetResourceObservable("Type.BrandDisplay"));
        identity.Children.Add(name); identity.Children.Add(new TextBlock { Text = "chaos into structure.", Classes = { "caption" } });
        brand.Children.Add(identity); body.Children.Add(brand);
        body.Children.Add(new TextBlock { Text = _shell.Version });
        var scope = Label("Host.Scope", "secondary"); scope.TextWrapping = TextWrapping.Wrap; body.Children.Add(scope);
        return Scroll(body);
    }

    internal void SelectPage(string section)
    {
        CancelCapture(); _page.Content = _pages[section];
        foreach (var (id, button) in _navigation)
        {
            button.FontWeight = id == section ? FontWeight.SemiBold : FontWeight.Normal;
            button.BorderThickness = new(id == section ? 3 : 1, 1, 1, 1);
            button.Bind(Avalonia.Controls.Button.BorderBrushProperty, button.GetResourceObservable(id == section ? "Accent.Action" : "Border.Default"));
        }
    }

    private void Changed(object? sender, PropertyChangedEventArgs args) => Refresh();
    private void Refresh()
    {
        _refreshing = true;
        try
        {
            Title = _shell["Settings.Title"];
            foreach (var update in _localize) update();
            bool languageChanged = _renderedLanguage != _shell.Preferences.Language;
            if (languageChanged)
            {
                _language.ItemsSource = new[] { "English", "Русский" };
                _theme.ItemsSource = ThemeCatalog.All.Select(theme => _shell[theme.LabelKey]).ToArray();
            }
            _language.SelectedIndex = (int)_shell.Preferences.Language;
            _theme.SelectedIndex = ThemeCatalog.All.FindIndex(theme => theme.Theme == _shell.Preferences.Theme);
            _auto.IsChecked = _shell.Controls.AutoPreview;
            if (languageChanged || !ReferenceEquals(_renderedControls, _shell.Controls)) RefreshCommands();
            RefreshCapture();
            _renderedLanguage = _shell.Preferences.Language; _renderedControls = _shell.Controls;
        }
        finally { _refreshing = false; }
    }

    private void RefreshCommands()
    {
        _commands.Children.Clear();
        string search = _search.Text?.Trim() ?? "";
        string? group = null;
        foreach (var command in HostCommands.All.Where(command =>
                     (_shell[command.LabelKey] + " " + _shell.Controls.Binding(command.Id))
                     .Contains(search, StringComparison.OrdinalIgnoreCase)))
        {
            bool customized = _shell.Controls.IsCustomized(command.Id);
            if (group != command.GroupKey)
            {
                group = command.GroupKey;
                _commands.Children.Add(new TextBlock { Text = _shell[group], Classes = { "caption" }, Margin = new(0, 8, 0, 2) });
            }
            var row = new Grid { ColumnDefinitions = new("*,142,Auto"), ColumnSpacing = 6 };
            row.ColumnDefinitions[2].Width = new GridLength(HostMetrics.Values["Control.IconWidth"]);
            var label = new TextBlock { Text = _shell[command.LabelKey] + (customized ? " •" : ""), VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis };
            ToolTip.SetTip(label, _shell[command.LabelKey] + (customized ? " · " + _shell["Settings.Customized"] : "")); row.Children.Add(label);
            var binding = new Button { Content = _shell.Controls.Binding(command.Id) is { } chord
                    ? HostCommands.Display(chord) : _shell["Settings.Unassigned"],
                HorizontalAlignment = HorizontalAlignment.Stretch };
            binding.FontWeight = customized ? FontWeight.SemiBold : FontWeight.Normal;
            AutomationProperties.SetAutomationId(binding, "Shortcut." + command.Id);
            AutomationProperties.SetName(binding, label.Text + ": " + binding.Content);
            binding.Click += (_, _) => BeginCapture(command.Id, false);
            Grid.SetColumn(binding, 1); row.Children.Add(binding);
            if (customized)
            {
                var reset = new Button { Classes = { "command" } };
                reset.Content = HostIcons.Create("Refresh", reset, 14);
                ToolTip.SetTip(reset, _shell["Settings.Default"] + " · " + HostCommands.Display(command.DefaultBinding));
                AutomationProperties.SetName(reset, _shell["Settings.Default"] + ": " + _shell[command.LabelKey]);
                AutomationProperties.SetAutomationId(reset, "Shortcut.Reset." + command.Id);
                reset.Click += (_, _) => BeginCapture(command.Id, true);
                Grid.SetColumn(reset, 2); row.Children.Add(reset);
            }
            _commands.Children.Add(row);
        }
        if (!_commands.Children.Any()) _commands.Children.Add(new TextBlock { Text = _shell["Settings.NoResults"], Classes = { "caption" } });
    }

    private void BeginCapture(string id, bool reset)
    {
        _fixedKeys.IsExpanded = false;
        _editing = id; _candidate = reset ? HostCommands.All.Single(command => command.Id == id).DefaultBinding : null;
        _hasCandidate = reset; RefreshCapture();
        _cancel.Focus();
    }

    private void CancelCapture()
    {
        bool wasEditing = _editing is not null;
        _editing = null; _hasCandidate = false; _capture.IsVisible = false;
        if (wasEditing && IsVisible) _search.Focus();
    }

    private void RefreshCapture()
    {
        _capture.IsVisible = _editing is not null;
        if (_editing is not { } id) return;
        _captureTitle.Text = _shell[HostCommands.All.Single(command => command.Id == id).LabelKey];
        var conflict = _shell.Controls.Conflict(id, _candidate);
        _captureHint.Text = !_hasCandidate ? _shell["Settings.PressKeys"] :
            (_candidate is null ? _shell["Settings.Unassigned"] : HostCommands.Display(_candidate)) + (conflict is null ? "" : " · " + _shell["Settings.UsedBy"] + " " +
                _shell[HostCommands.All.Single(command => command.Id == conflict).LabelKey]);
        _assign.Content = _shell[conflict is null ? "Settings.Assign" : "Settings.Replace"];
        _assign.IsEnabled = _hasCandidate; _cancel.Content = _shell["Settings.Cancel"];
    }

    private void CaptureKey(object? sender, KeyEventArgs e)
    {
        if (_editing is null)
        {
            if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None) { Close(); e.Handled = true; }
            return;
        }
        // Native window/menu and ordinary focus traversal retain their platform meaning.
        if ((e.KeyModifiers & (KeyModifiers.Alt | KeyModifiers.Meta)) != 0 || e.Key == Key.Tab) return;
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None) { CancelCapture(); e.Handled = true; return; }
        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift) return;
        if (e.Key == Key.Enter && e.KeyModifiers == KeyModifiers.None) return;
        e.Handled = true;
        if (e.Key is Key.Delete or Key.Back && e.KeyModifiers == KeyModifiers.None)
        { _candidate = null; _hasCandidate = true; RefreshCapture(); return; }
        if (!HostCommands.IsAssignable(e.Key, e.KeyModifiers))
        { _hasCandidate = false; RefreshCapture(); _captureHint.Text = _shell["Settings.Reserved"]; return; }
        _candidate = new KeyGesture(e.Key, e.KeyModifiers).ToString(); _hasCandidate = true; RefreshCapture();
    }
}
