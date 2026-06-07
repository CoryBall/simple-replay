using System.Collections.Concurrent;

namespace SimpleReplay.Services;

public sealed class FrameBuffer
{
    private readonly ConcurrentQueue<(long TimestampMs, byte[] JpegBytes)> _queue = new();
    private readonly Func<long> _clock;
    private long _bufferMs = (long)TimeSpan.FromMinutes(5).TotalMilliseconds;

    public FrameBuffer(Func<long>? clock = null)
    {
        _clock = clock ?? (() => Environment.TickCount64);
    }

    public void SetBufferDuration(int minutes)
    {
        _bufferMs = (long)TimeSpan.FromMinutes(minutes).TotalMilliseconds;
    }

    public void Push(byte[] jpegBytes)
    {
        var now = _clock();
        _queue.Enqueue((now, jpegBytes));
        Evict(now);
    }

    private void Evict(long now)
    {
        var cutoff = now - _bufferMs;
        while (_queue.TryPeek(out var oldest) && oldest.TimestampMs < cutoff)
            _queue.TryDequeue(out _);
    }

    public List<byte[]> Snapshot()
    {
        return _queue.ToArray().Select(f => f.JpegBytes).ToList();
    }

    public int Count => _queue.Count;

    public long EstimatedRamBytes =>
        _queue.ToArray().Sum(f => (long)f.JpegBytes.Length);
}
