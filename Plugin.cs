using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using JPRaidDictionary.Data;
using JPRaidDictionary.Windows;

namespace JPRaidDictionary;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;

    private const string CommandName = "/jpdict";

    public Configuration Configuration { get; }

    public readonly WindowSystem WindowSystem = new("JPRaidDictionary");

    public DictionaryRepository Repository { get; }

    private readonly MainWindow mainWindow;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Repository = new DictionaryRepository();

        mainWindow = new MainWindow(this, Repository);
        WindowSystem.AddWindow(mainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnJpDictCommand)
        {
            HelpMessage = "Open the JP Raid Dictionary window.",
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

        CommandManager.RemoveHandler(CommandName);

        PluginInterface.UiBuilder.Draw -= DrawUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainWindow;
    }

    private void OnJpDictCommand(string command, string args)
    {
        ToggleMainWindow();
    }

    private void DrawUi() => WindowSystem.Draw();

    public void ToggleMainWindow() => mainWindow.Toggle();
}
