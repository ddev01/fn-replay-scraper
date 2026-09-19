using FortniteReplayReader.Models;

namespace ReplayAssistant.Core;

public static class HumanIdentityMapper
{
    public static IReadOnlyList<string> ExtractHumanIds(FortniteReplay replay) =>
        NameIngestMapper.FromReplay(replay, "").Players.Select(p => p.EpicId).ToArray();
}
