using SimpleReplay.Models;

namespace SimpleReplay.Services;

public sealed class OrchestratorService : IDisposable
{
    private readonly FrameBuffer _buffer = new();
    private readonly EncoderService _encoder = new();
    private readonly NotificationService _notifications;
    private AppSettings _settings;
    private CaptureService? _capture;
    private volatile int _saving; // 0 = idle, 1 = saving

    public OrchestratorService(AppSettings settings, NotificationService notifications)
    {
        _settings = settings;
        _notifications = notifications;
    }

    public void StartCapture()
    {
        StopCapture();
        _buffer.Clear();
        _buffer.SetBufferDuration(_settings.BufferMinutes);
        _capture = new CaptureService(
            _buffer, _settings.Fps, _settings.Width, _settings.Height, _settings.BufferJpegQuality);
        _capture.Start();
    }

    // Call when capture-affecting settings changed (fps, resolution, quality, buffer length)
    public void RestartCapture(AppSettings settings)
    {
        _settings = settings;
        StartCapture();
    }

    // Call when only encode-time settings changed (codec, preset, crf, output dir, hotkey)
    public void UpdateEncodeSettings(AppSettings settings)
    {
        _settings = settings;
    }

    public int BufferFrameCount => _buffer.Count;

    public void TriggerSave()
    {
        if (Interlocked.CompareExchange(ref _saving, 1, 0) != 0)
            return; // already saving

        Task.Run(SaveAsync);
    }

    private async Task SaveAsync()
    {
        try
        {
            var (frames, actualFps) = _buffer.SnapshotWithFps(_settings.Fps);
            if (frames.Count == 0)
            {
                _notifications.ShowError("No frames in buffer yet — wait a moment and try again.");
                return;
            }

            _notifications.ShowSaving(frames.Count, _settings.Fps);

            Directory.CreateDirectory(_settings.OutputDirectory);
            var filename = $"replay_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
            var outputPath = Path.Combine(_settings.OutputDirectory, filename);

            await _encoder.EncodeAsync(
                frames, _settings.Fps, outputPath,
                _settings.Codec, _settings.Preset, _settings.Crf, _settings.HwAccel,
                inputFps: actualFps);

            var size = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
            _notifications.ShowSaved(outputPath, size, frames.Count, _settings.Fps);
        }
        catch (Exception ex)
        {
            _notifications.ShowError(ex.Message);
        }
        finally
        {
            Interlocked.Exchange(ref _saving, 0);
        }
    }

    private void StopCapture()
    {
        _capture?.Stop();
        _capture?.Dispose();
        _capture = null;
    }

    public void Dispose() => StopCapture();
}
