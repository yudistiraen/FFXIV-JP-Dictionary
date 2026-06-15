using Dalamud.Configuration;
using System;

namespace JPRaidDictionary;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    /// <summary>Whether the main dictionary window should be open automatically on login.</summary>
    public bool IsMainWindowOpenOnStartup { get; set; } = false;

    /// <summary>The last category selected by the user, persisted for convenience.</summary>
    public int LastSelectedCategory { get; set; } = -1;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
