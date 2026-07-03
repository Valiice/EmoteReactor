using EmoteReactor;
using FluentAssertions;

namespace EmoteReactor.Tests;

public class ReactionRuleTests
{
    [Fact]
    public void New_rule_has_unique_id_and_sane_defaults()
    {
        var a = new ReactionRule();
        var b = new ReactionRule();

        a.Id.Should().NotBe(Guid.Empty);
        a.Id.Should().NotBe(b.Id);
        a.Enabled.Should().BeTrue();
        a.RestorePreviousTarget.Should().BeTrue();
        a.MinDelaySeconds.Should().Be(0f);
        a.MaxDelaySeconds.Should().Be(0f);
    }

    [Fact]
    public void Config_starts_with_no_rules_and_zero_throttle()
    {
        var config = new Config();
        config.Rules.Should().BeEmpty();
        config.GlobalThrottleSeconds.Should().Be(0f);
    }
}
