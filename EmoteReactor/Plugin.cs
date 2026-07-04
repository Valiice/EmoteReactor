using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using EmoteReactor.Emotes;
using EmoteReactor.Game;
using EmoteReactor.Reactions;
using EmoteReactor.Windows;

namespace EmoteReactor;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;
    [PluginService] internal static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static INotificationManager NotificationManager { get; private set; } = null!;
    [PluginService] internal static ISigScanner SigScanner { get; private set; } = null!;

    private const string CommandName = "/emotereactor";

    public Config Config { get; init; }

    public readonly WindowSystem WindowSystem = new("EmoteReactor");
    private ConfigWindow ConfigWindow { get; init; } = null!;

    private readonly IEmoteCatalog _catalog;
    private readonly ChatSender _chatSender;
    private readonly EmoteReaderHook _emoteReaderHook;
    private readonly ReactionEngine _engine;
    private readonly EmoteExecutor _executor;

    public Plugin()
    {
        Config = PluginInterface.GetPluginConfig() as Config ?? new Config();
        Config.Initialize(PluginInterface);

        _catalog = EmoteCatalogFactory.Build(DataManager);

        ConfigWindow = new ConfigWindow(Config, _catalog);
        WindowSystem.AddWindow(ConfigWindow);
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += () => ConfigWindow.IsOpen = true;

        _chatSender = new ChatSender(SigScanner, PluginLog);

        var clock = new SystemClock();
        _engine = new ReactionEngine(Config, clock, new SystemRandomProvider());
        var performer = new GameReactionPerformer(_chatSender, _catalog, TargetManager);
        _executor = new EmoteExecutor(clock, performer);

        _emoteReaderHook = new EmoteReaderHook(GameInteropProvider, ObjectTable, PluginLog);
        _emoteReaderHook.OnEmote += HandleEmote;

        Framework.Update += OnFrameworkUpdate;

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Open the EmoteReactor configuration window.",
        });
    }

    private void HandleEmote(EmoteEvent evt)
    {
        var localPlayer = ObjectTable.LocalPlayer;
        if (localPlayer is null)
            return;

        foreach (var scheduled in _engine.Evaluate(evt, localPlayer.GameObjectId))
            _executor.Enqueue(scheduled);
    }

    private void OnFrameworkUpdate(IFramework framework) => _executor.Update();

    private void OnCommand(string command, string args) => ConfigWindow.IsOpen = true;

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;
        _emoteReaderHook.OnEmote -= HandleEmote;
        _emoteReaderHook.Dispose();
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        WindowSystem.RemoveAllWindows();
        CommandManager.RemoveHandler(CommandName);
    }
}
