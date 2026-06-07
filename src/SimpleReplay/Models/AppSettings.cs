namespace SimpleReplay.Models;

public class AppSettings
{
    public string Hotkey { get; set; } = "Ctrl+Shift+F9";
    public int BufferMinutes { get; set; } = 5;
    public int Fps { get; set; } = 30;
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public string OutputDirectory { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "SimpleReplay");
    public string Codec { get; set; } = "h264";
    public string Preset { get; set; } = "ultrafast";
    public int Crf { get; set; } = 23;
    public string HwAccel { get; set; } = "none";
    public int BufferJpegQuality { get; set; } = 55;
}
