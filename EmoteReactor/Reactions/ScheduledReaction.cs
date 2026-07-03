using System;

namespace EmoteReactor.Reactions;

public record ScheduledReaction(ReactionRule Rule, ulong InstigatorObjectId, DateTime FireAt);
