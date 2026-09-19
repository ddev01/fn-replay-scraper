using System.Diagnostics;
using ReplayAssistant.Core;
using Xunit.Abstractions;

namespace ReplayAssistant.Tests;

public class ReplayBakeoffTests(ITestOutputHelper output)
{
    [SkippableFact]
    public void ReportEveryCompletedSample()
    {
        TestData.SkipIfNoReplays();
        var parser = new ReplayParser(timeout: TimeSpan.FromSeconds(30));
        foreach (var path in TestData.CompletedReplayPaths)
        {
            var info = new FileInfo(path);
            var sw = Stopwatch.StartNew();
            var result = parser.ParseCompleted(path);
            sw.Stop();
            output.WriteLine(
                "{0}\tsize={1}\tok={2}\tlive={3}\tids={4}\tstatus={5}\tms={6}\terr={7}",
                info.Name,
                info.Length,
                result.Success,
                result.IsLive,
                result.HumanIds.Count,
                result.Status,
                sw.ElapsedMilliseconds,
                result.Error
            );
            Assert.False(result.IsLive);
        }
    }
}
