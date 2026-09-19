namespace ReplayAssistant.Tests;

public static class TestData
{
    public static IReadOnlyList<string> CompletedReplayPaths
    {
        get
        {
            var found = new List<string>();
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                var testdata = Path.Combine(dir.FullName, "testdata");
                if (Directory.Exists(testdata))
                {
                    found.AddRange(Directory.EnumerateFiles(testdata, "*.replay"));
                }

                dir = dir.Parent;
            }

            var demos = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FortniteGame",
                "Saved",
                "Demos"
            );
            if (Directory.Exists(demos))
            {
                found.AddRange(Directory.EnumerateFiles(demos, "*.replay"));
            }

            var lobby = @"F:\Dev\fn-lobby-info\testdata";
            if (Directory.Exists(lobby))
            {
                found.AddRange(Directory.EnumerateFiles(lobby, "*.replay"));
            }

            return found
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}live{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    public static void SkipIfNoReplays() =>
        Skip.If(CompletedReplayPaths.Count == 0, "no completed .replay samples found");
}
