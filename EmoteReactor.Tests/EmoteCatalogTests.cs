using EmoteReactor.Emotes;
using FluentAssertions;

namespace EmoteReactor.Tests;

public class EmoteCatalogTests
{
    private static EmoteCatalog Build() => new(new[]
    {
        new EmoteInfo(1, "Pet", "/pet", 100),
        new EmoteInfo(2, "Hug", "/hug", 101),
        new EmoteInfo(3, "Dote", "/dote", 102),
        new EmoteInfo(4, "", "", 0), // nameless/commandless rows must be filtered out
    });

    [Fact]
    public void All_excludes_rows_without_name_or_command()
    {
        Build().All.Select(e => e.Id).Should().BeEquivalentTo(new uint[] { 1, 2, 3 });
    }

    [Fact]
    public void FindById_returns_matching_emote_or_null()
    {
        var catalog = Build();
        catalog.FindById(2)!.Name.Should().Be("Hug");
        catalog.FindById(999).Should().BeNull();
    }

    [Fact]
    public void Search_is_case_insensitive_substring_over_name()
    {
        Build().Search("o").Select(e => e.Name).Should().BeEquivalentTo("Dote");
    }

    [Fact]
    public void Search_empty_query_returns_all()
    {
        Build().Search("  ").Should().HaveCount(3);
    }
}
