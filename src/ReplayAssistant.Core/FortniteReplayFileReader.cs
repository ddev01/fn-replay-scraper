using FortniteReplayReader;
using FortniteReplayReader.Models;
using Unreal.Core.Models.Enums;

namespace ReplayAssistant.Core;

public sealed class FortniteReplayFileReader : IReplayFileReader
{
    public FortniteReplay ReadMinimal(string filePath)
    {
        var reader = new ReplayReader(parseMode: ParseMode.Minimal);
        return reader.ReadReplay(filePath);
    }
}
