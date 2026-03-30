using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Components;
using ECommons.ImGuiMethods;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.Utility;
using SixLabors.ImageSharp.Formats.Webp;

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
        PluginConfig.Dirty |= ImGuiEx.EnumCombo("Folder Display Order", ref PluginConfig.FolderSortStrategy, (e) => e != FolderSortStrategy.Inherit);
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
        PluginConfig.Dirty |= ImGui.Checkbox("Show shared folders on their own line", ref PluginConfig.SharedFoldersOnOwnLine);

        var useCustomFolderPolaroid = PluginConfig.CustomCharacterPolaroidStyle != null;

        using (ImRaii.Disabled(useCustomFolderPolaroid && !ImGui.GetIO().KeyShift)) {
            if (ImGui.Checkbox(useCustomFolderPolaroid ? "##useCustomCharacterPolaroid" : "Use custom character image style", ref useCustomFolderPolaroid)) {
                if (useCustomFolderPolaroid) {
                    PluginConfig.CustomCharacterPolaroidStyle = (PluginConfig.CustomStyle?.CharacterPolaroid ?? Style.Default.CharacterPolaroid).Clone();
                } else {
                    PluginConfig.CustomCharacterPolaroidStyle = null;
                }

                PluginConfig.Dirty = true;
            }
        }

        if (useCustomFolderPolaroid && !ImGui.GetIO().KeyShift && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Custom style will be lost if disabled.\nHold SHIFT to confirm.");
        }
        
        if (PluginConfig.CustomCharacterPolaroidStyle != null) {
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Custom Character Image Style")) {

                using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled))) {
                    ImGui.TextWrapped("Customize the display of the character switcher preview images. Changing this will cause all existing images to be stretched to the new size.");
                }
               
                using (ImRaii.PushIndent()) {
                    if (PolaroidStyle.DrawEditor("Character", PluginConfig.CustomCharacterPolaroidStyle)) {
                        PluginConfig.Dirty = true;
                    }
                }
            }
        }

        if (ImGui.CollapsingHeader("Animated 'Screenshot' Settings")) {

            using (ImRaii.PushIndent()) {
                ImGui.SetNextItemWidth(150);
                PluginConfig.Dirty |= ImGui.SliderFloat("Target Frame Rate", ref PluginConfig.AnimatedImageConfiguration.MaxFrameRate, 1, 60, flags: ImGuiSliderFlags.AlwaysClamp);
                PluginConfig.Dirty |= ImGui.Checkbox("Use Lossless Compression", ref PluginConfig.AnimatedImageConfiguration.UseLosslessCompression);
                ImGui.SetNextItemWidth(150);
                PluginConfig.Dirty |= ImGui.SliderInt("Compression Quality", ref PluginConfig.AnimatedImageConfiguration.CompressionQuality, 0, 100, flags: ImGuiSliderFlags.AlwaysClamp);



                var selectedEncodingValueNames = Common.GetEnumValueNames(PluginConfig.AnimatedImageConfiguration.EncodingMethod).ToArray();
                if (ImGui.BeginCombo("Encoding Method", selectedEncodingValueNames.Length > 1 ? $"{selectedEncodingValueNames[0]} ({string.Join(',', selectedEncodingValueNames[1..])})" : $"{selectedEncodingValueNames[0]}")) {
                    foreach (var f in Common.GetEnumValueNames<WebpEncodingMethod>()) {
                        var names = f.ToArray();
                        if (names.Length == 0) continue;
                        if (ImGui.Selectable(names.Length > 1 ? $"{names[0]} ({string.Join(',', names[1..])})" : $"{names[0]}", f.Key == PluginConfig.AnimatedImageConfiguration.EncodingMethod)) {
                            PluginConfig.AnimatedImageConfiguration.EncodingMethod = f.Key;
                            PluginConfig.Dirty = true;
                        }
                    }
                    
                    ImGui.EndCombo();
                }
            }
        }

        #if DEBUG
        var debugPages = new[] { "none", "automation", "outfit", "active" };
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
        
        DrawAutomaticModDetectionSettings();
        DrawEquippedWindowSettings();

    }

    private void DrawEquippedWindowSettings() {
        if (!ImGui.CollapsingHeader("Equipped Window Settings")) return;

        PluginConfig.Dirty |= ImGui.Checkbox("Show 'Save Outfit' Button", ref PluginConfig.EquippedWindowConfig.ShowSaveButton);
        
        ImGui.Text("Quick Switch Row Count:");
        using (ImRaii.PushIndent()) {
            foreach (var slot in Common.GetGearSlots()) {
                var c = PluginConfig.EquippedWindowConfig.QuickSwitchRowCount.GetValueOrDefault(slot, 1);
                if (ImGui.SliderInt($"{slot.ToName()}##rowCount", ref c, 1, EquippedWindowConfig.QuickSwitchMaxRows)) {
                    PluginConfig.EquippedWindowConfig.QuickSwitchRowCount[slot] = Math.Clamp(c, 1, EquippedWindowConfig.QuickSwitchMaxRows);
                    PluginConfig.Dirty = true;
                }
            }
        }
    }

    private readonly OutfitAppearance _appearance = new();
    private void DrawAutomaticModDetectionSettings() {
        if (!ImGui.CollapsingHeader("Automatic Mod Detection")) return;
        ImGuiExt.TextDisabledWrapped("Simple Glamour Switcher will try to detect mods automatically when creating new outfits.");
        using var _ = ImRaii.PushIndent();
        
        ImGui.Text("Enable Automatic Detection for Appearance:");
        using (ImRaii.PushIndent()) {
            foreach (var (customizeIndex, label) in CustomizeEditor.GetCustomizeTypes()) {
                if (_appearance[customizeIndex] is not IHasModConfigs) continue;
                if (customizeIndex is CustomizeIndex.Clan or CustomizeIndex.SkinColor) continue; // No Automatic Detection
                
                var e = !PluginConfig.DisableAutoModsCustomize.Contains(customizeIndex);
                if (ImGui.Checkbox($"{label}##autoModDetectCustomize_{customizeIndex}", ref e)) {
                    if (e) {
                        PluginConfig.DisableAutoModsCustomize.Remove(customizeIndex);
                    } else {
                        PluginConfig.DisableAutoModsCustomize.Add(customizeIndex);
                    }
                }
            }
        }
        
        ImGui.Text("Enable Automatic Detection for Equipment:");
        using (ImRaii.PushIndent()) {
            foreach (var slot in Common.GetGearSlots()) {
                var e = !PluginConfig.DisableAutoModsEquip.Contains(slot);
                if (ImGui.Checkbox($"{slot.ToName()}##autoModDetectEquip_{slot}", ref e)) {
                    if (e) {
                        PluginConfig.DisableAutoModsEquip.Remove(slot);
                    } else {
                        PluginConfig.DisableAutoModsEquip.Add(slot);
                    }
                }
            }

            foreach (var slot in Common.Set(EquipSlot.MainHand, EquipSlot.OffHand)) {
                var e = !PluginConfig.DisableAutoModsWeapons.Contains(slot);
                if (ImGui.Checkbox($"{slot.PrettyName()}##autoModDetectWeapon_{slot}", ref e)) {
                    if (e) {
                        PluginConfig.DisableAutoModsWeapons.Remove(slot);
                    } else {
                        PluginConfig.DisableAutoModsWeapons.Add(slot);
                    }
                }
            }
        }
    }
}
