using System;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Services;

/// <summary>
/// Coordinates translation requests for both the Quick Translate tab and the
/// outgoing chat translation modes. Wraps the configured
/// <see cref="ITranslationProvider"/> (OpenAI or Claude, depending on
/// <see cref="Configuration.TranslationProvider"/>) with configuration
/// lookups, API-key validation, and error handling so callers never need to
/// worry about the underlying provider failing.
/// </summary>
public class TranslationService
{
    private readonly ITranslationProvider openAiProvider;
    private readonly ITranslationProvider anthropicProvider;
    private readonly ITranslationProvider googleTranslateProvider;
    private readonly Configuration configuration;
    private readonly IPluginLog log;

    public TranslationService(ITranslationProvider openAiProvider, ITranslationProvider anthropicProvider, ITranslationProvider googleTranslateProvider, Configuration configuration, IPluginLog log)
    {
        this.openAiProvider = openAiProvider;
        this.anthropicProvider = anthropicProvider;
        this.googleTranslateProvider = googleTranslateProvider;
        this.configuration = configuration;
        this.log = log;
    }

    /// <summary>The result of the most recent <see cref="TestConnectionAsync"/> call.</summary>
    public ApiConnectionStatus Status { get; private set; } = ApiConnectionStatus.NotConfigured;

    /// <summary>
    /// Translates <paramref name="text"/> using the currently configured
    /// provider. Never throws: if the API key is missing or the request
    /// fails, the result contains the original text with
    /// <see cref="TranslationResult.Success"/> set to <c>false</c> and
    /// <see cref="TranslationResult.ErrorMessage"/> describing the problem.
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

    /// <summary>Resets <see cref="Status"/> to <see cref="ApiConnectionStatus.NotConfigured"/>, e.g. after switching providers.</summary>
    public void ResetStatus() => Status = ApiConnectionStatus.NotConfigured;

    private (ITranslationProvider Provider, string ApiKey) GetActiveProvider() => configuration.TranslationProvider switch
    {
        TranslationProviderType.Claude => (anthropicProvider, configuration.AnthropicApiKey),
        TranslationProviderType.GoogleTranslate => (googleTranslateProvider, string.Empty),
        _ => (openAiProvider, configuration.OpenAiApiKey),
    };

    public static string GetProviderDisplayName(TranslationProviderType provider) => provider switch
    {
        TranslationProviderType.Claude => "Anthropic (Claude)",
        TranslationProviderType.GoogleTranslate => "Google Translate (Free)",
        _ => "OpenAI",
    };
}
