using System.Collections.Generic;
using System.Linq;

namespace EmoteReactor.Reactions;

public class EmoteExecutor
{
    private readonly IClock _clock;
    private readonly IReactionPerformer _performer;
    private readonly object _lock = new();

    // Accessed from both the game hook's Enqueue path and the Framework.Update
    // path; these are the same thread in current Dalamud, but that is not a
    // documented guarantee, so we lock defensively.
    private readonly List<ScheduledReaction> _pending = new();

    public EmoteExecutor(IClock clock, IReactionPerformer performer)
    {
        _clock = clock;
        _performer = performer;
    }

    public void Enqueue(ScheduledReaction reaction)
    {
        lock (_lock)
        {
            _pending.Add(reaction);
        }
    }

    public void Update()
    {
        List<ScheduledReaction> due;
        lock (_lock)
        {
            var now = _clock.Now;
            due = _pending.Where(r => r.FireAt <= now).OrderBy(r => r.FireAt).ToList();
            foreach (var reaction in due)
                _pending.Remove(reaction);
        }

        // Perform outside the lock: Perform() sends game commands and must not run while holding it.
        foreach (var reaction in due)
        {
            _performer.Perform(reaction);
        }
    }
}
