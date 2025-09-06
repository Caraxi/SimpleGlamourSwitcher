using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
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
        ImGui.SameLine();
        ImGui.TextDisabled("Set a hotkey to open the main UI.");

        PluginConfig.Dirty |= ImGui.Checkbox("Allow Hotkey in GPose", ref PluginConfig.AllowHotkeyInGpose);
        
        PluginConfig.Dirty |= ImGui.Checkbox("Fullscreen", ref PluginConfig.FullScreenMode);

        if (PluginConfig.FullScreenMode) {
            using (ImRaii.PushIndent()) {
                PluginConfig.Dirty |= ImGui.DragFloat2("Window Offset##fullscreenOffset", ref PluginConfig.FullscreenOffset);
                PluginConfig.Dirty |= ImGui.DragFloat2("Screen Padding##fullscreenPadding", ref PluginConfig.FullscreenPadding);
            }
        }
        
        
        PluginConfig.Dirty |= ImGui.Checkbox("Close window after applying outfit", ref PluginConfig.AutoCloseAfterApplying);
        PluginConfig.Dirty |= ImGui.Checkbox("Enable Outfit Commands", ref PluginConfig.EnableOutfitCommands);
        if (PluginConfig.EnableOutfitCommands) {
            using (ImRaii.PushIndent()) {
                PluginConfig.Dirty |= ImGui.Checkbox("Dry Run", ref PluginConfig.DryRunOutfitCommands);
                ImGui.SameLine();
                ImGuiComponents.HelpMarker("When enabled, commands will instead be printed to chatlog without being used.");
            }
        }
        PluginConfig.Dirty |= ImGui.Checkbox("Log actions to chat", ref PluginConfig.LogActionsToChat);
        PluginConfig.Dirty |= ImGui.Checkbox("Show current character on character list", ref PluginConfig.ShowActiveCharacterInCharacterList);
        PluginConfig.Dirty |= ImGui.Checkbox("Show icons on buttons", ref PluginConfig.ShowButtonIcons);
        
        #if DEBUG
        var debugPages = new[] { "none", "automation", "outfit" };
        var debugPage = debugPages.IndexOf(PluginConfig.DebugDefaultPage);
        if (debugPage < 0) {
            debugPage = 0;
            PluginConfig.DebugDefaultPage = debugPages[0];
        }
        if (ImGui.Combo("Debug: Startup Page", ref debugPage, debugPages, debugPages.Length)) {
            PluginConfig.Dirty = true;
            PluginConfig.DebugDefaultPage = debugPages[debugPage];
        }
        
        PluginConfig.Dirty |= ImGui.Checkbox("Open Debug Window at Startup", ref PluginConfig.OpenDebugOnStartup);
        
        #endif
        
    }
}
