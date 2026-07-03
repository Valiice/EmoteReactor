using System;
using System.Collections.Generic;

namespace EmoteReactor.Reactions;

public class ReactionEngine
{
    private readonly Config _config;
    private readonly IClock _clock;
    private readonly IRandomProvider _random;

    private readonly Dictionary<(ulong Instigator, Guid RuleId), DateTime> _lastPerRule = new();
    private DateTime? _lastAnyReaction;

    public ReactionEngine(Config config, IClock clock, IRandomProvider random)
    {
        _config = config;
        _clock = clock;
        _random = random;
    }

    public IReadOnlyList<ScheduledReaction> Evaluate(EmoteEvent evt, ulong localPlayerObjectId)
    {
        var scheduled = new List<ScheduledReaction>();

        if (!_config.Enabled)
            return scheduled;
        if (evt.InstigatorObjectId == localPlayerObjectId)
            return scheduled; // never react to our own emote
        if (evt.TargetObjectId != localPlayerObjectId)
            return scheduled; // only directed-at-me

        var now = _clock.Now;

        if (_lastAnyReaction is { } lastAny
            && (now - lastAny).TotalSeconds < _config.GlobalThrottleSeconds)
            return scheduled; // global throttle

        foreach (var rule in _config.Rules)
        {
            if (!rule.Enabled || rule.TriggerEmoteId != evt.EmoteId)
                continue;

            var key = (evt.InstigatorObjectId, rule.Id);
            if (_lastPerRule.TryGetValue(key, out var last)
                && (now - last).TotalSeconds < rule.CooldownSeconds)
                continue; // per-rule, per-instigator cooldown

            var delay = PickDelay(rule);
            scheduled.Add(new ScheduledReaction(rule, evt.InstigatorObjectId, now.AddSeconds(delay)));
            _lastPerRule[key] = now;
            _lastAnyReaction = now;
        }

        return scheduled;
    }

    private double PickDelay(ReactionRule rule)
    {
        var min = Math.Max(0, rule.MinDelaySeconds);
        var max = Math.Max(min, rule.MaxDelaySeconds);
        if (max <= min)
            return min;
        return min + (_random.NextDouble() * (max - min));
    }
}
