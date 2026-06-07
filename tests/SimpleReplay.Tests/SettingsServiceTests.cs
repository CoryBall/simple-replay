using SimpleReplay.Models;
using SimpleReplay.Services;

namespace SimpleReplay.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _tempPath = Path.Combine(Path.GetTempPath(), $"SimpleReplayTests_{Guid.NewGuid()}.json");

    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var service = new SettingsService(_tempPath);

        var settings = service.Load();

        settings.Hotkey.Should().Be("Ctrl+Shift+F9");
        settings.BufferMinutes.Should().Be(5);
        settings.Fps.Should().Be(30);
        settings.Width.Should().Be(1280);
        settings.Height.Should().Be(720);
        settings.Codec.Should().Be("h264");
        settings.Preset.Should().Be("ultrafast");
        settings.Crf.Should().Be(23);
        settings.HwAccel.Should().Be("none");
        settings.BufferJpegQuality.Should().Be(55);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsAllFields()
    {
        var service = new SettingsService(_tempPath);
        var original = new AppSettings
        {
            Hotkey = "Alt+F12",
            BufferMinutes = 10,
            Fps = 15,
            Width = 1920,
            Height = 1080,
            OutputDirectory = @"C:\Videos\Test",
            Codec = "h265",
            Preset = "fast",
            Crf = 28,
            HwAccel = "nvenc",
            BufferJpegQuality = 40,
        };

        service.Save(original);
        var loaded = service.Load();

        loaded.Hotkey.Should().Be(original.Hotkey);
        loaded.BufferMinutes.Should().Be(original.BufferMinutes);
        loaded.Fps.Should().Be(original.Fps);
        loaded.Width.Should().Be(original.Width);
        loaded.Height.Should().Be(original.Height);
        loaded.OutputDirectory.Should().Be(original.OutputDirectory);
        loaded.Codec.Should().Be(original.Codec);
        loaded.Preset.Should().Be(original.Preset);
        loaded.Crf.Should().Be(original.Crf);
        loaded.HwAccel.Should().Be(original.HwAccel);
        loaded.BufferJpegQuality.Should().Be(original.BufferJpegQuality);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileIsCorruptJson()
    {
        File.WriteAllText(_tempPath, "this is not valid json {{{");
        var service = new SettingsService(_tempPath);

        var settings = service.Load();

        settings.Should().NotBeNull();
        settings.Fps.Should().Be(30);
    }

    [Fact]
    public void Save_CreatesFileAtPath()
    {
        var service = new SettingsService(_tempPath);

        service.Save(new AppSettings());

        File.Exists(_tempPath).Should().BeTrue();
    }

    public void Dispose()
    {
        if (File.Exists(_tempPath))
            File.Delete(_tempPath);
    }
}
