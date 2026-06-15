namespace JPRaidDictionary.Models;

/// <summary>The translation backend used for Quick Translate and chat translation.</summary>
public enum TranslationProviderType
{
    /// <summary>OpenAI Chat Completions API.</summary>
    OpenAi,

    /// <summary>Anthropic (Claude) Messages API.</summary>
    Claude,

    /// <summary>Free, keyless Google Translate endpoint. No API key required.</summary>
    GoogleTranslate,
}
