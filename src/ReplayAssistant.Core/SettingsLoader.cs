namespace ReplayAssistant.Core;

public static class SettingsLoader
{
    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

    public static AppSettings Load(params string[] candidatePaths)
    {
        foreach (var path in candidatePaths)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                continue;
            }

            var json = File.ReadAllText(path);
            var file = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            if (file is not null)
            {
                return AppSettings.FromEnvironment(file);
            }
        }

        return AppSettings.FromEnvironment(new AppSettings());
    }
}
