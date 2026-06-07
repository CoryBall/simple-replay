using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;

namespace SimpleReplay.Services;

public sealed class CaptureService : IDisposable
{
    private readonly FrameBuffer _buffer;
    private readonly int _fps;
    private readonly int _width;
    private readonly int _height;
    private readonly ImageCodecInfo _jpegCodec;
    private readonly EncoderParameters _encoderParams;
    private Thread? _thread;
    private volatile bool _running;

    public CaptureService(FrameBuffer buffer, int fps, int width, int height, int jpegQuality)
    {
        _buffer = buffer;
        _fps = fps;
        _width = width;
        _height = height;
        _jpegCodec = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        _encoderParams = new EncoderParameters(1);
        _encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, (long)jpegQuality);
    }

    public void Start()
    {
        _running = true;
        _thread = new Thread(CaptureLoop) { IsBackground = true, Name = "CaptureThread", Priority = ThreadPriority.AboveNormal };
        _thread.Start();
    }

    public void Stop()
    {
        _running = false;
        _thread?.Join(3000);
        _thread = null;
    }

    private void CaptureLoop()
    {
        var intervalMs = 1000.0 / _fps;
        var srcBounds = Screen.PrimaryScreen!.Bounds;

        using var srcBitmap = new Bitmap(srcBounds.Width, srcBounds.Height, PixelFormat.Format32bppArgb);
        using var captureGraphics = Graphics.FromImage(srcBitmap);

        using var dstBitmap = new Bitmap(_width, _height, PixelFormat.Format24bppRgb);
        using var dstGraphics = Graphics.FromImage(dstBitmap);
        dstGraphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;

        while (_running)
        {
            var t = Stopwatch.GetTimestamp();

            try
            {
                captureGraphics.CopyFromScreen(srcBounds.Location, Point.Empty, srcBounds.Size);
                dstGraphics.DrawImage(srcBitmap, 0, 0, _width, _height);

                using var ms = new MemoryStream(32 * 1024);
                dstBitmap.Save(ms, _jpegCodec, _encoderParams);
                _buffer.Push(ms.ToArray());
            }
            catch
            {
                // swallow per-frame errors (e.g. during screen lock/unlock)
            }

            var elapsedMs = Stopwatch.GetElapsedTime(t).TotalMilliseconds;
            var sleepMs = intervalMs - elapsedMs;
            if (sleepMs > 1)
                Thread.Sleep((int)sleepMs);
        }
    }

    public void Dispose()
    {
        Stop();
        _encoderParams.Dispose();
    }
}
