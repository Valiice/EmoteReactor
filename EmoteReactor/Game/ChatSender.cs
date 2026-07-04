using System;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace EmoteReactor.Game;

/// <summary>
/// Interop shell that submits a chat command to the game exactly as if the player
/// typed it into the chatbox. Delegates the actual memory interop to ECommons
/// (<see cref="ECommons.Automation.Chat"/>), which sends via ClientStructs'
/// auto-maintained <c>UIModule.ProcessChatBoxEntry</c>/<c>Utf8String.SanitizeString</c>
/// member functions instead of a hand-copied signature. Not unit-tested: verified by
/// build + in-game (Task 10).
/// </summary>
public sealed unsafe class ChatSender
{
    private readonly IPluginLog _log;

    public bool IsAvailable { get; }

    public ChatSender(IPluginLog log)
    {
        _log = log;
        try
        {
            // ECommons.Automation.Chat.SendMessage (used by ExecuteCommand) ultimately
            // calls these two ClientStructs member functions. Probe both so the
            // availability check fails fast if either signature can't resolve on the
            // current game patch, rather than only discovering it on first send.
            if (UIModule.MemberFunctionPointers.ProcessChatBoxEntry == null)
                throw new InvalidOperationException("UIModule.ProcessChatBoxEntry signature not found.");
            if (Utf8String.MemberFunctionPointers.SanitizeString == null)
                throw new InvalidOperationException("Utf8String.SanitizeString signature not found.");

            IsAvailable = true;
        }
        catch (Exception ex)
        {
            IsAvailable = false;
            _log.Warning(ex, "EmoteReactor: chat-send signatures failed to resolve; reactions disabled.");
        }
    }

    public void SendCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command) || command[0] != '/')
            return;

        try
        {
            ECommons.Automation.Chat.ExecuteCommand(command);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "EmoteReactor: failed to send chat command '{0}'.", command);
        }
    }
}
