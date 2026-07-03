using System.Collections.Generic;
using System.Linq;

namespace EmoteReactor.Reactions;

public class EmoteExecutor
{
    private readonly IClock _clock;
    private readonly IReactionPerformer _performer;
    private readonly List<ScheduledReaction> _pending = new();

    public EmoteExecutor(IClock clock, IReactionPerformer performer)
    {
        _clock = clock;
        _performer = performer;
    }

    public void Enqueue(ScheduledReaction reaction) => _pending.Add(reaction);

    public void Update()
    {
        var now = _clock.Now;
        var due = _pending.Where(r => r.FireAt <= now).OrderBy(r => r.FireAt).ToList();
        foreach (var reaction in due)
        {
            _pending.Remove(reaction);
            _performer.Perform(reaction);
        }
    }
}
