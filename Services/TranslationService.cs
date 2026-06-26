using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Services;

/// <summary>
/// Coordinates translation requests for both the Quick Translate tab and the
/// outgoing chat translation modes. Routes to the correct
/// <see cref="ITranslationProvider"/> based on
/// <see cref="Configuration.TranslationProvider"/>.
/// </summary>
public class TranslationService
{
    private readonly Dictionary<TranslationProviderType, ITranslationProvider> providers;
    private readonly Configuration configuration;
    private readonly IPluginLog log;

    public TranslationService(Dictionary<TranslationProviderType, ITranslationProvider> providers, Configuration configuration, IPluginLog log)
    {
        this.providers = providers;
        this.configuration = configuration;
        this.log = log;
    }

    /// <summary>The result of the most recent <see cref="TestConnectionAsync"/> call.</summary>
    public ApiConnectionStatus Status { get; private set; } = ApiConnectionStatus.NotConfigured;

    /// <summary>
    /// Translates <paramref name="text"/> using the currently configured provider.
    /// Never throws.
    /// </summary>
    public async Task<TranslationResult> TranslateAsync(string text, TranslationDirection direction, CancellationToken cancellationToken = default)
    {
        var (provider, apiKey) = GetActiveProvider();

        if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(apiKey))
            return new TranslationResult(text, false, $"{GetProviderDisplayName(configuration.TranslationProvider)} API key is not configured.");

        try
        {
            var translated = await provider.TranslateAsync(text, direction, apiKey, cancellationToken).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(translated))
                return new TranslationResult(text, false, "The translation provider returned an empty response.");

            return new TranslationResult(translated, true, null);
        }
        catch (Exception ex)
        {
            if (configuration.EnableTranslationLogging)
                log.Error(ex, "Translation request failed");

            return new TranslationResult(text, false, ex.Message);
        }
    }

    /// <summary>Tests the configured API key against the active provider and updates <see cref="Status"/>.</summary>
    public async Task<ApiConnectionStatus> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var (provider, apiKey) = GetActiveProvider();

        if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(apiKey))
        {
            Status = ApiConnectionStatus.NotConfigured;
            return Status;
        }

        try
        {
            Status = await provider.TestConnectionAsync(apiKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (configuration.EnableTranslationLogging)
                log.Error(ex, "Translation provider connection test failed");

            Status = ApiConnectionStatus.Error;
        }

        return Status;
    }

    /// <summary>Fetches available models from the active provider's API.</summary>
    public async Task<List<string>> FetchModelsAsync(CancellationToken cancellationToken = default)
    {
        var (provider, apiKey) = GetActiveProvider();

        if (provider.RequiresApiKey && string.IsNullOrWhiteSpace(apiKey))
            return new List<string>();

        return await provider.FetchModelsAsync(apiKey, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Resets <see cref="Status"/> to <see cref="ApiConnectionStatus.NotConfigured"/>.</summary>
    public void ResetStatus() => Status = ApiConnectionStatus.NotConfigured;

    private (ITranslationProvider Provider, string ApiKey) GetActiveProvider()
    {
        var type = configuration.TranslationProvider;
        var provider = providers.TryGetValue(type, out var p) ? p : providers[TranslationProviderType.OpenAi];
        var apiKey = configuration.GetApiKey(type);
        return (provider, apiKey);
    }

    public static string GetProviderDisplayName(TranslationProviderType provider) => provider switch
    {
        TranslationProviderType.Claude => "Anthropic (Claude)",
        TranslationProviderType.GoogleTranslate => "Google Translate (Free)",
        TranslationProviderType.OpenRouter => "OpenRouter",
        TranslationProviderType.Groq => "Groq",
        TranslationProviderType.TogetherAi => "Together AI",
        TranslationProviderType.CustomOpenAi => "Custom (OpenAI-Compatible)",
        _ => "OpenAI",
    };
}
