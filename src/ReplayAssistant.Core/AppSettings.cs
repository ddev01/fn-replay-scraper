namespace ReplayAssistant.Core;

public sealed class AppSettings
{
    public const string ApiKeyHeaderName = "X-Api-Key";
    public const string DefaultApiPath = "/api/replay/players";
    public const string AuthorizationHeaderName = "Authorization";

    public string ApiUrl { get; init; } = "";

    public string ApiKey { get; init; } = "";

    public string GitHubOwner { get; init; } = "";

    public string GitHubRepo { get; init; } = "";

    public string GitHubToken { get; init; } = "";

    public string DemosPath { get; init; } = "";

    public int StableSeconds { get; init; } = 60;

    public bool WatchOnlyWhenFortniteClosed { get; init; } = true;

    public int FortniteProcessPollMinutes { get; init; } = 5;

    public int ParseTimeoutSeconds { get; init; } = 20;

    public int ApiChunkSize { get; init; } = 1000;

    public string ResolvedDemosPath =>
        string.IsNullOrWhiteSpace(DemosPath) ? DemosFolder.DefaultPath : DemosPath;

    public Uri? ApiBaseUri =>
        Uri.TryCreate(ApiUrl, UriKind.Absolute, out var uri) ? uri : null;

    public string ResolvedPostUrl
    {
        get
        {
            var root = (ApiUrl ?? "").TrimEnd('/');
            if (root.EndsWith(DefaultApiPath, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            return string.IsNullOrWhiteSpace(root) ? "" : root + DefaultApiPath;
        }
    }

    public bool CanPost =>
        Uri.TryCreate(ResolvedPostUrl, UriKind.Absolute, out var uri)
        && !string.IsNullOrWhiteSpace(ApiKey)
        && uri.Host is not "example.invalid";

    public bool CanCheckUpdates =>
        !string.IsNullOrWhiteSpace(GitHubOwner) && !string.IsNullOrWhiteSpace(GitHubRepo);

    public TimeSpan StableWindow => TimeSpan.FromSeconds(Math.Max(5, StableSeconds));

    public TimeSpan ParseTimeout => TimeSpan.FromSeconds(Math.Max(5, ParseTimeoutSeconds));

    public TimeSpan FortnitePollInterval =>
        TimeSpan.FromMinutes(Math.Max(1, FortniteProcessPollMinutes));

    public static AppSettings FromEnvironment(AppSettings file)
    {
        return new AppSettings
        {
            ApiUrl = FirstNonEmpty(
                Environment.GetEnvironmentVariable("REPLAY_ASSISTANT_API_URL"),
                file.ApiUrl
            ),
            ApiKey = FirstNonEmpty(
                Environment.GetEnvironmentVariable("REPLAY_ASSISTANT_API_KEY"),
                file.ApiKey
            ),
            GitHubOwner = FirstNonEmpty(
                Environment.GetEnvironmentVariable("REPLAY_ASSISTANT_GITHUB_OWNER"),
                file.GitHubOwner
            ),
            GitHubRepo = FirstNonEmpty(
                Environment.GetEnvironmentVariable("REPLAY_ASSISTANT_GITHUB_REPO"),
                file.GitHubRepo
            ),
            GitHubToken = FirstNonEmpty(
                Environment.GetEnvironmentVariable("REPLAY_ASSISTANT_GITHUB_TOKEN"),
                file.GitHubToken
            ),
            DemosPath = FirstNonEmpty(
                Environment.GetEnvironmentVariable("REPLAY_ASSISTANT_DEMOS_PATH"),
                file.DemosPath
            ),
            StableSeconds = file.StableSeconds,
            WatchOnlyWhenFortniteClosed = file.WatchOnlyWhenFortniteClosed,
            FortniteProcessPollMinutes = file.FortniteProcessPollMinutes,
            ParseTimeoutSeconds = file.ParseTimeoutSeconds,
            ApiChunkSize = file.ApiChunkSize,
        };
    }

    private static string FirstNonEmpty(string? a, string b) =>
        string.IsNullOrWhiteSpace(a) ? b : a;
}
