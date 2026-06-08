using System.IO.Compression;

namespace SimpleReplay.Services;

public sealed class FfmpegBootstrapper
{
    private const string DownloadUrl =
        "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";

    public static readonly string FfmpegExePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SimpleReplay", "tools", "ffmpeg.exe");

    public bool IsInstalled => File.Exists(FfmpegExePath);

    public async Task DownloadAsync(
        IProgress<(string Status, int Percent)> progress,
        CancellationToken ct = default)
    {
        var toolsDir = Path.GetDirectoryName(FfmpegExePath)!;
        Directory.CreateDirectory(toolsDir);
        var zipPath = Path.Combine(toolsDir, "ffmpeg_download.zip");

        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("SimpleReplay/1.0");
            http.Timeout = TimeSpan.FromMinutes(10);

            using var response = await http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;
            await using var download = await response.Content.ReadAsStreamAsync(ct);

            await using (var file = File.Create(zipPath))
            {
                var buffer = new byte[81920];
                long downloaded = 0;
                int read;
                while ((read = await download.ReadAsync(buffer, ct)) > 0)
                {
                    await file.WriteAsync(buffer.AsMemory(0, read), ct);
                    downloaded += read;
                    var pct = totalBytes > 0 ? (int)(downloaded * 80L / totalBytes) : 0;
                    var mb = downloaded / (1024.0 * 1024.0);
                    var total = totalBytes > 0 ? $"/ {totalBytes / (1024.0 * 1024.0):F0} MB" : "";
                    progress.Report(($"Downloading ffmpeg… {mb:F0} MB {total}", pct));
                }
            }

            progress.Report(("Extracting…", 85));
            using (var zip = ZipFile.OpenRead(zipPath))
            {
                var entry = zip.Entries.FirstOrDefault(e =>
                    e.Name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
                    ?? throw new InvalidOperationException("ffmpeg.exe not found in downloaded archive.");

                progress.Report(("Installing…", 95));
                if (File.Exists(FfmpegExePath)) File.Delete(FfmpegExePath);
                entry.ExtractToFile(FfmpegExePath, overwrite: true);
            }

            progress.Report(("Done!", 100));
        }
        finally
        {
            if (File.Exists(zipPath))
                try { File.Delete(zipPath); } catch { }
        }
    }
}
