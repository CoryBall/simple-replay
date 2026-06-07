using SimpleReplay.Services;

namespace SimpleReplay.Tests;

public sealed class FrameBufferTests
{
    private static byte[] Frame(byte value = 0) => [value];

    [Fact]
    public void Push_IncreasesCount()
    {
        var buffer = new FrameBuffer();
        buffer.SetBufferDuration(5);

        buffer.Push(Frame());

        buffer.Count.Should().Be(1);
    }

    [Fact]
    public void Snapshot_ReturnsAllPushedFrames()
    {
        var buffer = new FrameBuffer();
        buffer.SetBufferDuration(5);

        buffer.Push(Frame(1));
        buffer.Push(Frame(2));
        buffer.Push(Frame(3));

        buffer.Snapshot().Should().HaveCount(3);
    }

    [Fact]
    public void Snapshot_ReturnsEmptyList_WhenNoFramesPushed()
    {
        var buffer = new FrameBuffer();
        buffer.SetBufferDuration(5);

        buffer.Snapshot().Should().BeEmpty();
    }

    [Fact]
    public void Snapshot_ReturnsFramesInOrder()
    {
        var buffer = new FrameBuffer();
        buffer.SetBufferDuration(5);

        buffer.Push(Frame(10));
        buffer.Push(Frame(20));
        buffer.Push(Frame(30));

        var frames = buffer.Snapshot();
        frames[0][0].Should().Be(10);
        frames[1][0].Should().Be(20);
        frames[2][0].Should().Be(30);
    }

    [Fact]
    public void Eviction_RemovesFramesOlderThanBufferWindow()
    {
        long fakeTime = 0;
        var buffer = new FrameBuffer(clock: () => fakeTime);
        buffer.SetBufferDuration(1); // 1 minute = 60 000 ms

        // Push a frame at t=0
        fakeTime = 0;
        buffer.Push(Frame(1));

        // Advance clock past the buffer window and push another frame —
        // the eviction check runs on Push, so the old frame should be removed
        fakeTime = 61_000;
        buffer.Push(Frame(2));

        var snapshot = buffer.Snapshot();
        snapshot.Should().HaveCount(1);
        snapshot[0][0].Should().Be(2);
    }

    [Fact]
    public void Eviction_KeepsFramesWithinBufferWindow()
    {
        long fakeTime = 0;
        var buffer = new FrameBuffer(clock: () => fakeTime);
        buffer.SetBufferDuration(1);

        fakeTime = 0;
        buffer.Push(Frame(1));

        fakeTime = 30_000; // 30s — still inside the 60s window
        buffer.Push(Frame(2));

        buffer.Snapshot().Should().HaveCount(2);
    }

    [Fact]
    public void EstimatedRamBytes_ReflectsTotalFrameSize()
    {
        var buffer = new FrameBuffer();
        buffer.SetBufferDuration(5);

        buffer.Push(new byte[100]);
        buffer.Push(new byte[200]);

        buffer.EstimatedRamBytes.Should().Be(300);
    }
}
