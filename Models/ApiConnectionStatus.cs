namespace JPRaidDictionary.Models;

/// <summary>The current state of the configured translation provider's API key/connection.</summary>
public enum ApiConnectionStatus
{
    /// <summary>No API key has been entered yet.</summary>
    NotConfigured,

    /// <summary>The API key was accepted by the provider.</summary>
    Connected,

    /// <summary>The provider rejected the API key (HTTP 401).</summary>
    InvalidApiKey,

    /// <summary>The connection check failed for another reason (network error, server error, etc.).</summary>
    Error,
}
