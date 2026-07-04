using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using EmoteReactor.Reactions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;

namespace EmoteReactor.Game;

/// <summary>
/// Interop shell that hooks <see cref="EmoteController.PlayEmote"/> and raises
/// <see cref="OnEmote"/> when another object performs an emote targeting the
/// local player (or otherwise observed). The hook target is a ClientStructs
/// member function resolved by its own auto-maintained signature, so this
/// class no longer hand-carries a signature of its own. Not unit-tested:
/// verified by build + in-game (Task 10).
/// </summary>
public sealed unsafe class EmoteReaderHook : IDisposable
{
    private unsafe delegate bool PlayEmoteDelegate(
        EmoteController* thisPtr, uint emoteId, EmoteController.PlayEmoteOption* options);

    private readonly IPluginLog _log;
    private readonly Hook<PlayEmoteDelegate>? _hook;

    public event Action<EmoteEvent>? OnEmote;
    public bool IsAvailable => _hook is { IsEnabled: true };

    public EmoteReaderHook(IGameInteropProvider interop, IPluginLog log)
    {
        _log = log;
        try
        {
            var playEmoteAddr = (nint)EmoteController.MemberFunctionPointers.PlayEmote;
            if (playEmoteAddr == 0)
                throw new InvalidOperationException("EmoteController.PlayEmote signature not found.");

            _hook = interop.HookFromAddress<PlayEmoteDelegate>(playEmoteAddr, Detour);
            _hook.Enable();
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "EmoteReactor: emote hook failed to install; detection disabled.");
        }
    }

    private bool Detour(EmoteController* thisPtr, uint emoteId, EmoteController.PlayEmoteOption* options)
    {
        try
        {
            var instigator = thisPtr->OwnerObject;
            ulong instigatorId = instigator != null ? instigator->GetGameObjectId() : 0UL;
            ulong targetId = options != null ? options->TargetId : thisPtr->Target;
            OnEmote?.Invoke(new EmoteEvent(instigatorId, emoteId, targetId));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "EmoteReactor: error in emote detour.");
        }

        // Always call through to the original, regardless of whether our own
        // handling above threw, so the game's emote flow is never broken by us.
        return _hook!.Original(thisPtr, emoteId, options);
    }

    public void Dispose()
    {
        _hook?.Disable();
        _hook?.Dispose();
    }
}
