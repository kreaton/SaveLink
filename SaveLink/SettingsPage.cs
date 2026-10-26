using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using SaveLink.Services;

namespace SaveLink;

/// <summary>
/// The Settings tab: preferred browser and the folder saved icons go to. Every change is saved at once
/// and announced through <see cref="SettingsChanged"/>, so the Shortcut tab can follow along.
/// </summary>
public sealed class SettingsPage : Panel
{
    private readonly AppSettings _settings;
    private readonly ComboBox _browserBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _appWindow = new() { Text = "Open in its own app window by default", AutoSize = true };
    private readonly Label _browserNote = new() { AutoSize = true, ForeColor = SystemColors.GrayText };
    private readonly TextBox _iconFolder = new() { ReadOnly = true };
    private readonly Button _browse = new() { Text = "Browse…" };
    private readonly LinkLabel _openFolder = new() { Text = "Open folder", AutoSize = true };
    private readonly LinkLabel _defaultFolder = new() { Text = "Use default", AutoSize = true };
    private BrowserInfo? _defaultBrowser;
    private bool _loading;

    public event EventHandler? SettingsChanged;

    public SettingsPage(AppSettings settings, Font font, int contentWidth)
    {
        _settings = settings;
        Font = font;
        Build(contentWidth);

        _browserBox.SelectedIndexChanged += (_, _) => OnBrowserChanged();
        _appWindow.CheckedChanged += (_, _) => OnBrowserChanged();
        _browse.Click += (_, _) => BrowseForIconFolder();
        _openFolder.LinkClicked += (_, _) => OpenIconFolder();
        _defaultFolder.LinkClicked += (_, _) => SetIconFolder(null);
        ShowIconFolder();
    }

    private int Dp(int logical) => LogicalToDeviceUnits(logical);

    private void Build(int contentWidth)
    {
        int pad = Dp(16);
        var root = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(pad, Dp(14), pad, Dp(12)),
            Margin = Padding.Empty,
            Location = Point.Empty,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, Dp(contentWidth)));

        void Row(Control c, int topGap = 0, int left = 0)
        {
            c.Margin = new Padding(left, topGap, 0, 0);
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.Controls.Add(c, 0, root.RowCount++);
        }
        Label Note(string text) =>
            new() { Text = text, AutoSize = true, ForeColor = SystemColors.GrayText, MaximumSize = new Size(Dp(contentWidth), 0) };

        // Browser
        Row(BoldLabel("Browser"));
        Row(Note("Chosen on the Shortcut tab when the program starts. You can still pick another browser there for a single shortcut."), Dp(4));
        _browserBox.Width = Dp(260);
        Row(_browserBox, Dp(8));
        Row(_appWindow, Dp(8), Dp(2));
        _browserNote.MaximumSize = new Size(Dp(contentWidth - 20), 0);
        Row(_browserNote, Dp(2), Dp(20));

        // Icon folder
        Row(BoldLabel("Icon folder"), Dp(22));
        Row(Note("\"Save icon locally\" stores icons here, and the shortcuts point at these files, so keep them. "
               + "Changing the folder doesn't move icons that are already saved."), Dp(4));
        var folder = new TableLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, ColumnCount = 2, Dock = DockStyle.Fill };
        folder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folder.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _iconFolder.Dock = DockStyle.Fill;
        _iconFolder.Margin = new Padding(0, Dp(2), Dp(8), 0);
        _browse.AutoSize = true;
        _browse.MinimumSize = new Size(Dp(84), Dp(26));
        _browse.Margin = Padding.Empty;
        folder.Controls.Add(_iconFolder, 0, 0);
        folder.Controls.Add(_browse, 1, 0);
        Row(folder, Dp(8));

        var links = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false };
        _openFolder.Margin = new Padding(0, 0, Dp(16), 0);
        _defaultFolder.Margin = Padding.Empty;
        links.Controls.Add(_openFolder);
        links.Controls.Add(_defaultFolder);
        Row(links, Dp(6));

        Controls.Add(root);
    }

    private Label BoldLabel(string text) =>
        new() { Text = text, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };

    // ------------------------------------------------------------------ browser

    /// <summary>Fills the browser list (found once at startup) and selects the saved choice.</summary>
    public void SetBrowsers(IReadOnlyList<BrowserChoice> choices, BrowserInfo? defaultBrowser)
    {
        _defaultBrowser = defaultBrowser;
        _loading = true;
        _browserBox.Items.Clear();
        foreach (var c in choices) _browserBox.Items.Add(c);
        _browserBox.SelectedIndex = BrowserChoice.IndexOf(choices, _settings.BrowserExe);
        _appWindow.Checked = _settings.AppWindow;
        _loading = false;
        UpdateBrowserNote();
    }

    // The system default browser, needed to tell whether "Default browser" can do app windows.
    private BrowserInfo? EffectiveBrowser => (_browserBox.SelectedItem as BrowserChoice)?.Browser ?? _defaultBrowser;

    private void OnBrowserChanged()
    {
        if (_loading) return;
        UpdateBrowserNote();
        _settings.BrowserExe = (_browserBox.SelectedItem as BrowserChoice)?.Browser?.ExePath;
        _settings.AppWindow = _appWindow.Checked;
        Save();
    }

    private void UpdateBrowserNote()
    {
        var browser = EffectiveBrowser;
        var appMode = browser?.SupportsAppWindow == true;
        _appWindow.Enabled = appMode;
        _browserNote.Text = browser != null && !appMode ? $"{browser.Name} has no app-window mode." : "";
        _browserNote.Visible = _browserNote.Text.Length > 0;
    }

    // ------------------------------------------------------------------ icon folder

    private void ShowIconFolder()
    {
        _iconFolder.Text = _settings.EffectiveIconFolder;
        _defaultFolder.Enabled = !string.IsNullOrWhiteSpace(_settings.IconFolder);
    }

    private void SetIconFolder(string? path)
    {
        _settings.IconFolder = string.Equals(path, AppSettings.DefaultIconFolder, StringComparison.OrdinalIgnoreCase) ? null : path;
        ShowIconFolder();
        Save();
    }

    private void BrowseForIconFolder()
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Choose where saved icons go",
            UseDescriptionForTitle = true,
            InitialDirectory = Directory.Exists(_settings.EffectiveIconFolder) ? _settings.EffectiveIconFolder : "",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK) SetIconFolder(dlg.SelectedPath);
    }

    private void OpenIconFolder()
    {
        var folder = _settings.EffectiveIconFolder;
        try
        {
            Directory.CreateDirectory(folder);
            Process.Start("explorer.exe", $"\"{folder}\"");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Couldn't open the folder:\n\n" + ex.Message,
                "SaveLink", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Save()
    {
        _settings.Save();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }
}
