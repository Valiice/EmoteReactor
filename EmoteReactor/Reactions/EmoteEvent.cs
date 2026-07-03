namespace EmoteReactor.Reactions;

public readonly record struct EmoteEvent(ulong InstigatorObjectId, uint EmoteId, ulong TargetObjectId);
