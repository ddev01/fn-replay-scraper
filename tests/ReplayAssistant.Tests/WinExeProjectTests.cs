namespace ReplayAssistant.Tests;

public class WinExeProjectTests
{
    [Fact]
    public void HostProjectIsWinExe()
    {
        var csproj = Find("ReplayAssistant.csproj");
        Assert.NotNull(csproj);
        var text = File.ReadAllText(csproj!);
        Assert.Contains("<OutputType>WinExe</OutputType>", text, StringComparison.Ordinal);
        Assert.Contains("Replay Assistant", text, StringComparison.Ordinal);
        Assert.Contains("replay-assistant.ico", text, StringComparison.Ordinal);
    }

    [Fact]
    public void HostKeepsSilentWinExeAndOptionalConsoleFlag()
    {
        var native = Find("NativeConsole.cs");
        Assert.NotNull(native);
        var program = Path.Combine(Path.GetDirectoryName(native)!, "Program.cs");
        Assert.True(File.Exists(program));
        var text = File.ReadAllText(program);
        Assert.Contains("--console", text, StringComparison.Ordinal);
        Assert.Contains("NativeConsole.Attach", text, StringComparison.Ordinal);
        Assert.Contains("--install-startup", text, StringComparison.Ordinal);
        Assert.Contains("LogonTask", text, StringComparison.Ordinal);
    }

    [Fact]
    public void UpdaterProjectIsWinExe()
    {
        var csproj = Find("ReplayAssistant.Update.csproj");
        Assert.NotNull(csproj);
        var text = File.ReadAllText(csproj!);
        Assert.Contains("<OutputType>WinExe</OutputType>", text, StringComparison.Ordinal);
    }

    private static string? Find(string fileName)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var matches = dir.GetFiles(fileName, SearchOption.AllDirectories)
                .Where(f => !f.FullName.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (matches.Length > 0)
            {
                return matches[0].FullName;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
