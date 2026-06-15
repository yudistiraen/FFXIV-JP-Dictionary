using System.Threading;
using System.Threading.Tasks;
using JPRaidDictionary.Models;

namespace JPRaidDictionary.Services;

/// <summary>
/// Abstraction over a translation backend. The rest of the plugin depends on
/// this interface rather than any concrete provider, so the backend (or a
/// test double) can be swapped without touching <see cref="TranslationService"/>.
/// </summary>
public interface ITranslationProvider
{
    /// <summary>Whether this provider needs a user-supplied API key to function.</summary>
    bool RequiresApiKey { get; }

    /// <summary>
    /// Translates <paramref name="text"/> in the given <paramref name="direction"/>.
    /// Throws if the request fails; callers are expected to handle errors.
    /// </summary>
    Task<string> TranslateAsync(string text, TranslationDirection direction, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>Verifies that <paramref name="apiKey"/> is valid and usable. Ignored if <see cref="RequiresApiKey"/> is <c>false</c>.</summary>
    Task<ApiConnectionStatus> TestConnectionAsync(string apiKey, CancellationToken cancellationToken = default);
}
