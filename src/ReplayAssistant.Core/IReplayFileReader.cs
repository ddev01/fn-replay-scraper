using FortniteReplayReader.Models;

namespace ReplayAssistant.Core;

public interface IReplayFileReader
{
    FortniteReplay ReadMinimal(string filePath);
}
