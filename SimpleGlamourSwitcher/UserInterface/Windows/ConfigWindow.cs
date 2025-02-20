using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Windows;

public class ConfigWindow : Window {

    public ConfigWindow() : base("Config | Simple Glamour Switcher") {
        SizeConstraints = new WindowSizeConstraints() {
            MinimumSize = new Vector2(640, 400),
            MaximumSize = new Vector2(640, 4000)
        };
    }
    
    public override void Draw() {
        PluginConfig.Dirty = ImGui.ColorEdit4("Background Colour", ref PluginConfig.BackgroundColour);
        if (HotkeyHelper.DrawHotkeyConfigEditor("Hotkey", PluginConfig.Hotkey, out var newHotkey)) {
            PluginConfig.Dirty = true;
            PluginConfig.Hotkey = newHotkey;
        }


        PluginConfig.Dirty |= ImGui.Checkbox("Fullscreen", ref PluginConfig.FullScreenMode);

        if (PluginConfig.FullScreenMode) {
            using (ImRaii.PushIndent()) {
                PluginConfig.Dirty |= ImGui.DragFloat2("Window Offset##fullscreenOffset", ref PluginConfig.FullscreenOffset);
                PluginConfig.Dirty |= ImGui.DragFloat2("Screen Padding##fullscreenPadding", ref PluginConfig.FullscreenPadding);
            }
        }
        
        
        PluginConfig.Dirty |= ImGui.Checkbox("Close window after applying outfit", ref PluginConfig.AutoCloseAfterApplying);
        PluginConfig.Dirty |= ImGui.Checkbox("Log actions to chat", ref PluginConfig.LogActionsToChat);
    }
}
