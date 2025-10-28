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
    public static readonly DebugWindow DebugWindow = new DebugWindow().Enroll(WindowSystem);
    public static readonly ScreenshotWindow ScreenshotWindow = new ScreenshotWindow() { IsOpen = true}.Enroll(WindowSystem);
    
    
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

        PluginUi.OpenMainUi += MainWindow.Toggle;
        PluginUi.OpenConfigUi += ConfigWindow.Toggle;

        PluginUi.Draw += WindowSystem.Draw;

        Commands.AddHandler("/sgs", new CommandInfo((_, args) => {
            var splitArgs = args.Split(' ', StringSplitOptions.TrimEntries);
            switch (splitArgs[0].ToLowerInvariant()) {
                case "c":
                case "config":
                    ConfigWindow.Toggle();
                    break;
                case "debug":
                    DebugWindow.Toggle();
                    break;
                case "apply":
                    ProcessApplyCommand(splitArgs[1..]).ConfigureAwait(false);
                    break;
                case "open":
                    ProcessOpenCommand(splitArgs[1..]);
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

            if (PluginConfig.OpenDebugOnStartup) {
                DebugWindow.IsOpen = true;
            }
#endif
        }, delayTicks: 3);
        
    }

    private void ProcessOpenCommand(string[] args) {
        if (args.Length == 0) {
            Chat.PrintError("/sgs open [GUID]", "SimpleGlamourSwitcher");
            return;
        }

        if (ActiveCharacter == null) {
            Chat.PrintError("No Character Active", "SimpleGlamourSwitcher");
            return;
        }
        
        if (!Guid.TryParse(args[0], out var guid)) {
            Chat.PrintError($"[{args[0]}] is not a valid GUID", "SimpleGlamourSwitcher");
            return;
        }

        if (ActiveCharacter.Folders.ContainsKey(guid)) {

            if (MainWindow is { IsOpen: true, RootPage: GlamourListPage glp } && glp.ActiveFolder == guid) {
                MainWindow.PopPage();
            } else {
                MainWindow.IsOpen = true;
                MainWindow.OpenPage(new GlamourListPage(guid, true), true);
            }
            
            
            
        } else {
            Chat.PrintError($"[{args[0]}] is not a valid folder.", "SimpleGlamourSwitcher");
        }
    }

    private async Task ProcessApplyCommand(string[] args) {
        if (args.Length == 0) {
            Chat.PrintError("/sgs apply [GUID]", "SimpleGlamourSwitcher");
            return;
        }

        if (ActiveCharacter == null) {
            Chat.PrintError("No Character Active", "SimpleGlamourSwitcher");
            return;
        }


        if (!Guid.TryParse(args[0], out var guid)) {
            Chat.PrintError($"[{args[0]}] is not a valid GUID", "SimpleGlamourSwitcher");
            return;
        }
        
        var entries = await ActiveCharacter.GetEntries();

        if (entries.TryGetValue(guid, out var entry)) {
            await entry.Apply();
        }
        else {
            Chat.PrintError($"[{args[0]}] was not found.", "SimpleGlamourSwitcher");
        }
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
