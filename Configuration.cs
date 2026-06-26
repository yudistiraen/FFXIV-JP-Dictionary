using Dalamud.Configuration;
using JPRaidDictionary.Models;
using System;

namespace JPRaidDictionary;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Whether the main dictionary window should be open automatically on login.</summary>
    public bool IsMainWindowOpenOnStartup { get; set; } = false;

    /// <summary>The last category selected by the user, persisted for convenience.</summary>
    public int LastSelectedCategory { get; set; } = -1;

    // --- Per-provider API keys ---
    public string OpenAiApiKey { get; set; } = string.Empty;
    public string AnthropicApiKey { get; set; } = string.Empty;
    public string OpenRouterApiKey { get; set; } = string.Empty;
    public string GroqApiKey { get; set; } = string.Empty;
    public string TogetherAiApiKey { get; set; } = string.Empty;
    public string CustomOpenAiApiKey { get; set; } = string.Empty;
    public string CustomOpenAiBaseUrl { get; set; } = string.Empty;

    // --- Per-provider selected model ---
    public string OpenAiModel { get; set; } = "gpt-4o-mini";
    public string AnthropicModel { get; set; } = "claude-haiku-4-5-20251001";
    public string OpenRouterModel { get; set; } = string.Empty;
    public string GroqModel { get; set; } = string.Empty;
    public string TogetherAiModel { get; set; } = string.Empty;
    public string CustomOpenAiModel { get; set; } = string.Empty;

    /// <summary>Which translation backend to use for Quick Translate and chat translation.</summary>
    public TranslationProviderType TranslationProvider { get; set; } = TranslationProviderType.OpenAi;

    /// <summary>Whether translation errors should be written to the plugin log.</summary>
    public bool EnableTranslationLogging { get; set; } = true;

    public string GetApiKey(TranslationProviderType type) => type switch
    {
        TranslationProviderType.OpenAi => OpenAiApiKey,
        TranslationProviderType.Claude => AnthropicApiKey,
        TranslationProviderType.OpenRouter => OpenRouterApiKey,
        TranslationProviderType.Groq => GroqApiKey,
        TranslationProviderType.TogetherAi => TogetherAiApiKey,
        TranslationProviderType.CustomOpenAi => CustomOpenAiApiKey,
        _ => string.Empty,
    };

    public void SetApiKey(TranslationProviderType type, string value)
    {
        switch (type)
        {
            case TranslationProviderType.OpenAi: OpenAiApiKey = value; break;
            case TranslationProviderType.Claude: AnthropicApiKey = value; break;
            case TranslationProviderType.OpenRouter: OpenRouterApiKey = value; break;
            case TranslationProviderType.Groq: GroqApiKey = value; break;
            case TranslationProviderType.TogetherAi: TogetherAiApiKey = value; break;
            case TranslationProviderType.CustomOpenAi: CustomOpenAiApiKey = value; break;
        }
    }

    public string GetModel(TranslationProviderType type) => type switch
    {
        TranslationProviderType.OpenAi => OpenAiModel,
        TranslationProviderType.Claude => AnthropicModel,
        TranslationProviderType.OpenRouter => OpenRouterModel,
        TranslationProviderType.Groq => GroqModel,
        TranslationProviderType.TogetherAi => TogetherAiModel,
        TranslationProviderType.CustomOpenAi => CustomOpenAiModel,
        _ => string.Empty,
    };

    public void SetModel(TranslationProviderType type, string value)
    {
        switch (type)
        {
            case TranslationProviderType.OpenAi: OpenAiModel = value; break;
            case TranslationProviderType.Claude: AnthropicModel = value; break;
            case TranslationProviderType.OpenRouter: OpenRouterModel = value; break;
            case TranslationProviderType.Groq: GroqModel = value; break;
            case TranslationProviderType.TogetherAi: TogetherAiModel = value; break;
            case TranslationProviderType.CustomOpenAi: CustomOpenAiModel = value; break;
        }
    }

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
