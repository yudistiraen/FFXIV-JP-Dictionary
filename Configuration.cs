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

    /// <summary>
    /// The user's own OpenAI API key, used for the Quick Translate tab and
    /// chat translation modes. Never bundled with the plugin and never sent
    /// anywhere other than the OpenAI API.
    /// </summary>
    public string OpenAiApiKey { get; set; } = string.Empty;

    /// <summary>
    /// The user's own Anthropic (Claude) API key, used when
    /// <see cref="TranslationProvider"/> is <see cref="TranslationProviderType.Claude"/>.
    /// Never bundled with the plugin and never sent anywhere other than the
    /// Anthropic API.
    /// </summary>
    public string AnthropicApiKey { get; set; } = string.Empty;

    /// <summary>Which translation backend to use for Quick Translate and chat translation.</summary>
    public TranslationProviderType TranslationProvider { get; set; } = TranslationProviderType.OpenAi;

    /// <summary>Whether translation errors should be written to the plugin log.</summary>
    public bool EnableTranslationLogging { get; set; } = true;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
