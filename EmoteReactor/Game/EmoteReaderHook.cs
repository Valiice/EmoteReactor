using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using EmoteReactor.Reactions;

namespace EmoteReactor.Game;

/// <summary>
/// Interop shell that hooks the game's emote-receive function and raises
/// <see cref="OnEmote"/> when another object performs an emote targeting the
/// local player (or otherwise observed). Not unit-tested: verified by build +
/// in-game (Task 10).
/// </summary>
public sealed class EmoteReaderHook : IDisposable
{
    // NOTE: verify this signature against the current game version before shipping.
    // Known community "on emote" signature (EmoteReaderHooks-style).
    private const string OnEmoteSig =
        "E8 ?? ?? ?? ?? 48 8D 8B ?? ?? ?? ?? 4C 89 74 24 ?? E8 ?? ?? ?? ?? 48 8B 6C 24";

    private delegate void OnEmoteDelegate(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2);

    private readonly IObjectTable _objectTable;
    private readonly IPluginLog _log;
    private readonly Hook<OnEmoteDelegate>? _hook;

    public event Action<EmoteEvent>? OnEmote;
    public bool IsAvailable => _hook is { IsEnabled: true };

    public EmoteReaderHook(IGameInteropProvider interop, IObjectTable objectTable, IPluginLog log)
    {
        _objectTable = objectTable;
        _log = log;
        try
        {
            _hook = interop.HookFromSignature<OnEmoteDelegate>(OnEmoteSig, Detour);
            _hook.Enable();
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "EmoteReactor: emote hook signature failed to resolve; detection disabled.");
        }
    }

    private void Detour(ulong unk, ulong instigatorAddr, ushort emoteId, ulong targetId, ulong unk2)
    {
        try
        {
            // instigatorAddr points at the source GameObject; read its ObjectId via the object table.
            var instigator = _objectTable.CreateObjectReference((IntPtr)instigatorAddr);
            var instigatorId = instigator?.GameObjectId ?? 0UL;
            OnEmote?.Invoke(new EmoteEvent(instigatorId, emoteId, targetId));
        }
        catch (Exception ex)
        {
            _log.Error(ex, "EmoteReactor: error in emote detour.");
        }
        finally
        {
            _hook!.Original(unk, instigatorAddr, emoteId, targetId, unk2);
        }
    }

    public void Dispose()
    {
        _hook?.Disable();
        _hook?.Dispose();
    }
}
