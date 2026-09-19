using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class CatchUpTests
{
    [Fact]
    public void FiveUntrackedStableFilesAreQueued()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ra-catchup-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            for (var i = 0; i < 5; i++)
            {
                var path = Path.Combine(root, $"match-{i}.replay");
                File.WriteAllBytes(path, [1, 2, 3, 4]);
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddMinutes(-10));
            }

            var queued = CatchUpScanner.FindCompletedCandidates(
                root,
                _ => false,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(1)
            );
            Assert.Equal(5, queued.Count);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void FinishedRowsAreSkipped()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ra-catchup-skip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var keep = Path.Combine(root, "new.replay");
            var skip = Path.Combine(root, "old.replay");
            File.WriteAllBytes(keep, [1]);
            File.WriteAllBytes(skip, [1]);
            File.SetLastWriteTimeUtc(keep, DateTime.UtcNow.AddMinutes(-10));
            File.SetLastWriteTimeUtc(skip, DateTime.UtcNow.AddMinutes(-10));
            var queued = CatchUpScanner.FindCompletedCandidates(
                root,
                path => path.EndsWith("old.replay", StringComparison.OrdinalIgnoreCase),
                DateTimeOffset.UtcNow,
                TimeSpan.FromMinutes(1)
            );
            Assert.Single(queued);
            Assert.EndsWith("new.replay", queued[0], StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
