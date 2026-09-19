using FortniteReplayReader.Models;
using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class ReplayParserTests
{
    [Fact]
    public void TimeoutFailsCleanlyWithoutHanging()
    {
        var parser = new ReplayParser(new HangingReader(), TimeSpan.FromMilliseconds(250));
        var path = Path.Combine(Path.GetTempPath(), $"ra-hang-{Guid.NewGuid():N}.replay");
        File.WriteAllBytes(path, HeaderBytes.Build(isLive: false, isEncrypted: false));
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var result = parser.ParseCompleted(path);
            sw.Stop();
            Assert.False(result.Success);
            Assert.True(result.TimedOut);
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(5));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void DoesNotParseLiveHeader()
    {
        var parser = new ReplayParser(new ThrowingReader(), TimeSpan.FromSeconds(5));
        var path = Path.Combine(Path.GetTempPath(), $"ra-live-{Guid.NewGuid():N}.replay");
        File.WriteAllBytes(path, HeaderBytes.Build(isLive: true, isEncrypted: true));
        try
        {
            var result = parser.ParseCompleted(path);
            Assert.True(result.IsLive);
            Assert.False(result.Success);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [SkippableFact]
    public void CurrentSeasonCompletedReplayYieldsHumanIds()
    {
        TestData.SkipIfNoReplays();
        var newest = TestData
            .CompletedReplayPaths.Select(p => new FileInfo(p))
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .First();
        Skip.If(newest.Length < 100_000, "replay too small to be a full match");
        var parser = new ReplayParser(timeout: TimeSpan.FromSeconds(30));
        var result = parser.ParseCompleted(newest.FullName);
        Assert.True(result.Success, result.Error);
        Assert.False(result.IsLive);
        Assert.True(result.HumanIds.Count > 0);
    }

    [SkippableFact]
    public void OutdatedOrOddReplaysDoNotHangAndAreNotRetriedLive()
    {
        TestData.SkipIfNoReplays();
        var parser = new ReplayParser(timeout: TimeSpan.FromSeconds(25));
        foreach (var path in TestData.CompletedReplayPaths)
        {
            var result = parser.ParseCompleted(path);
            Assert.False(result.IsLive && result.Success);
            if (!result.Success)
            {
                Assert.True(
                    result.Status is ReplayStatus.Unsupported or ReplayStatus.Failed,
                    result.Error
                );
            }
        }
    }

    private sealed class HangingReader : IReplayFileReader
    {
        public FortniteReplay ReadMinimal(string filePath)
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
            return new FortniteReplay();
        }
    }

    private sealed class ThrowingReader : IReplayFileReader
    {
        public FortniteReplay ReadMinimal(string filePath) =>
            throw new InvalidOperationException("live files must not be parsed");
    }
}
