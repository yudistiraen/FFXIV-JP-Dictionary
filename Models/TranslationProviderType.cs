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

    /// <summary>OpenRouter (OpenAI-compatible).</summary>
    OpenRouter,

    /// <summary>Groq (OpenAI-compatible).</summary>
    Groq,

    /// <summary>Together AI (OpenAI-compatible).</summary>
    TogetherAi,

    /// <summary>Any OpenAI-compatible API with a user-supplied base URL.</summary>
    CustomOpenAi,
}
