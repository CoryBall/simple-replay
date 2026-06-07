using SimpleReplay.Models;

namespace SimpleReplay.Services;

public sealed class OrchestratorService : IDisposable
{
    private readonly FrameBuffer _buffer = new();
    private readonly EncoderService _encoder = new();
    private AppSettings _settings;
    private CaptureService? _capture;
    private volatile int _saving; // 0 = idle, 1 = saving

    public OrchestratorService(AppSettings settings)
    {
        _settings = settings;
    }

    public void StartCapture()
    {
        StopCapture();
        _buffer.SetBufferDuration(_settings.BufferMinutes);
        _capture = new CaptureService(
            _buffer, _settings.Fps, _settings.Width, _settings.Height, _settings.BufferJpegQuality);
        _capture.Start();
    }

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        StartCapture();
    }

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
            var frames = _buffer.Snapshot();
            if (frames.Count == 0)
            {
                NotificationService.ShowError("No frames in buffer yet — wait a moment and try again.");
                return;
            }

            NotificationService.ShowSaving(frames.Count, _settings.Fps);

            Directory.CreateDirectory(_settings.OutputDirectory);
            var filename = $"replay_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
            var outputPath = Path.Combine(_settings.OutputDirectory, filename);

            await _encoder.EncodeAsync(
                frames, _settings.Fps, outputPath,
                _settings.Codec, _settings.Preset, _settings.Crf, _settings.HwAccel);

            var size = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0;
            NotificationService.ShowSaved(outputPath, size, frames.Count, _settings.Fps);
        }
        catch (Exception ex)
        {
            NotificationService.ShowError(ex.Message);
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
