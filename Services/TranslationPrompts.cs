using JPRaidDictionary.Models;

namespace JPRaidDictionary.Services;

/// <summary>
/// Shared prompt text used by every <see cref="ITranslationProvider"/>
/// implementation, so all providers translate consistently regardless of
/// which backend the user selects.
/// </summary>
internal static class TranslationPrompts
{
    /// <summary>System prompt used for every Quick Translate / chat translation request.</summary>
    public const string SystemPrompt =
        "Translate between English and Japanese for FFXIV (Final Fantasy XIV) raid/party chat.\n\n" +
        "Rules:\n" +
        "- Use the casual, informal Japanese that JP FFXIV raid groups and statics actually use in party chat " +
        "(plain/imperative form like \"~して\"/\"~て\", not polite/formal keigo like \"~してください\"/\"~です/ます\").\n" +
        "- Use common FFXIV community slang and shorthand where natural (e.g. 頭割り, 散開, 安置, スタック, 北/南/東/西基準, " +
        "ヘイト, タゲ, 開幕, 戻し, 抑え, MT/ST/OT/H1/H2/D1-D4, etc.), matching how JP players actually phrase callouts.\n" +
        "- Preserve raid mechanics terminology, job names, and abbreviations as commonly used - don't over-translate " +
        "established jargon.\n" +
        "- Keep the message short and concise, as if typed quickly during a pull.\n" +
        "- Output ONLY a single translated message and nothing else - no alternative phrasings, no romaji, " +
        "no explanations, no notes, no parentheticals, no commentary.";

    /// <summary>A short per-request instruction describing the translation direction.</summary>
    public static string GetDirectionInstruction(TranslationDirection direction) => direction == TranslationDirection.EnglishToJapanese
        ? "Translate the following text from English to Japanese."
        : "Translate the following text from Japanese to English.";
}
