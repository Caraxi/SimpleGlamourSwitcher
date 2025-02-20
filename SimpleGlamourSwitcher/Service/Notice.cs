namespace SimpleGlamourSwitcher.Service;

public static class Notice {
    public static void Show(string text) {
        PluginLog.Info(text);
        if (PluginConfig.LogActionsToChat) {
            Chat.Print(text, "Simple Glamour Switcher", 500);
        }
    }
}
