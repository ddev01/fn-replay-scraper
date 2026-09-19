namespace ReplayAssistant.Core;

public sealed class CatchUpScanner
{
    public static IReadOnlyList<string> FindCompletedCandidates(
        string demosDirectory,
        Func<string, bool> alreadyFinished,
        DateTimeOffset utcNow,
        TimeSpan stableFor
    )
    {
        var result = new List<string>();
        foreach (var path in DemosFolder.EnumerateReplayFiles(demosDirectory))
        {
            if (alreadyFinished(path))
            {
                continue;
            }

            var (length, lastWrite) = DemosFolder.Snapshot(path);
            if (length <= 0)
            {
                continue;
            }

            if (ReplayCompletionGate.IsOldEnoughForCatchUp(lastWrite, utcNow, stableFor))
            {
                result.Add(path);
            }
        }

        return result;
    }
}
