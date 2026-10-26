using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SaveLinkUtility.Services;

namespace SaveLinkUtility;

/// <summary>
/// The whole UI, built in code (no designer file, no XAML). Layout is plain absolute positioning
/// with DPI auto-scaling, so you can read it top to bottom.
/// </summary>
public sealed class MainForm : Form
{
    private const int ContentWidth = 448;
    private const int ShortcutTab = 0;
    private const int HelpTab = 1;
    private const int PreviewIconSize = 48;

    private readonly FaviconService _favicons = new();
    private readonly System.Windows.Forms.Timer _debounce = new() { Interval = 600 };
    private readonly ToolTip _tips = new();

    private readonly TabStrip _tabs = new();
    private readonly TableLayoutPanel _shortcutPage = new();
    private readonly HelpPage _helpPage = new();

    private readonly TextBox _url = new() { PlaceholderText = "Paste a link, or drag one onto this window" };
    private readonly TextBox _name = new() { PlaceholderText = "Name shown under the shortcut" };
    private readonly PictureBox _preview = new() { BorderStyle = BorderStyle.FixedSingle, SizeMode = PictureBoxSizeMode.CenterImage };
    private readonly Label _iconStatus = new() { ForeColor = SystemColors.GrayText, Text = "Paste or drop a link to begin" };
    private readonly LinkLabel _pickIcon = new() { Text = "Choose file…", AutoSize = true };
    private readonly LinkLabel _generatedIcon = new() { Text = "Generate", AutoSize = true };
    private readonly LinkLabel _resetIcon = new() { Text = "Reset", AutoSize = true };
    private readonly LinkLabel _saveIco = new() { Text = "Save as .ico…", AutoSize = true };
    private readonly TextBox _location = new() { ReadOnly = true };
    private readonly Button _browse = new() { Text = "Browse…" };
    private readonly CheckBox _saveIcon = new() { Text = "Save icon locally", Checked = true, AutoSize = true };
    private readonly CheckBox _createShortcut = new() { Text = "Create shortcut", Checked = true, AutoSize = true };
    private readonly Label _openWithLabel = new() { Text = "Open with", AutoSize = true };
    private readonly ComboBox _browserBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _appWindow = new() { Text = "Open in its own app window instead of a browser tab", AutoSize = true };
    private readonly Label _browserNote = new() { AutoSize = true, ForeColor = SystemColors.GrayText };
    private readonly Button _create = new() { Text = "Create Shortcut" };
    private readonly Label _status = new() { TextAlign = ContentAlignment.MiddleCenter };
    private readonly LinkLabel _show = new() { Text = "Show in folder", AutoSize = true, Visible = false };

    private CancellationTokenSource? _lookupCts;
    private Bitmap? _icon;
    private Bitmap? _sourceIcon;      // what the website lookup produced, kept so Reset can go back to it
    private string _sourceStatus = "";
    private bool _iconIsSource;
    private BrowserInfo? _defaultBrowser;
    private string _saveFolder = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    private string? _lastCreatedPath;

    private bool _ready;
    private bool _lookupRunning;
    private bool _nameEditedByUser;
    private bool _settingName;

    /// <summary>An entry in the "Open with" list. <see cref="Browser"/> is null for "Default browser".</summary>
    private sealed record BrowserChoice(BrowserInfo? Browser, string Text)
    {
        public override string ToString() => Text;
    }

    public MainForm()
    {
        Text = "SaveLinkUtility";
        // No automatic scaling: BuildUi sizes everything from the font and DPI via auto-sizing panels.
        AutoScaleMode = AutoScaleMode.None;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // Reuse the icon embedded in the exe (set via <ApplicationIcon>) for the title bar / taskbar.
        try { Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!); } catch { }

        BuildUi();

        // Drag & drop works anywhere on the form, including on top of the child controls.
        HookDrag(this);

