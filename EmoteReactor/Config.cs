using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace EmoteReactor;

[Serializable]
public class Config : IPluginConfiguration
{
    [field: NonSerialized]
    private IDalamudPluginInterface? _pluginInterface;

    public int Version { get; set; }
    public bool Enabled { get; set; } = true;

    public float GlobalThrottleSeconds { get; set; }
    public List<ReactionRule> Rules { get; set; } = [];

    public void Initialize(IDalamudPluginInterface pluginInterface) => _pluginInterface = pluginInterface;

    public void Save()
    {
        var interfaceToUse = _pluginInterface ?? Plugin.PluginInterface;
        interfaceToUse.SavePluginConfig(this);
    }
}
