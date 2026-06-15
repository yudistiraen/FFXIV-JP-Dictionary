using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Services;

/// <summary>
/// <see cref="ITranslationProvider"/> implementation backed by the Anthropic
/// (Claude) Messages API. Uses the API key the user supplies via
/// <see cref="Configuration.AnthropicApiKey"/> - the plugin never ships with
/// or uses a developer-owned key.
/// </summary>
public class AnthropicTranslationProvider : ITranslationProvider
{
    private const string ApiBaseUrl = "https://api.anthropic.com/v1";
    private const string Model = "claude-haiku-4-5-20251001";
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient httpClient;

    public AnthropicTranslationProvider(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public bool RequiresApiKey => true;

    public async Task<string> TranslateAsync(string text, TranslationDirection direction, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var requestBody = new
        {
            model = Model,
            max_tokens = 1024,
            system = TranslationPrompts.SystemPrompt,
            messages = new object[]
            {
                new { role = "user", content = $"{TranslationPrompts.GetDirectionInstruction(direction)}\n\n{text}" },
            },
        };

        using var request = CreateRequest(HttpMethod.Post, "/messages", apiKey);
        request.Content = JsonContent.Create(requestBody);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Anthropic API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);

        var content = json
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString();

        return content?.Trim() ?? string.Empty;
    }

    public async Task<ApiConnectionStatus> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            return ApiConnectionStatus.NotConfigured;

        using var request = CreateRequest(HttpMethod.Get, "/models", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
            return ApiConnectionStatus.Connected;

        if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
            return ApiConnectionStatus.InvalidApiKey;

        return ApiConnectionStatus.Error;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string apiKey)
    {
        var request = new HttpRequestMessage(method, $"{ApiBaseUrl}{path}");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        return request;
    }
}
