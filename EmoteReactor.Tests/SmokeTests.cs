using EmoteReactor;
using FluentAssertions;

namespace EmoteReactor.Tests;

public class SmokeTests
{
    [Fact]
    public void Config_defaults_to_enabled()
    {
        var config = new Config();
        config.Enabled.Should().BeTrue();
    }
}
