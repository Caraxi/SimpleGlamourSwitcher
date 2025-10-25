using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditFolderPage(Guid parentFolder, CharacterFolder? editFolder) : Page {
    private bool dirty;
    
    private readonly CharacterFolder? newFolder = editFolder == null ? new CharacterFolder() { FolderGuid = Guid.NewGuid(), ConfigFile = ActiveCharacter } : null;
    
    
    private readonly string folderPath = ActiveCharacter?.ParseFolderPath(parentFolder) ?? throw new Exception("No Active Character");
    private string folderName = editFolder?.Name ?? string.Empty;
    private const float SubWindowWidth = 500f;
    private bool focusName = editFolder == null;

    private PolaroidStyle? outfitStyle = editFolder?.OutfitPolaroidStyle.Clone();
    private PolaroidStyle? folderStyle = editFolder?.FolderPolaroidStyle.Clone();

    private HashSet<CustomizeIndex>? defaultAppearanceToggles = editFolder?.CustomDefaultEnabledCustomizeIndexes.Clone();
    private HashSet<HumanSlot>? defaultEquipmentToggles = editFolder?.CustomDefaultDisabledEquipmentSlots.Clone();
    private HashSet<AppearanceParameterKind>? defaultParameterToggles = editFolder?.CustomDefaultEnabledParameterKinds.Clone();
    private HashSet<ToggleType>? defaultToggleTypes = editFolder?.CustomDefaultEnabledToggles.Clone();
    private CharacterFolder.DefaultLinks? defaultLinks = editFolder?.CustomDefaultLinks.Clone();
    private List<AutoCommandEntry> autoCommandBeforeOutfit = editFolder?.AutoCommandBeforeOutfit.Clone() ?? [];
    private List<AutoCommandEntry> autoCommandAfterOutfit = editFolder?.AutoCommandAfterOutfit.Clone() ?? [];
    private bool autoCommandsSkipCharacter = editFolder?.AutoCommandsSkipCharacter ?? false;
    
    private bool hidden = editFolder?.Hidden ?? false;
    private CharacterFolder? folder = editFolder;

    private OutfitLinksEditor? outfitLinksEditor;

    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(folder == null ? "Creating" : "Editing Folder", shadowed: true);
        ImGuiExt.CenterText(folder == null ? $"New Folder in {folderPath}" : $"{folderPath} / {folder.Name}", shadowed: true);
    }
    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        if (ActiveCharacter == null) return;
        if (dirty) controlFlags |= WindowControlFlags.PreventClose;
        
        var w = MathF.Min(500, ImGui.GetContentRegionAvail().X) * ImGuiHelpers.GlobalScale;
        
        var pad = (ImGui.GetContentRegionAvail().X - SubWindowWidth * ImGuiHelpers.GlobalScale) / 2f;
        if (pad > 0) {
            ImGui.Dummy(new Vector2(pad, 1f));
            ImGui.SameLine();
        }

        using (ImRaii.Child("newFolder", new Vector2(w, ImGui.GetContentRegionAvail().Y))) 
        using (ImRaii.ItemWidth(ImGui.GetContentRegionAvail().X)) {
            if (focusName) {
                ImGui.SetKeyboardFocusHere();
                focusName = false;
            }
            dirty |= CustomInput.InputText("New Folder Name", ref folderName, 64, errorMessage: folderName.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.ReadOnlyInputText("Path", folderPath);
            var inputSize = ImGui.GetItemRectSize();
            if (ImGui.IsItemActive()) focusName = true;

            dirty |= ImGui.Checkbox("Hide Folder", ref hidden);
            
            
            var useCustomOutfitPolaroid = outfitStyle != null;
            var useCustomFolderPolaroid = folderStyle != null;
            var useCustomDefaultAppearanceToggles = defaultAppearanceToggles != null;
            var useCustomDefaultEquipmentToggles = defaultEquipmentToggles != null;
            var useCustomDefaultParameterToggles = defaultParameterToggles != null;
            var useCustomDefaultToggles = defaultToggleTypes != null;
            var useCustomDefaultLinks = defaultLinks != null;
            
            if (ImGui.Checkbox(useCustomOutfitPolaroid ? "##useCustomOutfitPolaroid" : "Use custom outfit style", ref useCustomOutfitPolaroid)) {
                dirty = true;
                outfitStyle = useCustomOutfitPolaroid ? (ActiveCharacter.OutfitPolaroidStyle ?? PluginConfig.CustomStyle?.OutfitList.Polaroid ?? Style.Default.OutfitList.Polaroid).Clone() : null;
            }

            if (outfitStyle != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Outfit Image Style")) {
                    using (ImRaii.PushIndent()) {
                        dirty |= PolaroidStyle.DrawEditor("Outfit", outfitStyle);
                    }
                }
            }

            if (ImGui.Checkbox(useCustomFolderPolaroid ? "##useCustomFolderPolaroid" : "Use custom folder style", ref useCustomFolderPolaroid)) {
                dirty = true;
                folderStyle = useCustomFolderPolaroid ? (ActiveCharacter.FolderPolaroidStyle ?? PluginConfig.CustomStyle?.FolderPolaroid ?? Style.Default.FolderPolaroid).Clone() : null;
            }

            if (folderStyle != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Folder Image Style")) {
                    using (ImRaii.PushIndent()) {
                        dirty |= PolaroidStyle.DrawEditor("Folder", folderStyle);
                    }
                }
            }
            
            if (ImGui.Checkbox(useCustomDefaultAppearanceToggles ? "##useCustomDefaultAppearanceToggles" : "Use Custom Default Appearance Toggles", ref useCustomDefaultAppearanceToggles)) {
                dirty = true;
                defaultAppearanceToggles = useCustomDefaultAppearanceToggles ? ActiveCharacter.DefaultEnabledCustomizeIndexes.Clone() : null;
            }
            
            if (useCustomDefaultAppearanceToggles && defaultAppearanceToggles != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Default Appearance Toggles")) {
                    ImGui.Columns(3, "defaultAppearanceToggles", false);
                    foreach (var ci in Enum.GetValues<CustomizeIndex>()) {
                        var v = defaultAppearanceToggles.Contains(ci);
                        if (ImGui.Checkbox($"{ci}##defaultEnabledCustomize", ref v)) {
                            dirty = true;
                            if (v) {
                                defaultAppearanceToggles.Add(ci);
                            } else {
                                defaultAppearanceToggles.Remove(ci);
                            }
                        }
                        ImGui.NextColumn();
                    }
                
                    ImGui.Columns(1);
                }
            }
            
            if (ImGui.Checkbox(useCustomDefaultParameterToggles ? "##useCustomDefaultParameterToggles" : "Use Custom Default Advanced Appearance Toggles", ref useCustomDefaultParameterToggles)) {
                dirty = true;
                defaultParameterToggles = useCustomDefaultParameterToggles ? ActiveCharacter.DefaultEnabledParameterKinds.Clone() : null;
            }
            
            if (useCustomDefaultParameterToggles && defaultParameterToggles != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Default Advanced Apperance Toggles")) {
                    ImGui.Columns(3, "defaultParameterToggles", false);
                    foreach (var ci in Enum.GetValues<AppearanceParameterKind>()) {
                        var v = defaultParameterToggles.Contains(ci);
                        if (ImGui.Checkbox($"{ci}##defaultEnabledParameter", ref v)) {
                            dirty = true;
                            if (v) {
                                defaultParameterToggles.Add(ci);
                            } else {
                                defaultParameterToggles.Remove(ci);
                            }
                        }
                        ImGui.NextColumn();
                    }
                
                    ImGui.Columns(1);
                }
            }
            
            if (ImGui.Checkbox(useCustomDefaultEquipmentToggles ? "##useCustomDefaultEquipToggles" : "Use Custom Default Equipment Toggles", ref useCustomDefaultEquipmentToggles)) {
                dirty = true;
                defaultEquipmentToggles = useCustomDefaultEquipmentToggles ? ActiveCharacter.DefaultDisabledEquipmentSlots.Clone() : null;
            }
            
            if (useCustomDefaultEquipmentToggles && defaultEquipmentToggles != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Default Equipment Toggles")) {
                    ImGui.Columns(2, "defaultEquipmentToggles", false);
                    foreach (var hs in Common.GetGearSlots()) {
                        var v = !defaultEquipmentToggles.Contains(hs);
                        if (ImGui.Checkbox($"{hs.PrettyName()}##defaultEnabledEquip", ref v)) {
                            dirty = true;
                            if (v) {
                                defaultEquipmentToggles.Remove(hs);
                            } else {
                                defaultEquipmentToggles.Add(hs);
                            }
                        }
                        ImGui.NextColumn();
                    }
                    
                    ImGui.Columns(1);
                }
            }
            
            if (ImGui.Checkbox(useCustomDefaultToggles ? "##useCustomDefaultToggles" : "Use Custom Defaults for Other Toggles", ref useCustomDefaultToggles)) {
                dirty = true;
                defaultToggleTypes = useCustomDefaultToggles ? ActiveCharacter.DefaultEnabledToggles.Clone() : null;
            }
            
            if (useCustomDefaultToggles && defaultToggleTypes != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Defaults for Other Toggles")) {
                    ImGui.Columns(3, "defaultParameterToggles", false);
                    foreach (var ci in Enum.GetValues<ToggleType>()) {
                        var v = defaultToggleTypes.Contains(ci);
                        if (ImGui.Checkbox($"{ci}##defaultEnabledToggle", ref v)) {
                            dirty = true;
                            if (v) {
                                defaultToggleTypes.Add(ci);
                            } else {
                                defaultToggleTypes.Remove(ci);
                            }
                            
                        }
                        ImGui.NextColumn();
                    }
                
                    ImGui.Columns(1);
                }
            }
            
            
            if (ImGui.Checkbox(useCustomDefaultLinks ? "##useCustomOutfitLinks" : "Use Custom Defaults for Outfit Links", ref useCustomDefaultLinks)) {
                dirty = true;
                outfitLinksEditor = null;
                defaultLinks = useCustomDefaultLinks ? new CharacterFolder.DefaultLinks() { Before = ActiveCharacter.DefaultLinkBefore.Clone(), After = ActiveCharacter.DefaultLinkAfter.Clone() } : null;
            }
            
            if (useCustomDefaultLinks && defaultLinks != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Defaults for Outfit Links")) {
                    outfitLinksEditor ??= new OutfitLinksEditor(ActiveCharacter, defaultLinks.Before, defaultLinks.After);
                    if (outfitLinksEditor.Draw($"New Outfit inside {folderName.OrDefault("This Folder")}")) {
                        dirty = true;
                    }
                }
            }
            
            if (PluginConfig.EnableOutfitCommands && ImGui.CollapsingHeader("Commands")) {
                ImGui.TextColoredWrapped(ImGui.GetColorU32(ImGuiCol.TextDisabled), "Execute commands automatically when changing outfits. Commands set here will be executed when any outfit from this folder is applied.");
                
                ImGui.Spacing();
                
                ImGui.TextDisabled("Before Outfit Commands:");
                using (ImRaii.PushIndent()) {
                    using (ImRaii.PushId("autoCommandBeforeOutfit")) {
                        dirty |= CommandEditor.Show(autoCommandBeforeOutfit, down: autoCommandAfterOutfit);
                    }
                }
                
                ImGui.TextDisabled("After Outfit Commands:");
                using (ImRaii.PushIndent()) {
                    using (ImRaii.PushId("autoCommandAfterOutfit")) {
                        dirty |= CommandEditor.Show(autoCommandAfterOutfit, up: autoCommandBeforeOutfit);
                    }
                }


            }
            
            if (ImGui.CollapsingHeader("Image")) {
                var f = folder ?? newFolder;
                if (f != null) {
                    var pFolder = ActiveCharacter.Folders.GetValueOrDefault(parentFolder);
                    var style = pFolder?.FolderPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).FolderPolaroid;
                    ImageEditor.Draw(f, style, folderName, ref controlFlags);
                }
            }
            
            if (ImGui.CollapsingHeader("Details")) {
                var guid = folder?.FolderGuid?.ToString() ?? string.Empty;
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                ImGui.InputText("GUID", ref guid, 128, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
            }
            
            if (folder == null) {
                if (ImGuiExt.ButtonWithIcon("Create Folder", FontAwesomeIcon.FolderPlus, inputSize)) {
                    SaveChanges();
                    ActiveCharacter.Dirty = true;
                    MainWindow.PopPage();

                    var guid = folder?.FolderGuid;
                    if (guid != null) {
                        MainWindow.OpenPage(new GlamourListPage(guid.Value));
                    }
                    
                }
            } else {
                using (ImRaii.Disabled(!(dirty || ImGui.GetIO().KeyShift))) {
                    if (ImGuiExt.ButtonWithIcon("Save Changes", FontAwesomeIcon.Save, inputSize)) {
                        SaveChanges();
                        MainWindow.PopPage();
                    }
                }
            }
        }
        
        
        base.DrawCenter(ref controlFlags);
    }
    
    private void SaveChanges() {
        if (folder == null) {
            var newFolderGuid = newFolder?.FolderGuid ?? Guid.NewGuid();
            folder = new CharacterFolder() {
                Parent = parentFolder,
                ConfigFile = ActiveCharacter,
                FolderGuid = newFolderGuid,
            };
            
            ActiveCharacter?.Folders.Add(newFolderGuid, folder);
        }
        
        folder.Name = folderName;
        folder.Hidden = hidden;
        folder.FolderPolaroidStyle = folderStyle;
        folder.OutfitPolaroidStyle = outfitStyle;
        folder.CustomDefaultDisabledEquipmentSlots = defaultEquipmentToggles;
        folder.CustomDefaultEnabledCustomizeIndexes = defaultAppearanceToggles;
        folder.CustomDefaultEnabledParameterKinds = defaultParameterToggles;
        folder.CustomDefaultEnabledToggles = defaultToggleTypes;
        folder.CustomDefaultLinks = defaultLinks;
        folder.AutoCommandBeforeOutfit = autoCommandBeforeOutfit;
        folder.AutoCommandAfterOutfit = autoCommandAfterOutfit;
        folder.AutoCommandsSkipCharacter = autoCommandsSkipCharacter;

        if (ActiveCharacter == null) return;
        ActiveCharacter.Dirty = true;
        ActiveCharacter.Save(true);
    }
    
    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        if (dirty) controlFlags |= WindowControlFlags.PreventClose;

        using (ImRaii.Disabled(dirty && !ImGui.GetIO().KeyShift)) {
            if (ImGuiExt.ButtonWithIcon(dirty && folder != null ? "Revert Changes" : "Cancel", FontAwesomeIcon.Ban, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                controlFlags |= WindowControlFlags.PreventClose;
                MainWindow.PopPage();
            }
        }

        if (!dirty || ImGui.GetIO().KeyShift) return;
        
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Hold SHIFT to confirm");
        }
    }
    
}
