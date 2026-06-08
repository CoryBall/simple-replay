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

        _trayIcon = new NotifyIcon
        {
            Icon = BuildTrayIcon(),
            Text = "Simple Replay",
            Visible = true,
            ContextMenuStrip = BuildContextMenu(),
        };

        var notifications = new NotificationService(_trayIcon);
        _orchestrator = new OrchestratorService(_settings, notifications);

        // On first launch after install, download ffmpeg before starting capture
        var bootstrapper = new FfmpegBootstrapper();
        if (!bootstrapper.IsInstalled)
        {
            using var dlg = new FfmpegDownloadForm();
            if (dlg.ShowDialog() != DialogResult.OK)
            {
                _trayIcon.Visible = false;
                Application.Exit();
                return;
            }
        }

        RegisterHotkey();
        _orchestrator.StartCapture();

        // Check for updates in the background — downloaded update applies on next launch
        _ = new UpdateService().CheckAndDownloadAsync();
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

        var next = form.Settings;

        bool captureChanged =
            next.Fps            != _settings.Fps            ||
            next.Width          != _settings.Width          ||
            next.Height         != _settings.Height         ||
            next.BufferMinutes  != _settings.BufferMinutes  ||
            next.BufferJpegQuality != _settings.BufferJpegQuality;

        if (captureChanged && _orchestrator.BufferFrameCount > 0)
        {
            var frames = _orchestrator.BufferFrameCount;
            var secs   = _settings.Fps > 0 ? frames / _settings.Fps : 0;
            var dur    = secs >= 60 ? $"{secs / 60}m {secs % 60}s" : $"{secs}s";

            var confirm = MessageBox.Show(
                $"Changing capture settings will clear the current buffer ({dur} of footage).\n\nContinue?",
                "Simple Replay", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes)
                return;
        }

        _settings = next;
        _settingsService.Save(_settings);
        RegisterHotkey();

        if (captureChanged)
            _orchestrator.RestartCapture(_settings);
        else
            _orchestrator.UpdateEncodeSettings(_settings);
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
