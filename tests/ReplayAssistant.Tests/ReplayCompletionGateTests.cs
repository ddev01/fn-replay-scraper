using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class ReplayCompletionGateTests
{
    [Fact]
    public void GrowingFileIsNotReady()
    {
        var gate = new ReplayCompletionGate(TimeSpan.FromMinutes(1));
        var path = Path.Combine(Path.GetTempPath(), "growing.replay");
        var t0 = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
        gate.Note(path, 100, t0);
        gate.Note(path, 200, t0.AddSeconds(30));
        Assert.Empty(gate.DrainReady(t0.AddSeconds(50)));
    }

    [Fact]
    public void StableSizeBecomesReadyOnce()
    {
        var gate = new ReplayCompletionGate(TimeSpan.FromSeconds(60));
        var path = Path.Combine(Path.GetTempPath(), "stable.replay");
        var t0 = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
        gate.Note(path, 1000, t0);
        Assert.Empty(gate.DrainReady(t0.AddSeconds(30)));
        var ready = gate.DrainReady(t0.AddSeconds(60));
        Assert.Single(ready);
        Assert.Empty(gate.DrainReady(t0.AddSeconds(120)));
    }

    [Fact]
    public void SizeChangeResetsStability()
    {
        var gate = new ReplayCompletionGate(TimeSpan.FromSeconds(60));
        var path = Path.Combine(Path.GetTempPath(), "reset.replay");
        var t0 = new DateTimeOffset(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
        gate.Note(path, 1000, t0);
        gate.Note(path, 1001, t0.AddSeconds(50));
        Assert.Empty(gate.DrainReady(t0.AddSeconds(100)));
        Assert.Single(gate.DrainReady(t0.AddSeconds(110)));
    }
}
