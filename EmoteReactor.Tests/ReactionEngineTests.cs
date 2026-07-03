using EmoteReactor;
using EmoteReactor.Reactions;
using FluentAssertions;

namespace EmoteReactor.Tests;

public class ReactionEngineTests
{
    private const ulong Me = 100;
    private const ulong Them = 200;

    private sealed class FakeClock : IClock
    {
        public DateTime Now { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class FakeRandom : IRandomProvider
    {
        public double Value { get; set; } = 0.5;
        public double NextDouble() => Value;
    }

    private static (Config, FakeClock, FakeRandom, ReactionEngine) Build(Action<Config>? configure = null)
    {
        var config = new Config();
        configure?.Invoke(config);
        var clock = new FakeClock();
        var random = new FakeRandom();
        return (config, clock, random, new ReactionEngine(config, clock, random));
    }

    private static ReactionRule Rule(uint trigger, uint reaction, Action<ReactionRule>? tweak = null)
    {
        var r = new ReactionRule { TriggerEmoteId = trigger, ReactionEmoteId = reaction };
        tweak?.Invoke(r);
        return r;
    }

    [Fact]
    public void Matches_rule_when_emote_is_directed_at_me()
    {
        var (_, _, _, engine) = Build(c => c.Rules.Add(Rule(5, 9)));
        var result = engine.Evaluate(new EmoteEvent(Them, 5, Me), Me);
        result.Should().ContainSingle();
        result[0].Rule.ReactionEmoteId.Should().Be(9);
        result[0].InstigatorObjectId.Should().Be(Them);
    }

    [Fact]
    public void Ignores_emote_not_targeted_at_me()
    {
        var (_, _, _, engine) = Build(c => c.Rules.Add(Rule(5, 9)));
        engine.Evaluate(new EmoteEvent(Them, 5, 999), Me).Should().BeEmpty();
    }

    [Fact]
    public void Ignores_my_own_emote()
    {
        var (_, _, _, engine) = Build(c => c.Rules.Add(Rule(5, 9)));
        engine.Evaluate(new EmoteEvent(Me, 5, Me), Me).Should().BeEmpty();
    }

    [Fact]
    public void Ignores_when_master_disabled()
    {
        var (_, _, _, engine) = Build(c => { c.Enabled = false; c.Rules.Add(Rule(5, 9)); });
        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me).Should().BeEmpty();
    }

    [Fact]
    public void Ignores_disabled_rule()
    {
        var (_, _, _, engine) = Build(c => c.Rules.Add(Rule(5, 9, r => r.Enabled = false)));
        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me).Should().BeEmpty();
    }

    [Fact]
    public void Per_rule_cooldown_blocks_same_instigator_repeat()
    {
        var (_, clock, _, engine) = Build(c => c.Rules.Add(Rule(5, 9, r => r.CooldownSeconds = 10)));

        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me).Should().ContainSingle();
        clock.Now = clock.Now.AddSeconds(5);
        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me).Should().BeEmpty(); // still on cooldown
        clock.Now = clock.Now.AddSeconds(6);
        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me).Should().ContainSingle(); // cooldown elapsed
    }

    [Fact]
    public void Cooldown_is_tracked_per_instigator()
    {
        var (_, _, _, engine) = Build(c => c.Rules.Add(Rule(5, 9, r => r.CooldownSeconds = 10)));
        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me).Should().ContainSingle();
        engine.Evaluate(new EmoteEvent(300, 5, Me), Me).Should().ContainSingle(); // different person, not blocked
    }

    [Fact]
    public void Global_throttle_blocks_any_second_reaction_within_window()
    {
        var (_, clock, _, engine) = Build(c =>
        {
            c.GlobalThrottleSeconds = 3;
            c.Rules.Add(Rule(5, 9));
            c.Rules.Add(Rule(6, 8));
        });

        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me).Should().ContainSingle();
        clock.Now = clock.Now.AddSeconds(1);
        engine.Evaluate(new EmoteEvent(Them, 6, Me), Me).Should().BeEmpty(); // throttled
        clock.Now = clock.Now.AddSeconds(3);
        engine.Evaluate(new EmoteEvent(Them, 6, Me), Me).Should().ContainSingle();
    }

    [Fact]
    public void FireAt_uses_random_within_delay_window()
    {
        var (_, clock, random, engine) = Build(c =>
            c.Rules.Add(Rule(5, 9, r => { r.MinDelaySeconds = 2; r.MaxDelaySeconds = 4; })));
        random.Value = 0.5; // midpoint -> +3s

        var result = engine.Evaluate(new EmoteEvent(Them, 5, Me), Me);
        result[0].FireAt.Should().Be(clock.Now.AddSeconds(3));
    }

    [Fact]
    public void FireAt_is_now_when_no_delay_configured()
    {
        var (_, clock, _, engine) = Build(c => c.Rules.Add(Rule(5, 9)));
        engine.Evaluate(new EmoteEvent(Them, 5, Me), Me)[0].FireAt.Should().Be(clock.Now);
    }
}
