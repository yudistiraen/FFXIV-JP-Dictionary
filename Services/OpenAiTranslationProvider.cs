using System;
using System.Collections.Generic;
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
/// <see cref="ITranslationProvider"/> for any OpenAI-compatible Chat Completions API.
/// Base URL and model are resolved via delegates so they can be read from
/// <see cref="Configuration"/> at call time - changes take effect immediately.
/// </summary>
public class OpenAiTranslationProvider : ITranslationProvider
{
    private readonly HttpClient httpClient;
    private readonly Func<string> getBaseUrl;
    private readonly Func<string> getModel;

    public OpenAiTranslationProvider(HttpClient httpClient, Func<string> getBaseUrl, Func<string> getModel)
    {
        this.httpClient = httpClient;
        this.getBaseUrl = getBaseUrl;
        this.getModel = getModel;
    }

    public bool RequiresApiKey => true;

    public async Task<string> TranslateAsync(string text, TranslationDirection direction, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("API key is not configured.");

        var baseUrl = getBaseUrl().TrimEnd('/');
        var model = getModel();

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Model is not configured. Please select or enter a model name.");

        var requestBody = new
        {
            model,
            messages = new object[]
            {
                new { role = "system", content = TranslationPrompts.SystemPrompt },
                new { role = "user", content = $"{TranslationPrompts.GetDirectionInstruction(direction)}\n\n{text}" },
            },
            temperature = 0.2,
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = JsonContent.Create(requestBody),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"API request failed with status {(int)response.StatusCode} ({response.StatusCode}): {body}");
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

        var baseUrl = getBaseUrl().TrimEnd('/');

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
            return ApiConnectionStatus.Connected;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return ApiConnectionStatus.InvalidApiKey;

        return ApiConnectionStatus.Error;
    }

    public async Task<List<string>> FetchModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var baseUrl = getBaseUrl().TrimEnd('/');

        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("Base URL is not configured.");

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{baseUrl}/models");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            throw new HttpRequestException($"Failed to fetch models ({(int)response.StatusCode}): {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);
        var models = new List<string>();

        if (json.TryGetProperty("data", out var dataArray) && dataArray.ValueKind == JsonValueKind.Array)
        {
            foreach (var model in dataArray.EnumerateArray())
            {
                if (model.TryGetProperty("id", out var idProp))
                {
                    var id = idProp.GetString();
                    if (!string.IsNullOrEmpty(id))
                        models.Add(id);
                }
            }
        }

        models.Sort(StringComparer.OrdinalIgnoreCase);
        return models;
    }
}
