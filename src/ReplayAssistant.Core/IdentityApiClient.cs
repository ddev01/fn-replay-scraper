using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReplayAssistant.Core;

public sealed class IdentityApiClient
{
    private readonly HttpClient _http;
    private readonly AppSettings _settings;
    private readonly JsonSerializerOptions _json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public IdentityApiClient(HttpClient http, AppSettings settings)
    {
        _http = http;
        _settings = settings;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<bool> PostReplayAsync(ReplayIngest ingest, CancellationToken cancellationToken)
    {
        if (!_settings.CanPost)
        {
            return false;
        }

        var body = ReplayPlayersRequestFactory.TryCreate(ingest);
        if (body is null)
        {
            return true;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ResolvedPostUrl);
        request.Headers.TryAddWithoutValidation(AppSettings.ApiKeyHeaderName, _settings.ApiKey);
        if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            request.Headers.TryAddWithoutValidation(
                AppSettings.AuthorizationHeaderName,
                "Bearer " + _settings.ApiKey
            );
        }

        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var payload = JsonSerializer.Serialize(body, _json);
        request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}
