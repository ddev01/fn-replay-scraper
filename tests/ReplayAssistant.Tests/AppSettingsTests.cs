using ReplayAssistant.Core;

namespace ReplayAssistant.Tests;

public class AppSettingsTests
{
    [Fact]
    public void PlaceholderHostCannotPost()
    {
        var settings = new AppSettings
        {
            ApiUrl = "https://example.invalid/api",
            ApiKey = "secret",
        };
        Assert.False(settings.CanPost);
    }

    [Fact]
    public void EnvOverridesApiUrl()
    {
        Environment.SetEnvironmentVariable("REPLAY_ASSISTANT_API_URL", "http://localhost:9");
        Environment.SetEnvironmentVariable("REPLAY_ASSISTANT_API_KEY", "from-env");
        try
        {
            var merged = AppSettings.FromEnvironment(
                new AppSettings { ApiUrl = "https://example.invalid", ApiKey = "file" }
            );
            Assert.Equal("http://localhost:9", merged.ApiUrl);
            Assert.Equal("from-env", merged.ApiKey);
        }
        finally
        {
            Environment.SetEnvironmentVariable("REPLAY_ASSISTANT_API_URL", null);
            Environment.SetEnvironmentVariable("REPLAY_ASSISTANT_API_KEY", null);
        }
    }

    [Fact]
    public void ResolvesNameFnPlayersPath()
    {
        var settings = new AppSettings { ApiUrl = "https://namefn.example" };
        Assert.Equal("https://namefn.example/api/replay/players", settings.ResolvedPostUrl);
    }
}
