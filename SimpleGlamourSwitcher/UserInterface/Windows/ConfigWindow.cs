using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
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
        ImGui.TextDisabled("Set a hotkey to open the main UI.");
        
        PluginConfig.Dirty |= ImGui.Checkbox("Fullscreen", ref PluginConfig.FullScreenMode);

        if (PluginConfig.FullScreenMode) {
            using (ImRaii.PushIndent()) {
                PluginConfig.Dirty |= ImGui.DragFloat2("Window Offset##fullscreenOffset", ref PluginConfig.FullscreenOffset);
                PluginConfig.Dirty |= ImGui.DragFloat2("Screen Padding##fullscreenPadding", ref PluginConfig.FullscreenPadding);
            }
        }
        
        
        PluginConfig.Dirty |= ImGui.Checkbox("Close window after applying outfit", ref PluginConfig.AutoCloseAfterApplying);
        PluginConfig.Dirty |= ImGui.Checkbox("Log actions to chat", ref PluginConfig.LogActionsToChat);
        PluginConfig.Dirty |= ImGui.Checkbox("Show current character on character list", ref PluginConfig.ShowActiveCharacterInCharacterList);
    }
}
