namespace JPRaidDictionary.Models;

/// <summary>
/// The outcome of a translation request. <see cref="Success"/> is
/// <c>false</c> when the translation could not be performed (missing API
/// key, network error, etc.); in that case <see cref="Text"/> contains the
/// original, untranslated text so callers can fall back safely.
/// </summary>
public record TranslationResult(string Text, bool Success, string? ErrorMessage);
