using System.Buffers.Binary;
using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class ReplayHeaderTests
{
    [Fact]
    public void InspectsLiveAndCompletedSyntheticHeaders()
    {
        var live = HeaderBytes.Build(isLive: true, isEncrypted: true);
        var done = HeaderBytes.Build(isLive: false, isEncrypted: true);
        Assert.True(ReplayHeader.TryInspect(new MemoryStream(live), out var liveInfo));
        Assert.True(liveInfo.IsLive);
        Assert.True(liveInfo.IsEncrypted);
        Assert.True(ReplayHeader.TryInspect(new MemoryStream(done), out var doneInfo));
        Assert.False(doneInfo.IsLive);
        Assert.True(doneInfo.IsEncrypted);
    }

    [SkippableFact]
    public void InspectsRealCompletedReplayNotLive()
    {
        TestData.SkipIfNoReplays();
        Assert.True(ReplayHeader.TryInspect(TestData.CompletedReplayPaths[0], out var info));
        Assert.False(info.IsLive);
    }
}

internal static class HeaderBytes
{
    public static byte[] Build(bool isLive, bool isEncrypted)
    {
        using var ms = new MemoryStream();
        WriteU32(ms, ReplayHeader.FileMagic);
        WriteU32(ms, 7);
        WriteU32(ms, 0);
        WriteU32(ms, 0);
        WriteU32(ms, 0);
        WriteU32(ms, 0);
        WriteU32(ms, 0);
        WriteU32(ms, isLive ? 1u : 0u);
        ms.Write(new byte[8]);
        WriteU32(ms, 0);
        WriteU32(ms, isEncrypted ? 1u : 0u);
        WriteU32(ms, 16);
        ms.Write(new byte[16]);
        return ms.ToArray();
    }

    private static void WriteU32(Stream stream, uint value)
    {
        Span<byte> buf = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(buf, value);
        stream.Write(buf);
    }
}
