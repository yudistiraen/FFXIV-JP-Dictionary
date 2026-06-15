using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Services;

/// <summary>
/// <see cref="ITranslationProvider"/> implementation backed by the OpenAI
/// Chat Completions API. Uses the API key the user supplies via
/// <see cref="Configuration.OpenAiApiKey"/> - the plugin never ships with or
/// uses a developer-owned key.
/// </summary>
public class OpenAiTranslationProvider : ITranslationProvider
{
    private const string ApiBaseUrl = "https://api.openai.com/v1";
    private const string Model = "gpt-4o-mini";

    private readonly HttpClient httpClient;

    public OpenAiTranslationProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public bool RequiresApiKey => true;

    public async Task<string> TranslateAsync(string text, TranslationDirection direction, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("OpenAI API key is not configured.");

        var requestBody = new
        {
            model = Model,
            messages = new object[]
            {
                new { role = "system", content = TranslationPrompts.SystemPrompt },
                new { role = "user", content = $"{TranslationPrompts.GetDirectionInstruction(direction)}\n\n{text}" },
            },
            temperature = 0.2,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/chat/completions")
        {
            Content = JsonContent.Create(requestBody),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"OpenAI API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);

        var content = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content?.Trim() ?? string.Empty;
    }

    public async Task<ApiConnectionStatus> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return ApiConnectionStatus.NotConfigured;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
            return ApiConnectionStatus.Connected;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ApiConnectionStatus.InvalidApiKey;

        return ApiConnectionStatus.Error;
    }
}
