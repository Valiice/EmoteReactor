namespace EmoteReactor.Reactions;

public interface IReactionPerformer
{
    void Perform(ScheduledReaction reaction);
}
