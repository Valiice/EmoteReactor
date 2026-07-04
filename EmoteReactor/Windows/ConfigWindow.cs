using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using EmoteReactor.Emotes;

namespace EmoteReactor.Windows;

public sealed class ConfigWindow : Window
{
    private readonly Config _config;
    private readonly IEmoteCatalog _catalog;
    private readonly Dictionary<Guid, string> _triggerSearch = new();
    private readonly Dictionary<Guid, string> _reactionSearch = new();

    public ConfigWindow(Config config, IEmoteCatalog catalog)
        : base("EmoteReactor Configuration###EmoteReactorConfig")
    {
        _config = config;
        _catalog = catalog;
        Size = new Vector2(720, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var enabled = _config.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled))
        {
            _config.Enabled = enabled;
            _config.Save();
        }

        var throttle = _config.GlobalThrottleSeconds;
        if (ImGui.SliderFloat("Global throttle (seconds)", ref throttle, 0f, 30f, "%.1f"))
        {
            _config.GlobalThrottleSeconds = Math.Max(0f, throttle);
            _config.Save();
        }

        ImGui.Separator();

        if (ImGui.Button("Add rule"))
        {
            _config.Rules.Add(new ReactionRule());
            _config.Save();
        }

        ReactionRule? toRemove = null;
        foreach (var rule in _config.Rules)
        {
            ImGui.PushID(rule.Id.ToString());
            DrawRule(rule, ref toRemove);
            ImGui.Separator();
            ImGui.PopID();
        }

        if (toRemove is not null)
        {
            _config.Rules.Remove(toRemove);
            _config.Save();
        }
    }

    private void DrawRule(ReactionRule rule, ref ReactionRule? toRemove)
    {
        var enabled = rule.Enabled;
        if (ImGui.Checkbox("On", ref enabled)) { rule.Enabled = enabled; _config.Save(); }

        ImGui.SameLine();
        DrawEmoteCombo("When someone does", rule.TriggerEmoteId, _triggerSearch, rule.Id,
            id => { rule.TriggerEmoteId = id; _config.Save(); });

        ImGui.SameLine();
        DrawEmoteCombo("I react with", rule.ReactionEmoteId, _reactionSearch, rule.Id,
            id => { rule.ReactionEmoteId = id; _config.Save(); });

        var motion = rule.MotionOnly;
        if (ImGui.Checkbox("Motion only", ref motion)) { rule.MotionOnly = motion; _config.Save(); }
        ImGui.SameLine();
        var direct = rule.DirectAtInstigator;
        if (ImGui.Checkbox("At them", ref direct)) { rule.DirectAtInstigator = direct; _config.Save(); }
        ImGui.SameLine();
        var restore = rule.RestorePreviousTarget;
        if (ImGui.Checkbox("Restore target", ref restore)) { rule.RestorePreviousTarget = restore; _config.Save(); }

        var cooldown = rule.CooldownSeconds;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputFloat("Cooldown (s)", ref cooldown)) { rule.CooldownSeconds = Math.Max(0f, cooldown); _config.Save(); }

        var min = rule.MinDelaySeconds;
        var max = rule.MaxDelaySeconds;
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputFloat("Delay min (s)", ref min)) { rule.MinDelaySeconds = Math.Max(0f, min); _config.Save(); }
        ImGui.SameLine();
        ImGui.SetNextItemWidth(120);
        if (ImGui.InputFloat("Delay max (s)", ref max)) { rule.MaxDelaySeconds = Math.Max(0f, max); _config.Save(); }

        if (ImGui.Button("Delete rule"))
            toRemove = rule;
    }

    private void DrawEmoteCombo(string label, uint currentId, Dictionary<Guid, string> searchState,
        Guid ruleId, Action<uint> onSelect)
    {
        var current = _catalog.FindById(currentId);
        var preview = current?.Name ?? "(none)";
        ImGui.SetNextItemWidth(200);
        if (!ImGui.BeginCombo(label, preview))
            return;

        if (!searchState.TryGetValue(ruleId, out var search))
            search = string.Empty;

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint($"##search{label}", "Search…", ref search, 64))
            searchState[ruleId] = search;

        foreach (var emote in _catalog.Search(search))
        {
            if (ImGui.Selectable(emote.Name, emote.Id == currentId))
                onSelect(emote.Id);
        }

        ImGui.EndCombo();
    }
}
