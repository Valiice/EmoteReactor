using Dalamud.Plugin.Services;
using EmoteReactor.Emotes;
using EmoteReactor.Reactions;

namespace EmoteReactor.Game;

public sealed class GameReactionPerformer : IReactionPerformer
{
    private readonly ChatSender _chatSender;
    private readonly IEmoteCatalog _catalog;
    private readonly ITargetManager _targetManager;
    private readonly Config? _config;
    private readonly Diagnostics? _diagnostics;

    public GameReactionPerformer(ChatSender chatSender, IEmoteCatalog catalog, ITargetManager targetManager,
        Config? config = null, Diagnostics? diagnostics = null)
    {
        _chatSender = chatSender;
        _catalog = catalog;
        _targetManager = targetManager;
        _config = config;
        _diagnostics = diagnostics;
    }

    public void Perform(ScheduledReaction reaction)
    {
        var emote = _catalog.FindById(reaction.Rule.ReactionEmoteId);
        if (emote is null)
            return;

        _diagnostics?.IncrementReactionsFired();

        var previousTarget = _targetManager.Target;

        if (reaction.Rule.DirectAtInstigator)
        {
            var instigator = FindInstigator(reaction.InstigatorObjectId);
            if (instigator is not null)
                _targetManager.Target = instigator;
        }

        var command = reaction.Rule.MotionOnly ? $"{emote.Command} motion" : emote.Command;

        if (_config is { DebugLogging: true })
            Plugin.PluginLog.Information($"[EmoteReactor] Reacting: {command}");

        _chatSender.SendCommand(command);

        // NOTE: restoring on the same tick as the send. If in-game testing (Task 10) shows the
        // game reads the target asynchronously after the chat command is queued (rather than
        // synchronously when ProcessChatBox runs), this restore will need to move to a
        // one-shot "pending restore" applied on the next EmoteExecutor.Update() tick instead.
        if (reaction.Rule.DirectAtInstigator && reaction.Rule.RestorePreviousTarget)
            _targetManager.Target = previousTarget;
    }

    private Dalamud.Game.ClientState.Objects.Types.IGameObject? FindInstigator(ulong objectId)
    {
        foreach (var obj in Plugin.ObjectTable)
        {
            if (obj.GameObjectId == objectId)
                return obj;
        }
        return null;
    }
}
