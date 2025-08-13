using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using SimpleGlamourSwitcher.Configuration;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Page;
using SimpleGlamourSwitcher.UserInterface.Windows;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher;

public class Plugin : IDalamudPlugin {
    private static readonly WindowSystem WindowSystem = new(nameof(SimpleGlamourSwitcher));
    public static readonly MainWindow MainWindow = new MainWindow().Enroll(WindowSystem);
    public static readonly ConfigWindow ConfigWindow = new ConfigWindow().Enroll(WindowSystem);
#if DEBUG
    public static readonly DebugWindow DebugWindow = new DebugWindow { IsOpen = true }.Enroll(WindowSystem);
#endif
    public Plugin(IDalamudPluginInterface pluginInterface) {
        (pluginInterface.Create<PluginService>() ?? throw new Exception("Failed to initialize PluginService")).Initialize();
#if DEBUG
        PluginLog.Debug("");
        PluginLog.Debug("");
        PluginLog.Debug("---------------------------------");
        PluginLog.Debug("- Loading SimpleGlamourSwitcher -");
        PluginLog.Debug("---------------------------------");
#endif

        try {
            var tempDir = Path.Join(PluginInterface.GetPluginConfigDirectory(), "temp");
            if (Directory.Exists(tempDir)) {
                Directory.Delete(tempDir, true);
            }
        } catch (Exception ex) {
            PluginLog.Error(ex, "Failed to delete temp dir");
        }

        ECommonsMain.Init(pluginInterface, this);
        HotkeyHelper.Initialize();
        PluginState.Initialize();
        ActionQueue.Initialize();
        AutomationManager.Initialize();

        UiBuilder.OpenMainUi += MainWindow.Toggle;
        UiBuilder.OpenConfigUi += ConfigWindow.Toggle;

        UiBuilder.Draw += WindowSystem.Draw;

        Commands.AddHandler("/sgs", new CommandInfo((_, args) => {
            switch (args.ToLowerInvariant()) {
                case "c":
                case "config":
                    ConfigWindow.Toggle();
                    break;
                default:
                    MainWindow.IsOpen = true;
                    break;
            }
        }) { ShowInHelp = true, HelpMessage = "Open the Simple Glamour Switcher window." });

        Framework.RunOnTick(() => {
            PluginState.LoadActiveCharacter(false, true);
#if DEBUG
            if (ClientState.IsLoggedIn) {
                OpenOnStartup();
            } else {
                ClientState.Login += OpenOnStartup;
            }
#endif
        }, delayTicks: 3);
        
    }
    
    #if DEBUG
    private void OpenOnStartup() {
        ClientState.Login -= OpenOnStartup;
        Framework.RunOnTick(() => {
            if (ActiveCharacter != null && ClientState.LocalContentId != 0) {
                switch (PluginConfig.DebugDefaultPage.ToLowerInvariant()) {
                    case "none":
                        break;
                    case "automation":
                        MainWindow.IsOpen = true;
                        MainWindow.OpenPage(new AutomationPage(ActiveCharacter));
                        break;
                    case "outfit":
                        MainWindow.IsOpen = true;
                        MainWindow.OpenPage(new EditOutfitPage(ActiveCharacter, Guid.Empty, null));
                        break;
                }
            }
        }, delayTicks: 1);
    }
    #endif

    public void Dispose() {
        Commands.RemoveHandler("/sgs");
        MainWindow.IsOpen = false;
        AutomationManager.Dispose();
        ECommonsMain.Dispose();
        HotkeyHelper.Dispose();
        PluginState.Dispose();
        ActionQueue.Dispose();
        Config.Shutdown();
        PenumbraIpc.Dispose();
        PluginService.Dispose();
    }
}
