using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using SimpleGlamourSwitcher.Configuration;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Windows;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher;

public class Plugin : IDalamudPlugin {

    private readonly WindowSystem windowSystem = new(nameof(SimpleGlamourSwitcher));
    public static readonly MainWindow MainWindow = new();
    public static readonly ConfigWindow ConfigWindow = new();
    
    
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
        
        UiBuilder.OpenMainUi += MainWindow.Toggle;
        UiBuilder.OpenConfigUi += ConfigWindow.Toggle;

        UiBuilder.Draw += windowSystem.Draw;
        windowSystem.AddWindow(MainWindow);
        windowSystem.AddWindow(ConfigWindow);

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
        }) { ShowInHelp = true, HelpMessage = "Open the Simple Glamour Switcher window."});
    }


    public void Dispose() {
        Commands.RemoveHandler("/sgs");
        MainWindow.IsOpen = false;
        ECommonsMain.Dispose();
        HotkeyHelper.Dispose();
        PluginState.Dispose();
        ActionQueue.Dispose();
        Config.Shutdown();
        
        ModManager.Dispose();
        
        PenumbraIpc.Dispose();


    }


    
    
}
