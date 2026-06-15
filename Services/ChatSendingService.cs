using System.Text;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace JPRaidDictionary.Services;

/// <summary>
/// Sends a chat message by calling the game's own
/// <c>UIModule::ProcessChatBoxEntry</c> function - the same function the
/// client uses when the player types a message and presses Enter, and the
/// same one used by <c>ECommons.Automation.Chat.SendMessage</c>. Any leading
/// slash command in <paramref name="text"/> (e.g. <c>/p </c>) is honored by
/// the game, just like manually typing it into the chat box.
///
/// Must be called from the game's main thread, e.g. via
/// <see cref="Dalamud.Plugin.Services.IFramework.RunOnFrameworkThread"/>.
/// </summary>
public static class ChatSendingService
{
    public static unsafe void SendMessage(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;

        var bytes = Encoding.UTF8.GetBytes(text);
        var message = Utf8String.FromSequence(bytes);

        try
        {
            UIModule.Instance()->ProcessChatBoxEntry(message);
        }
        finally
        {
            message->Dtor(true);
        }
    }
}
