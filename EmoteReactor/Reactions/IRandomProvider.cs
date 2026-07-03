using System;

namespace EmoteReactor.Reactions;

public interface IRandomProvider
{
    double NextDouble();
}

public sealed class SystemRandomProvider : IRandomProvider
{
    private readonly Random _random = new();
    public double NextDouble() => _random.NextDouble();
}
