using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace EmoteReactor;

[Serializable]
public class Config : IPluginConfiguration
{
    [field: NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public int Version { get; set; }
    public bool Enabled { get; set; } = true;

    public void Initialize(IDalamudPluginInterface pluginInterface) => _pluginInterface = pluginInterface;

    public void Save()
    {
        var interfaceToUse = _pluginInterface ?? Plugin.PluginInterface;
        interfaceToUse.SavePluginConfig(this);
    }
}
