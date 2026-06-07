using Microsoft.Toolkit.Uwp.Notifications;

namespace SimpleReplay.Services;

public static class NotificationService
{
    public static void ShowSaving(int frameCount, int fps)
    {
        var durationSec = fps > 0 ? frameCount / fps : 0;
        TryShow("Simple Replay", $"Saving {durationSec}s replay...");
    }

    public static void ShowSaved(string filePath, long fileSizeBytes, int frameCount, int fps)
    {
        var durationSec = fps > 0 ? frameCount / fps : 0;
        var sizeMb = fileSizeBytes / (1024.0 * 1024.0);
        TryShow("Simple Replay — Saved!", $"{durationSec}s · {sizeMb:F1} MB → {Path.GetFileName(filePath)}");
    }

    public static void ShowError(string message)
    {
        TryShow("Simple Replay — Error", message);
    }

    private static void TryShow(string title, string body)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(body)
                .Show();
        }
        catch
        {
            // Toast notifications may be unavailable (e.g. Focus Assist, policy)
        }
    }
}
