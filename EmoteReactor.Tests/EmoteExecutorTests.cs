using EmoteReactor;
using EmoteReactor.Reactions;
using FluentAssertions;

namespace EmoteReactor.Tests;

public class EmoteExecutorTests
{
    private sealed class FakeClock : IClock
    {
        public DateTime Now { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    private sealed class RecordingPerformer : IReactionPerformer
    {
        public List<ScheduledReaction> Performed { get; } = new();
        public void Perform(ScheduledReaction reaction) => Performed.Add(reaction);
    }

    private static ScheduledReaction At(DateTime when, uint reactionEmote = 1) =>
        new(new ReactionRule { ReactionEmoteId = reactionEmote }, 200, when);

    [Fact]
    public void Does_not_perform_reactions_before_their_fire_time()
    {
        var clock = new FakeClock();
        var performer = new RecordingPerformer();
        var executor = new EmoteExecutor(clock, performer);

        executor.Enqueue(At(clock.Now.AddSeconds(5)));
        executor.Update();

        performer.Performed.Should().BeEmpty();
    }

    [Fact]
    public void Performs_reaction_once_fire_time_reached()
    {
        var clock = new FakeClock();
        var performer = new RecordingPerformer();
        var executor = new EmoteExecutor(clock, performer);

        executor.Enqueue(At(clock.Now.AddSeconds(5)));
        clock.Now = clock.Now.AddSeconds(5);
        executor.Update();

        performer.Performed.Should().ContainSingle();
    }

    [Fact]
    public void Performs_each_reaction_only_once()
    {
        var clock = new FakeClock();
        var performer = new RecordingPerformer();
        var executor = new EmoteExecutor(clock, performer);

        executor.Enqueue(At(clock.Now));
        executor.Update();
        executor.Update();

        performer.Performed.Should().ContainSingle();
    }

    [Fact]
    public void Performs_due_reactions_in_fire_time_order()
    {
        var clock = new FakeClock();
        var performer = new RecordingPerformer();
        var executor = new EmoteExecutor(clock, performer);

        executor.Enqueue(At(clock.Now.AddSeconds(2), reactionEmote: 20));
        executor.Enqueue(At(clock.Now.AddSeconds(1), reactionEmote: 10));
        clock.Now = clock.Now.AddSeconds(5);
        executor.Update();

        performer.Performed.Select(r => r.Rule.ReactionEmoteId)
            .Should().ContainInOrder(10u, 20u);
    }
}
