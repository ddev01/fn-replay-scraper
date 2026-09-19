using System.Net.Http.Headers;
using System.Text.Json;

namespace ReplayAssistant.Core;

public sealed record GitHubRelease(Version Version, Uri AssetUri, string AssetName);

public sealed class GitHubUpdateChecker
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public GitHubUpdateChecker(HttpClient http, AppSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<GitHubRelease?> GetNewerReleaseAsync(
        Version current,
        CancellationToken cancellationToken
    )
    {
        if (!_settings.CanCheckUpdates)
        {
            return null;
        }

        var url =
            $"https://api.github.com/repos/{_settings.GitHubOwner}/{_settings.GitHubRepo}/releases/latest";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("ReplayAssistant", current.ToString()));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (!string.IsNullOrWhiteSpace(_settings.GitHubToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.GitHubToken);
        }

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        var root = doc.RootElement;
        if (!root.TryGetProperty("tag_name", out var tagEl))
        {
            return null;
        }

        if (!TryParseVersion(tagEl.GetString(), out var remote))
        {
            return null;
        }

        if (remote <= current)
        {
            return null;
        }

        if (!root.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        JsonElement? exe = null;
        JsonElement? manifest = null;
        foreach (var asset in assets.EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.Equals("latest.json", StringComparison.OrdinalIgnoreCase))
            {
                manifest = asset;
            }

            if (name.Equals(AppPaths.ExeFileName, StringComparison.OrdinalIgnoreCase))
            {
                exe = asset;
            }
        }

        if (exe is null)
        {
            return null;
        }

        var download = exe.Value.GetProperty("browser_download_url").GetString();
        if (!Uri.TryCreate(download, UriKind.Absolute, out var uri))
        {
            return null;
        }

        _ = manifest;
        return new GitHubRelease(remote, uri, AppPaths.ExeFileName);
    }

    public static bool TryParseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0, 0);
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var trimmed = tag.Trim();
        if (trimmed.StartsWith('v') || trimmed.StartsWith('V'))
        {
            trimmed = trimmed[1..];
        }

        return Version.TryParse(trimmed, out version!);
    }
}
