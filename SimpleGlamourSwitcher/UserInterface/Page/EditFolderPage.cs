using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditFolderPage(Guid parentFolder, CharacterFolder? editFolder) : Page {



    private readonly CharacterFolder? newFolder = editFolder == null ? new CharacterFolder() { FolderGuid = Guid.NewGuid(), ConfigFile = ActiveCharacter } : null;
    
    
    private readonly string folderPath = ActiveCharacter?.ParseFolderPath(parentFolder) ?? throw new Exception("No Active Character");
    private string folderName = editFolder?.Name ?? string.Empty;
    private const float SubWindowWidth = 500f;
    private bool focusName = editFolder == null;

    private PolaroidStyle? outfitStyle = editFolder?.OutfitPolaroidStyle.Clone();
    private PolaroidStyle? folderStyle = editFolder?.FolderPolaroidStyle.Clone();

    private HashSet<CustomizeIndex>? defaultAppearanceToggles = editFolder?.CustomDefaultEnabledCustomizeIndexes.Clone();
    private HashSet<HumanSlot>? defaultEquipmentToggles = editFolder?.CustomDefaultDisabledEquipmentSlots.Clone();
    

    private bool hidden = editFolder?.Hidden ?? false;
    private CharacterFolder? folder = editFolder;

    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(folder == null ? "Creating" : "Editing Folder", shadowed: true);
        ImGuiExt.CenterText(folder == null ? $"New Folder in {folderPath}" : $"{folderPath} / {folder.Name}", shadowed: true);
    }
    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        if (ActiveCharacter == null) return;

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
            var returnPressed = CustomInput.InputText("New Folder Name", ref folderName, 64, errorMessage: folderName.Length == 0 ? "Please enter a name" : string.Empty, flags: ImGuiInputTextFlags.EnterReturnsTrue);
            CustomInput.ReadOnlyInputText("Path", folderPath);
            var inputSize = ImGui.GetItemRectSize();
            if (ImGui.IsItemActive()) focusName = true;

            ImGui.Checkbox("Hide Folder", ref hidden);
            
            
            var useCustomOutfitPolaroid = outfitStyle != null;
            var useCustomFolderPolaroid = folderStyle != null;
            var useCustomDefaultAppearanceToggles = defaultAppearanceToggles != null;
            var useCustomDefaultEquipmentToggles = defaultEquipmentToggles != null;

            if (ImGui.Checkbox(useCustomOutfitPolaroid ? "##useCustomOutfitPolaroid" : "Use custom outfit style", ref useCustomOutfitPolaroid)) {
                if (useCustomOutfitPolaroid) {
                    outfitStyle = (ActiveCharacter.CustomStyle?.OutfitList.Polaroid ?? PluginConfig.CustomStyle?.OutfitList.Polaroid ?? Style.Default.OutfitList.Polaroid).Clone();
                } else {
                    outfitStyle = null;
                }
            }

            if (outfitStyle != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Outfit Style")) {
                    using (ImRaii.PushIndent()) {
                        PolaroidStyle.DrawEditor("Outfit", outfitStyle, PolaroidStyle.PolaroidStyleEditorFlags.ImageSize | PolaroidStyle.PolaroidStyleEditorFlags.ShowPreview);
                    }
                }
            }

            if (ImGui.Checkbox(useCustomFolderPolaroid ? "##useCustomFolderPolaroid" : "Use custom folder style", ref useCustomFolderPolaroid)) {
                if (useCustomFolderPolaroid) {
                    folderStyle = (ActiveCharacter.CustomStyle?.FolderPolaroid ?? PluginConfig.CustomStyle?.FolderPolaroid ?? Style.Default.FolderPolaroid).Clone();
                } else {
                    folderStyle = null;
                }
            }

            if (folderStyle != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Folder Style")) {
                    using (ImRaii.PushIndent()) {
                        PolaroidStyle.DrawEditor("Folder", folderStyle, PolaroidStyle.PolaroidStyleEditorFlags.ImageSize | PolaroidStyle.PolaroidStyleEditorFlags.ShowPreview);
                    }
                }
            }
            
            if (ImGui.Checkbox(useCustomDefaultAppearanceToggles ? "##useCustomDefaultAppearanceToggles" : "Use Custom Default Appearance Toggles", ref useCustomDefaultAppearanceToggles)) {
                if (useCustomDefaultAppearanceToggles) {
                    defaultAppearanceToggles = ActiveCharacter.DefaultEnabledCustomizeIndexes.Clone();
                } else {
                    defaultAppearanceToggles = null;
                }
            }
            
            if (useCustomDefaultAppearanceToggles && defaultAppearanceToggles != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Default Appearance Toggles")) {
                    ImGui.Columns(3, "defaultAppearanceToggles", false);
                    foreach (var ci in Enum.GetValues<CustomizeIndex>()) {
                        var v = defaultAppearanceToggles.Contains(ci);
                        if (ImGui.Checkbox($"{ci}##defaultEnabledCustomize", ref v)) {
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
            
            if (ImGui.Checkbox(useCustomDefaultEquipmentToggles ? "##useCustomDefaultEquipToggles" : "Use Custom Default Equipment Toggles", ref useCustomDefaultEquipmentToggles)) {
                if (useCustomDefaultEquipmentToggles) {
                    defaultEquipmentToggles = ActiveCharacter.DefaultDisabledEquipmentSlots.Clone();
                } else {
                    defaultEquipmentToggles = null;
                }
            }
            
            if (useCustomDefaultEquipmentToggles && defaultEquipmentToggles != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Default Equipment Toggles")) {
                    ImGui.Columns(2, "defaultEquipmentToggles", false);
                    foreach (var hs in Common.GetGearSlots()) {
                        var v = !defaultEquipmentToggles.Contains(hs);
                        if (ImGui.Checkbox($"{hs.PrettyName()}##defaultEnabledEquip", ref v)) {
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
            
            if (ImGui.CollapsingHeader("Image")) {
                var f = folder ?? newFolder;
                if (f != null) {
                    var pFolder = ActiveCharacter.Folders.GetValueOrDefault(parentFolder);
                    var style = pFolder?.FolderPolaroidStyle ?? (ActiveCharacter.CustomStyle ?? PluginConfig.CustomStyle ?? Style.Default).FolderPolaroid;
                    ImageEditor.Draw(f, style, folderName, ref controlFlags);
                }
            }
            
            
            
            
            if (folder == null) {
                if (ImGui.Button("Create Folder", inputSize) || returnPressed) {
                    SaveChanges();
                    ActiveCharacter.Dirty = true;
                    MainWindow.PopPage();

                    var guid = folder?.FolderGuid;
                    if (guid != null) {
                        MainWindow?.OpenPage(new GlamourListPage(guid.Value));
                    }
                    
                }
            } else {
                if (ImGui.Button("Save Changes", inputSize)) {
                    SaveChanges();
                    MainWindow.PopPage();
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

        if (ActiveCharacter == null) return;
        ActiveCharacter.Dirty = true;
        ActiveCharacter.Save(true);
    }
    
}
