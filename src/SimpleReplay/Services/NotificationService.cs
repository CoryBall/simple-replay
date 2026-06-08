using System.Windows.Forms;

namespace SimpleReplay.Services;

public sealed class NotificationService
{
    private readonly NotifyIcon _trayIcon;
    private readonly SynchronizationContext _sync;

    public NotificationService(NotifyIcon trayIcon)
    {
        _trayIcon = trayIcon;
        _sync = SynchronizationContext.Current ?? new SynchronizationContext();
    }

    public void ShowSaving(int frameCount, int fps)
    {
        var durationSec = fps > 0 ? frameCount / fps : 0;
        Show("Saving replay…", $"Encoding {durationSec}s of footage.", ToolTipIcon.Info);
    }

    public void ShowSaved(string filePath, long fileSizeBytes, int frameCount, int fps)
    {
        var durationSec = fps > 0 ? frameCount / fps : 0;
        var sizeMb = fileSizeBytes / (1024.0 * 1024.0);
        Show("Replay saved!", $"{durationSec}s · {sizeMb:F1} MB → {Path.GetFileName(filePath)}", ToolTipIcon.Info);
    }

    public void ShowError(string message)
    {
        Show("Simple Replay — Error", message, ToolTipIcon.Error);
    }

    private void Show(string title, string body, ToolTipIcon icon)
    {
        _sync.Post(_ =>
        {
            try { _trayIcon.ShowBalloonTip(4000, title, body, icon); }
            catch { }
        }, null);
    }
}
