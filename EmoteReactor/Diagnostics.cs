namespace EmoteReactor;

/// <summary>Live pipeline status + counters shown in the config window's debug panel.</summary>
public sealed class Diagnostics
{
    public bool DetectionAvailable { get; set; }
    public bool SendingAvailable { get; set; }
    private int _emotesSeen;
    private int _directedAtYou;
    private int _reactionsFired;
    public int EmotesSeen => _emotesSeen;
    public int DirectedAtYou => _directedAtYou;
    public int ReactionsFired => _reactionsFired;
    public void IncrementEmotesSeen() => System.Threading.Interlocked.Increment(ref _emotesSeen);
    public void IncrementDirectedAtYou() => System.Threading.Interlocked.Increment(ref _directedAtYou);
    public void IncrementReactionsFired() => System.Threading.Interlocked.Increment(ref _reactionsFired);
}
