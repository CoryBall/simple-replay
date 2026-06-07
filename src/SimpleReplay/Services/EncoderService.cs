using System.Diagnostics;
using System.Text;

namespace SimpleReplay.Services;

public sealed class EncoderService
{
    // Installed by install.ps1 → %APPDATA%\SimpleReplay\tools\ffmpeg.exe
    // Dev fallback: tools\ folder next to the exe
    private readonly string _ffmpegPath = LocateFfmpeg();

    private static string LocateFfmpeg()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "SimpleReplay", "tools", "ffmpeg.exe");

        if (File.Exists(appData)) return appData;

        return Path.Combine(AppContext.BaseDirectory, "tools", "ffmpeg.exe");
    }

    public bool FfmpegAvailable => File.Exists(_ffmpegPath);

    public async Task<bool> EncodeAsync(
        List<byte[]> frames,
        int fps,
        string outputPath,
        string codec,
        string preset,
        int crf,
        string hwAccel,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        if (!FfmpegAvailable)
            throw new FileNotFoundException(
                "ffmpeg.exe was not found. Please run install.ps1 to complete setup.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        var videoCodec = ResolveCodec(codec, hwAccel);
        var args = BuildArgs(fps, videoCodec, codec, preset, crf, outputPath);

        var stderr = new StringBuilder();
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardError = true,
            }
        };

        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        process.Start();
        process.BeginErrorReadLine();

        var stdin = process.StandardInput.BaseStream;
        int written = 0;

        try
        {
            foreach (var frame in frames)
            {
                ct.ThrowIfCancellationRequested();
                await stdin.WriteAsync(frame, ct);
                written++;
                progress?.Report(written * 100 / frames.Count);
            }
        }
        catch (OperationCanceledException)
        {
            process.Kill();
            throw;
        }
        finally
        {
            stdin.Close();
        }

        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            // If hardware accel failed, retry with software fallback
            if (hwAccel != "none")
                return await EncodeAsync(frames, fps, outputPath, codec, preset, crf, "none", progress, ct);

            throw new InvalidOperationException($"ffmpeg exited with code {process.ExitCode}:\n{stderr}");
        }

        return true;
    }

    private static string ResolveCodec(string codec, string hwAccel) => (codec, hwAccel) switch
    {
        ("h265", "nvenc") => "hevc_nvenc",
        ("h265", "qsv")   => "hevc_qsv",
        ("h265", "amf")   => "hevc_amf",
        ("h265", _)       => "libx265",
        ("h264", "nvenc") => "h264_nvenc",
        ("h264", "qsv")   => "h264_qsv",
        ("h264", "amf")   => "h264_amf",
        _                 => "libx264",
    };

    private static string BuildArgs(int fps, string videoCodec, string baseCodec, string preset, int crf, string outputPath)
    {
        var sb = new StringBuilder();

        // Read JPEG frames from stdin as mjpeg stream
        sb.Append($"-r {fps} -f image2pipe -vcodec mjpeg -i pipe:0 ");

        sb.Append($"-c:v {videoCodec} ");

        if (videoCodec is "libx264" or "libx265")
        {
            sb.Append($"-preset {preset} -crf {crf} ");
        }
        else if (videoCodec.EndsWith("_nvenc"))
        {
            sb.Append($"-preset p4 -cq {crf} ");
        }
        else if (videoCodec.EndsWith("_qsv"))
        {
            sb.Append($"-preset medium -global_quality {crf} ");
        }
        else if (videoCodec.EndsWith("_amf"))
        {
            sb.Append($"-quality balanced -qp_i {crf} -qp_p {crf} ");
        }

        sb.Append("-pix_fmt yuv420p ");
        sb.Append("-movflags +faststart ");
        sb.Append($"-y \"{outputPath}\"");

        return sb.ToString();
    }
}
