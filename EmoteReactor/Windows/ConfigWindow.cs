using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using EmoteReactor.Emotes;

namespace EmoteReactor.Windows;

public sealed class ConfigWindow : Window
{
    private readonly Config _config;
    private readonly IEmoteCatalog _catalog;
    private readonly ITextureProvider _textureProvider;
    private readonly Diagnostics _diagnostics;
    private readonly Dictionary<Guid, string> _triggerSearch = new();
    private readonly Dictionary<Guid, string> _reactionSearch = new();

    public ConfigWindow(Config config, IEmoteCatalog catalog, ITextureProvider textureProvider, Diagnostics diagnostics)
        : base("EmoteReactor Configuration###EmoteReactorConfig")
    {
        _config = config;
        _catalog = catalog;
        _textureProvider = textureProvider;
        _diagnostics = diagnostics;
        Size = new Vector2(720, 500);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        DrawDiagnosticsPanel();
        ImGui.Separator();

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

    private void DrawDiagnosticsPanel()
    {
        if (_diagnostics.DetectionAvailable)
            ImGui.TextColored(new Vector4(0.2f, 0.85f, 0.2f, 1f), "Emote detection: active");
        else
            ImGui.TextColored(new Vector4(0.95f, 0.25f, 0.25f, 1f), "Emote detection: SIGNATURE FAILED (won't detect emotes)");

        if (_diagnostics.SendingAvailable)
            ImGui.TextColored(new Vector4(0.2f, 0.85f, 0.2f, 1f), "Emote sending: active");
        else
            ImGui.TextColored(new Vector4(0.95f, 0.25f, 0.25f, 1f), "Emote sending: SIGNATURE FAILED (won't emote back)");

        ImGui.Text($"Emotes seen: {_diagnostics.EmotesSeen}  ·  Directed at you: {_diagnostics.DirectedAtYou}  ·  Reactions fired: {_diagnostics.ReactionsFired}");

        var debugLogging = _config.DebugLogging;
        if (ImGui.Checkbox("Debug logging (to /xllog)", ref debugLogging))
        {
            _config.DebugLogging = debugLogging;
            _config.Save();
        }
    }

    private void DrawRule(ReactionRule rule, ref ReactionRule? toRemove)
    {
        var enabled = rule.Enabled;
        if (ImGui.Checkbox("Enabled", ref enabled)) { rule.Enabled = enabled; _config.Save(); }

        ImGui.SameLine();
        DrawEmoteCombo("When someone does", "trigger", rule.TriggerEmoteId, _triggerSearch, rule.Id,
            id => { rule.TriggerEmoteId = id; _config.Save(); },
            "The emote another player must perform on you.");

        ImGui.SameLine();
        DrawEmoteCombo("I react with", "reaction", rule.ReactionEmoteId, _reactionSearch, rule.Id,
            id => { rule.ReactionEmoteId = id; _config.Save(); },
            "The emote you automatically perform back.");

        var motion = rule.MotionOnly;
        if (ImGui.Checkbox("Motion only", ref motion)) { rule.MotionOnly = motion; _config.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Play just the animation, with no chat text line.");
        ImGui.SameLine();
        var direct = rule.DirectAtInstigator;
        if (ImGui.Checkbox("At them", ref direct)) { rule.DirectAtInstigator = direct; _config.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Target the person and emote AT them. Off = just perform the emote without targeting.");
        ImGui.SameLine();
        var restore = rule.RestorePreviousTarget;
        if (ImGui.Checkbox("Restore target", ref restore)) { rule.RestorePreviousTarget = restore; _config.Save(); }
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("After reacting, re-select whoever you had targeted before.");

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

    private void DrawEmoteCombo(string leadingText, string comboKind, uint currentId, Dictionary<Guid, string> searchState,
        Guid ruleId, Action<uint> onSelect, string? tooltip = null)
    {
        ImGui.Text(leadingText);
        if (tooltip is not null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        ImGui.SameLine();

        var current = _catalog.FindById(currentId);
        var preview = current?.Name ?? "(none)";
        var iconSize = ImGui.GetTextLineHeight();

        DrawEmoteIcon(current?.IconId ?? 0, iconSize);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (!ImGui.BeginCombo($"##{comboKind}{ruleId}", preview))
            return;

        if (!searchState.TryGetValue(ruleId, out var search))
            search = string.Empty;

        ImGui.SetNextItemWidth(-1);
        if (ImGui.InputTextWithHint($"##search{comboKind}{ruleId}", "Search…", ref search, 64))
            searchState[ruleId] = search;

        foreach (var emote in _catalog.Search(search))
        {
            DrawEmoteIcon(emote.IconId, iconSize);
            ImGui.SameLine();
            if (ImGui.Selectable(emote.Name, emote.Id == currentId))
                onSelect(emote.Id);
        }

        ImGui.EndCombo();
    }

    private void DrawEmoteIcon(uint iconId, float size)
    {
        var iconVec = new Vector2(size, size);

        if (iconId == 0)
        {
            ImGui.Dummy(iconVec);
            return;
        }

        var wrap = _textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrDefault();
        if (wrap is null)
        {
            ImGui.Dummy(iconVec);
            return;
        }

        ImGui.Image(wrap.Handle, iconVec);
    }
}
