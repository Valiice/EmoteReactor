using System;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Framework;

namespace EmoteReactor.Game;

/// <summary>
/// Interop shell that submits a chat command to the game exactly as if the player
/// typed it into the chatbox. Not unit-tested: verified by build + in-game (Task 10).
/// </summary>
public sealed unsafe class ChatSender
{
    // NOTE: verify these signatures against the current game version before shipping.
    private const string ProcessChatBoxSig =
        "48 89 5C 24 ?? 57 48 83 EC 20 48 8B FA 48 8B D9 45 84 C9";

    private delegate void ProcessChatBoxDelegate(IntPtr uiModule, IntPtr message, IntPtr unused, byte a4);

    private readonly IPluginLog _log;
    private readonly ProcessChatBoxDelegate? _processChatBox;

    public bool IsAvailable => _processChatBox is not null;

    public ChatSender(ISigScanner sigScanner, IPluginLog log)
    {
        _log = log;
        try
        {
            _processChatBox = Marshal.GetDelegateForFunctionPointer<ProcessChatBoxDelegate>(
                sigScanner.ScanText(ProcessChatBoxSig));
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "EmoteReactor: chat-send signatures failed to resolve; reactions disabled.");
        }
    }

    public void SendCommand(string command)
    {
        if (!IsAvailable)
            return;
        if (string.IsNullOrWhiteSpace(command) || command[0] != '/')
            return;

        var uiModule = (IntPtr)Framework.Instance()->GetUIModule();
        using var payload = new ChatPayload(command);
        var mem = Marshal.AllocHGlobal(400);
        try
        {
            Marshal.StructureToPtr(payload, mem, false);
            _processChatBox!(uiModule, mem, IntPtr.Zero, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(mem);
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    private readonly struct ChatPayload : IDisposable
    {
        [FieldOffset(0)] private readonly IntPtr textPtr;
        [FieldOffset(16)] private readonly ulong textLen;
        [FieldOffset(8)] private readonly ulong unk1;
        [FieldOffset(24)] private readonly ulong unk2;

        internal ChatPayload(string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            textPtr = Marshal.AllocHGlobal(bytes.Length + 30);
            Marshal.Copy(bytes, 0, textPtr, bytes.Length);
            Marshal.WriteByte(textPtr + bytes.Length, 0);
            textLen = (ulong)(bytes.Length + 1);
            unk1 = 64;
            unk2 = 0;
        }

        public void Dispose() => Marshal.FreeHGlobal(textPtr);
    }
}
