using SimpleReplay.Forms;
using SimpleReplay.Models;
using SimpleReplay.Services;
using System.Drawing;
using System.Windows.Forms;

namespace SimpleReplay;

public sealed class TrayApplicationContext : ApplicationContext
{
    private readonly SettingsService _settingsService = new();
    private AppSettings _settings;
    private readonly OrchestratorService _orchestrator;
    private readonly HotkeyService _hotkey = new();
    private readonly NotifyIcon _trayIcon;

    public TrayApplicationContext()
    {
        _settings = _settingsService.Load();
        _orchestrator = new OrchestratorService(_settings);

        _trayIcon = new NotifyIcon
        {
            Icon = BuildTrayIcon(),
            Text = "Simple Replay",
            Visible = true,
            ContextMenuStrip = BuildContextMenu(),
        };

        RegisterHotkey();
        _orchestrator.StartCapture();
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Save Replay Now", null, (_, _) => _orchestrator.TriggerSave());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Settings…", null, OpenSettings);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, Exit);
        return menu;
    }

    private void OpenSettings(object? sender, EventArgs e)
    {
        using var form = new SettingsForm(_settings);
        if (form.ShowDialog() != DialogResult.OK)
            return;

        _settings = form.Settings;
        _settingsService.Save(_settings);

        RegisterHotkey();
        _orchestrator.ApplySettings(_settings);
    }

    private void RegisterHotkey()
    {
        try
        {
            _hotkey.Register(_settings.Hotkey, _orchestrator.TriggerSave);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Could not register hotkey '{_settings.Hotkey}':\n{ex.Message}\n\nOpen Settings to choose a different hotkey.",
                "Simple Replay", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Exit(object? sender, EventArgs e)
    {
        _trayIcon.Visible = false;
        _hotkey.Dispose();
        _orchestrator.Dispose();
        Application.Exit();
    }

    private static Icon BuildTrayIcon()
    {
        using var bmp = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bmp);
        g.Clear(Color.DodgerBlue);
        g.FillEllipse(Brushes.White, 2, 2, 12, 12);
        g.FillEllipse(new SolidBrush(Color.DodgerBlue), 5, 5, 6, 6);
        return Icon.FromHandle(bmp.GetHicon());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _hotkey.Dispose();
            _orchestrator.Dispose();
        }
        base.Dispose(disposing);
    }
}
