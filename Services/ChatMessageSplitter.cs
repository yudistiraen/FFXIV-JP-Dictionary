namespace JPRaidDictionary.Services;

/// <summary>
/// Splits a raw chat box message into an optional leading slash command
/// (e.g. <c>"/p "</c>) and the remaining message body, so that only the
/// body is sent for translation while the channel command is preserved.
/// </summary>
public static class ChatMessageSplitter
{
    /// <summary>
    /// Splits <paramref name="input"/> into a command prefix (including its
    /// trailing space, if any) and the message body. If <paramref name="input"/>
    /// does not start with <c>/</c>, <see cref="Prefix"/> is <c>null</c> and the
    /// whole input is returned as the body.
    /// </summary>
    public static (string? Prefix, string Body) Split(string input)
    {
        if (string.IsNullOrEmpty(input) || input[0] != '/')
            return (null, input);

        var spaceIndex = input.IndexOf(' ');
        if (spaceIndex < 0)
            return (input, string.Empty);

        return (input[..(spaceIndex + 1)], input[(spaceIndex + 1)..]);
    }
}
