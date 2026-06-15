using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Services;

/// <summary>
/// <see cref="ITranslationProvider"/> implementation backed by Google
/// Translate's free, keyless "gtx" web endpoint. No API key is required, but
/// this is an unofficial endpoint - it may be rate-limited or change without
/// notice, and (unlike <see cref="OpenAiTranslationProvider"/> and
/// <see cref="AnthropicTranslationProvider"/>) it does not understand the
/// FFXIV raid-translation prompt, so results are plain literal translations.
/// </summary>
public class GoogleTranslateProvider : ITranslationProvider
{
    private const string BaseUrl = "https://translate.googleapis.com/translate_a/single";

    private readonly HttpClient httpClient;

    public GoogleTranslateProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public bool RequiresApiKey => false;

    public async Task<string> TranslateAsync(string text, TranslationDirection direction, string apiKey, CancellationToken cancellationToken = default)
    {
        var (sourceLang, targetLang) = direction == TranslationDirection.EnglishToJapanese
            ? ("en", "ja")
            : ("ja", "en");

        var url = $"{BaseUrl}?client=gtx&sl={sourceLang}&tl={targetLang}&dt=t&q={Uri.EscapeDataString(text)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 (compatible; JPRaidDictionary Dalamud plugin)");

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Google Translate request failed with status {(int)response.StatusCode} ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);

        var result = new StringBuilder();
        foreach (var sentence in json[0].EnumerateArray())
            result.Append(sentence[0].GetString());

        return result.ToString().Trim();
    }

    public async Task<ApiConnectionStatus> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var translated = await TranslateAsync("test", TranslationDirection.EnglishToJapanese, apiKey, cancellationToken).ConfigureAwait(false);
            return string.IsNullOrWhiteSpace(translated) ? ApiConnectionStatus.Error : ApiConnectionStatus.Connected;
        }
        catch
        {
            return ApiConnectionStatus.Error;
        }
    }
}
