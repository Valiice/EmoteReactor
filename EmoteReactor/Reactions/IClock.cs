using System;

namespace EmoteReactor.Reactions;

public interface IClock
{
    DateTime Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.UtcNow;
}
