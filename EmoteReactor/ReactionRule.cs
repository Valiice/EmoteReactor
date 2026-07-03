using System;

namespace EmoteReactor;

[Serializable]
public class ReactionRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public uint TriggerEmoteId { get; set; }
    public uint ReactionEmoteId { get; set; }

    public bool Enabled { get; set; } = true;
    public bool MotionOnly { get; set; }
    public bool DirectAtInstigator { get; set; } = true;
    public bool RestorePreviousTarget { get; set; } = true;

    public float CooldownSeconds { get; set; }
    public float MinDelaySeconds { get; set; }
    public float MaxDelaySeconds { get; set; }
}
