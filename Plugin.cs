using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using JPRaidDictionary.Data;
using JPRaidDictionary.Models;
using JPRaidDictionary.Services;
using JPRaidDictionary.Windows;

namespace JPRaidDictionary;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    private const string CommandName = "/jpdict";
    private const string TranslateCommand = "/jpt";

    public Configuration Configuration { get; }

    public readonly WindowSystem WindowSystem = new("JPRaidDictionary");

    public DictionaryRepository Repository { get; }

    public TranslationService TranslationService { get; }

    private readonly HttpClient httpClient;
    private readonly MainWindow mainWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Repository = new DictionaryRepository();

        httpClient = new HttpClient();

        var providers = new Dictionary<TranslationProviderType, ITranslationProvider>
        {
            [TranslationProviderType.OpenAi] = new OpenAiTranslationProvider(httpClient,
                () => "https://api.openai.com/v1",
                () => Configuration.OpenAiModel),
            [TranslationProviderType.Claude] = new AnthropicTranslationProvider(httpClient,
                () => Configuration.AnthropicModel),
            [TranslationProviderType.OpenRouter] = new OpenAiTranslationProvider(httpClient,
                () => "https://openrouter.ai/api/v1",
                () => Configuration.OpenRouterModel),
            [TranslationProviderType.Groq] = new OpenAiTranslationProvider(httpClient,
                () => "https://api.groq.com/openai/v1",
                () => Configuration.GroqModel),
            [TranslationProviderType.TogetherAi] = new OpenAiTranslationProvider(httpClient,
                () => "https://api.together.xyz/v1",
                () => Configuration.TogetherAiModel),
            [TranslationProviderType.CustomOpenAi] = new OpenAiTranslationProvider(httpClient,
                () => Configuration.CustomOpenAiBaseUrl,
                () => Configuration.CustomOpenAiModel),
            [TranslationProviderType.GoogleTranslate] = new GoogleTranslateProvider(httpClient),
        };

        TranslationService = new TranslationService(providers, Configuration, Log);

        mainWindow = new MainWindow(this, Repository);
        WindowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnJpDictCommand)
        {
            HelpMessage = "Open the JP Raid Dictionary window.",
        });

        CommandManager.AddHandler(TranslateCommand, new CommandInfo(OnTranslateCommand)
        {
            HelpMessage = "Translate a message to Japanese and send it to chat, e.g. /jpt stack on me",
        });

        PluginInterface.UiBuilder.Draw += DrawUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainWindow;

        if (Configuration.IsMainWindowOpenOnStartup)
            mainWindow.IsOpen = true;
    }

    public void Dispose()
    {
        WindowSystem.RemoveAllWindows();
        mainWindow.Dispose();

        httpClient.Dispose();

        CommandManager.RemoveHandler(CommandName);
        CommandManager.RemoveHandler(TranslateCommand);

        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainWindow;
    }

    private void OnJpDictCommand(string command, string args)
    {
        ToggleMainWindow();
    }

    private void OnTranslateCommand(string command, string args)
    {
        var (prefix, body) = ChatMessageSplitter.Split(args.Trim());

        if (string.IsNullOrWhiteSpace(body))
        {
            ChatGui.PrintError("Usage: /jpt <message> - translates <message> to Japanese and sends it to chat.");
            return;
        }

        _ = TranslateAndSendAsync(prefix, body);
    }

    private async Task TranslateAndSendAsync(string? prefix, string body)
    {
        var result = await TranslationService.TranslateAsync(body, TranslationDirection.EnglishToJapanese).ConfigureAwait(false);

        if (!result.Success)
        {
            ChatGui.PrintError($"JP Translation failed: {result.ErrorMessage}");
            return;
        }

        var translated = result.Text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;

        var finalText = prefix + translated;

        await Framework.RunOnFrameworkThread(() => ChatSendingService.SendMessage(finalText)).ConfigureAwait(false);
    }

    private void DrawUi() => WindowSystem.Draw();

    public void ToggleMainWindow() => mainWindow.Toggle();
}
