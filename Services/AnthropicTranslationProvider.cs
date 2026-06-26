using System;
using System.Collections.Generic;
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
/// (Claude) Messages API. The model is resolved via a delegate so it can be
/// read from <see cref="Configuration"/> at call time.
/// </summary>
public class AnthropicTranslationProvider : ITranslationProvider
{
    private const string ApiBaseUrl = "https://api.anthropic.com/v1";
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient httpClient;
    private readonly Func<string> getModel;

    public AnthropicTranslationProvider(HttpClient httpClient, Func<string> getModel)
    {
        this.httpClient = httpClient;
        this.getModel = getModel;
    }

    public bool RequiresApiKey => true;

    public async Task<string> TranslateAsync(string text, TranslationDirection direction, string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("Anthropic API key is not configured.");

        var model = getModel();

        if (string.IsNullOrWhiteSpace(model))
            throw new InvalidOperationException("Model is not configured. Please select or enter a model name.");

        var requestBody = new
        {
            model,
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

    public async Task<List<string>> FetchModelsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var models = new List<string>();
        var hasMore = true;
        string? afterId = null;

        while (hasMore)
        {
            var path = afterId == null ? "/models?limit=100" : $"/models?limit=100&after_id={afterId}";
            using var request = CreateRequest(HttpMethod.Get, path, apiKey);

            using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                throw new HttpRequestException($"Failed to fetch models ({(int)response.StatusCode}): {body}");
            }

            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken).ConfigureAwait(false);

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

            hasMore = json.TryGetProperty("has_more", out var hasMoreProp) && hasMoreProp.GetBoolean();
            if (hasMore && json.TryGetProperty("last_id", out var lastIdProp))
                afterId = lastIdProp.GetString();
            else
                hasMore = false;
        }

        models.Sort(StringComparer.OrdinalIgnoreCase);
        return models;
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path, string apiKey)
    {
        var request = new HttpRequestMessage(method, $"{ApiBaseUrl}{path}");
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        return request;
    }
}
