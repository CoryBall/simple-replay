using SimpleReplay.Services;

namespace SimpleReplay.Tests;

public sealed class EncoderServiceTests
{
    // ── ResolveCodec ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("h264", "none",  "libx264")]
    [InlineData("h264", "nvenc", "h264_nvenc")]
    [InlineData("h264", "qsv",   "h264_qsv")]
    [InlineData("h264", "amf",   "h264_amf")]
    [InlineData("h265", "none",  "libx265")]
    [InlineData("h265", "nvenc", "hevc_nvenc")]
    [InlineData("h265", "qsv",   "hevc_qsv")]
    [InlineData("h265", "amf",   "hevc_amf")]
    public void ResolveCodec_ReturnsCorrectCodec(string codec, string hwAccel, string expected)
    {
        var result = EncoderService.ResolveCodec(codec, hwAccel);

        result.Should().Be(expected);
    }

    // ── BuildArgs ───────────────────────────────────────────────────────────────

    [Fact]
    public void BuildArgs_SoftwareCodec_IncludesPresetAndCrf()
    {
        var args = EncoderService.BuildArgs(30, "libx264", "h264", "ultrafast", 23, "output.mp4");

        args.Should().Contain("-preset ultrafast");
        args.Should().Contain("-crf 23");
    }

    [Fact]
    public void BuildArgs_NvencCodec_IncludesCq_NotPreset()
    {
        var args = EncoderService.BuildArgs(30, "h264_nvenc", "h264", "ultrafast", 23, "output.mp4");

        args.Should().Contain("-cq 23");
        args.Should().NotContain("-preset ultrafast");
        args.Should().NotContain("-crf");
    }

    [Fact]
    public void BuildArgs_AlwaysIncludesMjpegInput()
    {
        var args = EncoderService.BuildArgs(30, "libx264", "h264", "ultrafast", 23, "output.mp4");

        args.Should().Contain("-f image2pipe");
        args.Should().Contain("-vcodec mjpeg");
    }

    [Fact]
    public void BuildArgs_AlwaysIncludesFaststart()
    {
        var args = EncoderService.BuildArgs(30, "libx264", "h264", "ultrafast", 23, "output.mp4");

        args.Should().Contain("-movflags +faststart");
    }

    [Fact]
    public void BuildArgs_IncludesOutputPath()
    {
        var args = EncoderService.BuildArgs(30, "libx264", "h264", "ultrafast", 23, @"C:\Videos\replay.mp4");

        args.Should().Contain(@"C:\Videos\replay.mp4");
    }

    [Fact]
    public void BuildArgs_IncludesFramerate()
    {
        var args = EncoderService.BuildArgs(30, "libx264", "h264", "ultrafast", 23, "output.mp4");

        args.Should().Contain("-r 30");
    }
}