        _url.TextChanged += (_, _) => OnUrlChanged();
        _name.TextChanged += (_, _) => OnNameChanged();
        _pickIcon.LinkClicked += (_, _) => PickIcon();
        _generatedIcon.LinkClicked += (_, _) => UseGeneratedIcon();
        _resetIcon.LinkClicked += (_, _) => ResetIcon();
        _saveIco.LinkClicked += (_, _) => SaveIcoAs();
        _browse.Click += (_, _) => BrowseForFolder();
        _saveIcon.CheckedChanged += (_, _) => UpdateUi();
        _createShortcut.CheckedChanged += (_, _) => UpdateUi();
        _browserBox.SelectedIndexChanged += (_, _) => UpdateUi();
        _appWindow.CheckedChanged += (_, _) => UpdateUi();
        _create.Click += (_, _) => CreateShortcut();
        _show.LinkClicked += (_, _) => ShowInFolder();
        _tabs.SelectedIndexChanged += (_, _) => ShowTab(_tabs.SelectedIndex);
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _ = LookupAsync();
        };

        AcceptButton = _create;
        Load += OnLoaded;
        _ready = true;
    }

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        switch (keyData)
        {
            case Keys.F1:
                _tabs.SelectedIndex = HelpTab;
                return true;
            case Keys.Escape when _tabs.SelectedIndex == HelpTab:
                _tabs.SelectedIndex = ShortcutTab;
                return true;
            case Keys.Control | Keys.Tab:
            case Keys.Control | Keys.Shift | Keys.Tab:
                _tabs.SelectedIndex = (_tabs.SelectedIndex + 1) % _tabs.Count;
                return true;
        }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    // ------------------------------------------------------------------ layout

    // Layout uses auto-sizing table panels: everything is sized from the font and DPI, so there is
    // no pixel arithmetic to get wrong at 125% / 150% / 200% scaling.
    private int Dp(int logical) => LogicalToDeviceUnits(logical);

    private void BuildUi()
    {
        int pad = Dp(16);

        // Outer frame: tab bar on top, then whichever page is selected. Hidden pages don't count
        // towards the auto size, and the Help page is given the shortcut page's size when shown.
        var frame = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Location = Point.Empty,
        };
        _tabs.Height = Dp(38);
        _tabs.TextIndent = pad;
        _tabs.Dock = DockStyle.Fill;
        _tabs.Margin = Padding.Empty;
        _tabs.AddTab("Shortcut");
        _tabs.AddTab("Help", "F1");
        _helpPage.Margin = Padding.Empty;
        _helpPage.Visible = false;
        frame.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        frame.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        frame.Controls.Add(_tabs, 0, 0);
        var pages = new Panel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Margin = Padding.Empty };
        pages.Controls.Add(_shortcutPage);
        pages.Controls.Add(_helpPage);
        frame.Controls.Add(pages, 0, 1);

        var root = _shortcutPage;
        root.AutoSize = true;
        root.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        root.ColumnCount = 1;
        root.Padding = new Padding(pad, Dp(14), pad, Dp(12));
        root.Margin = Padding.Empty;
        root.Location = Point.Empty;
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dp(ContentWidth)));

        void Row(Control c, int topGap = 0, int left = 0)
        {
            c.Margin = new Padding(left, topGap, 0, 0);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(c, 0, root.RowCount++);
        }
        void Field(string caption, Control box, int topGap)
        {
            Row(BoldLabel(caption), topGap);
            box.Dock = DockStyle.Top;
            Row(box, Dp(4));
        }

        Field("Website URL", _url, 0);
        Field("Shortcut name", _name, Dp(14));

        // Icon preview row
        var icon = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Dock = DockStyle.Fill };
        icon.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        icon.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _preview.Size = new Size(Dp(64), Dp(64));
        _preview.Margin = new Padding(0, 0, Dp(12), 0);
        icon.Controls.Add(_preview, 0, 0);
        var iconText = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 1, Dock = DockStyle.Fill };
        var links = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, Margin = new Padding(0, Dp(4), 0, 0) };
        foreach (var link in new[] { _pickIcon, _generatedIcon, _resetIcon, _saveIco })
        {
            link.Margin = new Padding(0, 0, Dp(16), 0);
            links.Controls.Add(link);
        }
        _saveIco.Margin = Padding.Empty;
        _tips.SetToolTip(_pickIcon, "Use your own image file");
        _tips.SetToolTip(_generatedIcon, "Make a letter tile from the site name");
        _tips.SetToolTip(_resetIcon, "Go back to the icon from the website");
        _tips.SetToolTip(_saveIco, "Save this icon as an .ico file");
        var iconTitle = BoldLabel("Icon preview");
        _iconStatus.AutoSize = true;
        _iconStatus.Margin = new Padding(0, Dp(4), 0, 0);
        iconText.Controls.Add(iconTitle, 0, 0);
        iconText.Controls.Add(_iconStatus, 0, 1);
        iconText.Controls.Add(links, 0, 2);
        icon.Controls.Add(iconText, 1, 0);
        Row(icon, Dp(16));

        // Save location row
        Row(BoldLabel("Save location"), Dp(16));
        var loc = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Dock = DockStyle.Fill };
        loc.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        loc.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _location.Dock = DockStyle.Fill;
        _location.Margin = new Padding(0, Dp(2), Dp(8), 0);
        _browse.AutoSize = true;
        _browse.MinimumSize = new Size(Dp(84), Dp(26));
        _browse.Margin = Padding.Empty;
        loc.Controls.Add(_location, 0, 0);
        loc.Controls.Add(_browse, 1, 0);
        Row(loc, Dp(4));

        // Options
        Row(_saveIcon, Dp(16), Dp(2));
        Row(_createShortcut, Dp(4), Dp(2));

        var openWith = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2 };
        openWith.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        openWith.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _openWithLabel.Anchor = AnchorStyles.Left;
        _openWithLabel.Margin = new Padding(0, 0, Dp(8), 0);
        _browserBox.Width = Dp(260);
        _browserBox.Margin = Padding.Empty;
        openWith.Controls.Add(_openWithLabel, 0, 0);
        openWith.Controls.Add(_browserBox, 1, 0);
        Row(openWith, Dp(6), Dp(24));
        Row(_appWindow, Dp(6), Dp(24));
        _browserNote.MaximumSize = new Size(Dp(ContentWidth - 42), 0);
        Row(_browserNote, Dp(2), Dp(42));

        // Action
        _create.Size = new Size(Dp(170), Dp(32));
        _create.Anchor = AnchorStyles.None;
        Row(_create, Dp(16));

        _status.Dock = DockStyle.Fill;
        _status.Height = Dp(20);
        Row(_status, Dp(12));

        _show.Anchor = AnchorStyles.None;
        Row(_show, Dp(4));

        Controls.Add(frame);
    }

    private Label BoldLabel(string text) =>
        new() { Text = text, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };

    private void ShowTab(int index)
    {
        if (index == HelpTab)
        {
            // Same size as the shortcut page, so the window doesn't jump when switching tabs.
            _helpPage.Size = _shortcutPage.Size;
            _helpPage.Visible = true;
            _shortcutPage.Visible = false;
            _helpPage.Activate();
        }
        else
        {
            _shortcutPage.Visible = true;
            _helpPage.Visible = false;
            _url.Focus();
        }
    }

    // ------------------------------------------------------------------ startup

    private void OnLoaded(object? sender, EventArgs e)
    {
        LoadBrowsers();
        ShowSaveFolder();

        // A URL passed on the command line wins, otherwise use the clipboard if it holds a link.
        var arg = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();
        var initial = arg ?? ReadClipboardText();
        if (initial != null && UrlHelper.TryNormalize(initial, out var uri))
        {
            _url.Text = uri.AbsoluteUri;
            _debounce.Stop();
            _ = LookupAsync();
        }
        else
        {
            UpdateUi();
        }

        _url.Focus();
        _url.SelectionStart = _url.TextLength;
    }

    private void LoadBrowsers()
    {
        var installed = BrowserService.FindInstalled();
        _defaultBrowser = BrowserService.FindDefault(installed);

        _browserBox.Items.Add(new BrowserChoice(null,
            _defaultBrowser is null ? "Default browser" : $"Default browser ({_defaultBrowser.Name})"));
        foreach (var b in installed)
            _browserBox.Items.Add(new BrowserChoice(b, b.Name));
        _browserBox.SelectedIndex = 0;
    }

    private static string? ReadClipboardText()
    {
        try { return Clipboard.ContainsText() ? Clipboard.GetText() : null; }
        catch { return null; } // clipboard can be locked by another process
    }

    // ------------------------------------------------------------------ drag & drop

    private void HookDrag(Control c)
    {
        c.AllowDrop = true;
        c.DragEnter += OnDragOver;
        c.DragOver += OnDragOver;
        c.DragLeave += OnDragLeave;
        c.DragDrop += OnDragDrop;
        foreach (Control child in c.Controls) HookDrag(child);
    }

    private void OnDragOver(object? sender, DragEventArgs e)
    {
        var ok = e.Data != null && UrlHelper.HasUrlData(e.Data);
        e.Effect = ok ? DragDropEffects.Copy : DragDropEffects.None;
        _url.BackColor = ok ? Color.AliceBlue : SystemColors.Window;
    }

    private void OnDragLeave(object? sender, EventArgs e)
    {
        _url.BackColor = SystemColors.Window;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        _url.BackColor = SystemColors.Window;
        _tabs.SelectedIndex = ShortcutTab;

        var text = e.Data != null ? UrlHelper.ExtractUrl(e.Data) : null;
        if (text != null && UrlHelper.TryNormalize(text, out var uri))
        {
            _nameEditedByUser = false;
            _url.Text = uri.AbsoluteUri;
            _debounce.Stop();
            _ = LookupAsync();
        }
        else
        {
            _status.Text = "That doesn't look like a web address.";
        }
    }

    // ------------------------------------------------------------------ text changes

    private void OnUrlChanged()
    {
        if (!_ready) return;

        _lookupCts?.Cancel();
        _lookupRunning = false;
        _debounce.Stop();
        _show.Visible = false;
        SetSourceIcon(null, "");

        if (UrlHelper.TryNormalize(_url.Text, out _))
        {
            _iconStatus.Text = "Looking up icon…";
            _debounce.Start();
        }
        else
        {
            SetIcon(null);
            _iconStatus.Text = "Paste or drop a link to begin";
            if (!_nameEditedByUser) SetName("");
        }
        UpdateUi();
    }

    private void OnNameChanged()
    {
        if (!_ready || _settingName) return;
        _nameEditedByUser = _name.TextLength > 0;
        UpdateUi();
    }

    private void SetName(string name)
    {
        _settingName = true;
        _name.Text = name;
        _settingName = false;
    }

    // ------------------------------------------------------------------ icon lookup

    private async Task LookupAsync()
    {
        if (!UrlHelper.TryNormalize(_url.Text, out var uri)) return;

        _lookupCts?.Cancel();
        var cts = _lookupCts = new CancellationTokenSource();
        _lookupRunning = true;
        _iconStatus.Text = "Looking up icon…";
        UpdateUi();

        PageInfo info;
        try
        {
            info = await _favicons.GetAsync(uri, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return; // superseded by a newer lookup or text change
        }
        catch (Exception)
        {
            info = new PageInfo(null, null, null);
        }

        if (cts.IsCancellationRequested)
        {
            info.Icon?.Dispose();
            return;
        }
        _lookupRunning = false;

        if (!_nameEditedByUser)
        {
            var host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host;
            SetName(string.IsNullOrWhiteSpace(info.Title) ? host : info.Title!);
        }

        if (info.Icon != null)
            SetSourceIcon(info.Icon, "Detected from website");
        else
            SetSourceIcon(IconTools.CreateFallback(uri.Host), "No icon found - using a generated one");
        UpdateUi();
    }

    private void SetIcon(Bitmap? icon)
    {
        var old = _icon;
        var oldPreview = _preview.Image;
        _icon = icon;
        // Shown at desktop icon size (48 px at 100%), scaled the same way as the .ico frames, rather than
        // stretched by the PictureBox - so the preview looks like the shortcut will.
        _preview.Image = icon == null ? null : IconTools.Render(icon, Dp(PreviewIconSize));
        old?.Dispose();
        oldPreview?.Dispose();
    }

    /// <summary>Shows the icon the website lookup produced and keeps a copy of it for Reset. Null forgets it.</summary>
    private void SetSourceIcon(Bitmap? icon, string status)
    {
        _sourceIcon?.Dispose();
        _sourceIcon = icon == null ? null : new Bitmap(icon);
        _sourceStatus = status;
        _iconIsSource = false;
        if (icon == null) return;

        SetIcon(icon);
        _iconStatus.Text = status;
        _iconIsSource = true;
    }

    private void PickIcon()
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Choose an icon",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.ico|All files|*.*",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        Bitmap? img = null;
        try { img = IconTools.TryDecode(File.ReadAllBytes(dlg.FileName)); } catch { }

        if (img == null)
        {
            MessageBox.Show(this, "That file couldn't be read as an image (SVG isn't supported).",
                "SaveLinkUtility", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _lookupCts?.Cancel();
        _lookupRunning = false;
        SetIcon(img);
        _iconIsSource = false;
        _iconStatus.Text = "Custom: " + Path.GetFileName(dlg.FileName);
        UpdateUi();
    }

    private void UseGeneratedIcon()
    {
        if (!UrlHelper.TryNormalize(_url.Text, out var uri)) return;
        _lookupCts?.Cancel();
        _lookupRunning = false;
        SetIcon(IconTools.CreateFallback(uri.Host));
        _iconIsSource = false;
        _iconStatus.Text = "Generated fallback icon";
        UpdateUi();
    }

    private void ResetIcon()
    {
        if (_sourceIcon != null)
        {
            SetIcon(new Bitmap(_sourceIcon));
            _iconIsSource = true;
            _iconStatus.Text = _sourceStatus;
            UpdateUi();
        }
        else
        {
            // The lookup was interrupted (e.g. a file was picked before it finished): look it up again.
            _ = LookupAsync();
        }
    }

    private void SaveIcoAs()
    {
        if (_icon == null) return;

        var host = UrlHelper.TryNormalize(_url.Text, out var uri) ? uri.Host : null;
        using var dlg = new SaveFileDialog
        {
            Title = "Save icon as",
            Filter = "Icon files (*.ico)|*.ico",
            DefaultExt = "ico",
            AddExtension = true,
            FileName = FileNames.Sanitize(_name.Text.Trim().Length > 0 ? _name.Text : host, "icon") + ".ico",
            OverwritePrompt = true,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            IconTools.WriteIco(_icon, dlg.FileName);
            _lastCreatedPath = dlg.FileName;
            _status.Text = "Saved " + Path.GetFileName(dlg.FileName);
            _show.Visible = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Couldn't save the icon:\n\n" + ex.Message,
                "SaveLinkUtility", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ------------------------------------------------------------------ save location

    private void BrowseForFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where to save the shortcut",
            UseDescriptionForTitle = true,
            InitialDirectory = _saveFolder,
            SelectedPath = _saveFolder,
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        _saveFolder = dlg.SelectedPath;
        ShowSaveFolder();
    }

    private void ShowSaveFolder()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        _location.Text = string.Equals(_saveFolder, desktop, StringComparison.OrdinalIgnoreCase)
            ? "Desktop"
            : _saveFolder;
    }

    // ------------------------------------------------------------------ create

    private BrowserChoice? Choice => _browserBox.SelectedItem as BrowserChoice;

    /// <summary>The browser the shortcut will actually start: the chosen one, or the current default.</summary>
    private BrowserInfo? EffectiveBrowser => Choice?.Browser ?? _defaultBrowser;

    private void UpdateUi()
    {
        if (!_ready) return;

        var validUrl = UrlHelper.TryNormalize(_url.Text, out _);
        var save = _saveIcon.Checked;
        var create = _createShortcut.Checked;
        var browser = EffectiveBrowser;
        var appMode = browser?.SupportsAppWindow == true;

        _openWithLabel.Enabled = create;
        _browserBox.Enabled = create;
        _appWindow.Enabled = create && appMode;
        if (!appMode && _appWindow.Checked) _appWindow.Checked = false;

        _browserNote.Text =
            browser != null && !appMode ? $"{browser.Name} has no app-window mode, so it opens in a normal tab."
            : _appWindow.Checked ? $"Opens in a {browser!.Name} window without tabs or address bar."
            : Choice?.Browser != null ? $"Always opens in {Choice.Browser.Name}, even if the default browser changes."
            : "Opens in whichever browser is the default when you click it.";

        _generatedIcon.Enabled = validUrl;
        _resetIcon.Enabled = validUrl && !_iconIsSource && !_lookupRunning;
        _saveIco.Enabled = _icon != null && !_lookupRunning;

        _create.Enabled = validUrl
            && !_lookupRunning
            && _icon != null
            && (save || create)
            && (!create || _name.Text.Trim().Length > 0);
    }

    private void CreateShortcut()
    {
        if (!UrlHelper.TryNormalize(_url.Text, out var uri) || _icon == null) return;

        try
        {
            string? iconPath = null;
            if (_saveIcon.Checked)
                iconPath = IconTools.SaveIco(_icon, ShortcutService.IconDirectory, uri.Host);

            string? shortcutPath = null;
            if (_createShortcut.Checked)
            {
                var name = _name.Text.Trim();
                var browser = EffectiveBrowser;
                shortcutPath = _appWindow.Checked && browser != null
                    ? ShortcutService.CreateBrowserShortcut(uri, name, _saveFolder, iconPath, browser, appWindow: true)
                    : Choice?.Browser != null
                        ? ShortcutService.CreateBrowserShortcut(uri, name, _saveFolder, iconPath, Choice.Browser, appWindow: false)
                        : ShortcutService.CreateUrlFile(uri, name, _saveFolder, iconPath);
            }

            _lastCreatedPath = shortcutPath ?? iconPath;
            _status.Text = shortcutPath != null
                ? "Created " + Path.GetFileName(shortcutPath)
                : "Saved icon " + Path.GetFileName(iconPath);
            _show.Visible = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Couldn't create the shortcut:\n\n" + ex.Message,
                "SaveLinkUtility", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowInFolder()
    {
        if (_lastCreatedPath == null || !File.Exists(_lastCreatedPath)) return;
        Process.Start("explorer.exe", $"/select,\"{_lastCreatedPath}\"");
    }
}
