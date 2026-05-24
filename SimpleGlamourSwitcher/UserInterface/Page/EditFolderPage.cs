using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditFolderPage(CharacterConfigFile character, Guid parentFolderGuid, CharacterFolder? folder) : Page {
    private const float SubWindowWidth = 600;
    private CharacterConfigFile Character => character;
    protected Guid FolderGuid => CommonDetailsEditor.FolderGuid;
    private readonly FileDialogManager fileDialogManager = new();

    private bool dirty;
    private PolaroidStyle? outfitStyle = folder?.OutfitPolaroidStyle.Clone();
    private PolaroidStyle? folderStyle = folder?.FolderPolaroidStyle.Clone();
    private HashSet<CustomizeIndex>? defaultAppearanceToggles = folder?.CustomDefaultEnabledCustomizeIndexes.Clone();
    private HashSet<HumanSlot>? defaultEquipmentToggles = folder?.CustomDefaultDisabledEquipmentSlots.Clone();
    private HashSet<EquipSlot>? defaultWeaponToggles = folder?.CustomDefaultDisabledWeaponSlots.Clone();
    private HashSet<AppearanceParameterKind>? defaultParameterToggles = folder?.CustomDefaultEnabledParameterKinds.Clone();
    private HashSet<ToggleType>? defaultToggleTypes = folder?.CustomDefaultEnabledToggles.Clone();
    private CharacterFolder.DefaultLinks? defaultLinks = folder?.CustomDefaultLinks.Clone();
    private List<AutoCommandEntry> autoCommandBeforeOutfit = folder?.AutoCommandBeforeOutfit.Clone() ?? [];
    private List<AutoCommandEntry> autoCommandAfterOutfit = folder?.AutoCommandAfterOutfit.Clone() ?? [];
    private bool autoCommandsSkipCharacter = folder?.AutoCommandsSkipCharacter ?? false;
    private bool? defaultRevertEquip = folder?.CustomDefaultRevertEquip;
    private bool? defaultRevertCustomize = folder?.CustomDefaultRevertCustomize;
    private bool hidden = folder?.Hidden ?? false;
    private FolderSortStrategy folderSortStrategy = folder?.FolderSortStrategy ?? FolderSortStrategy.Inherit;

    private OutfitLinksEditor? outfitLinksEditor;

    private CharacterFolder Folder {
        get {
            if (folder != null) return folder;
            return field ??= new CharacterFolder {
                Guid = Guid.NewGuid(),
                Parent = parentFolderGuid,
                ConfigFile = Character,
            };
        }
    }

    private CommonDetailsEditor CommonDetailsEditor {
        get {
            return field ??= new CommonDetailsEditor(Character, Folder);
        }
    }

    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(folder == null ? "Creating" : "Editing Folder", shadowed: true);
        ImGuiExt.CenterText(folder == null ? $"New Folder in {CommonDetailsEditor.FolderPath}" : $"{CommonDetailsEditor.FolderPath} / {CommonDetailsEditor.Name}", shadowed: true);
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

    private void DrawEditor() {
        dirty |= ImGui.Checkbox("Hide Folder", ref hidden);


        var useCustomOutfitPolaroid = outfitStyle != null;
        var useCustomFolderPolaroid = folderStyle != null;
        var useCustomDefaultAppearanceToggles = defaultAppearanceToggles != null;
        var useCustomDefaultEquipmentToggles = defaultEquipmentToggles != null;
        var useCustomDefaultWeaponToggles = defaultWeaponToggles != null;
        var useCustomDefaultParameterToggles = defaultParameterToggles != null;
        var useCustomDefaultToggles = defaultToggleTypes != null;
        var useCustomDefaultLinks = defaultLinks != null;

        if (ImGui.Checkbox(useCustomOutfitPolaroid ? "##useCustomOutfitPolaroid" : "Use custom outfit style", ref useCustomOutfitPolaroid)) {
            dirty = true;
            outfitStyle = useCustomOutfitPolaroid ? (Character.OutfitPolaroidStyle ?? PluginConfig.CustomStyle?.OutfitList.Polaroid ?? Style.Default.OutfitList.Polaroid).Clone() : null;
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
            folderStyle = useCustomFolderPolaroid ? (Character.FolderPolaroidStyle ?? PluginConfig.CustomStyle?.FolderPolaroid ?? Style.Default.FolderPolaroid).Clone() : null;
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
            defaultAppearanceToggles = useCustomDefaultAppearanceToggles ? Character.DefaultEnabledCustomizeIndexes.Clone() : null;
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

                ImGui.Columns();
            }
        }

        if (ImGui.Checkbox(useCustomDefaultParameterToggles ? "##useCustomDefaultParameterToggles" : "Use Custom Default Advanced Appearance Toggles", ref useCustomDefaultParameterToggles)) {
            dirty = true;
            defaultParameterToggles = useCustomDefaultParameterToggles ? Character.DefaultEnabledParameterKinds.Clone() : null;
        }

        if (useCustomDefaultParameterToggles && defaultParameterToggles != null) {
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Custom Default Advanced Appearance Toggles")) {
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

                ImGui.Columns();
            }
        }

        dirty |= ImGuiExt.CheckboxTriState("Revert Appearance State", ref defaultRevertCustomize, true);

        if (ImGui.Checkbox(useCustomDefaultEquipmentToggles ? "##useCustomDefaultEquipToggles" : "Use Custom Default Equipment Toggles", ref useCustomDefaultEquipmentToggles)) {
            dirty = true;
            defaultEquipmentToggles = useCustomDefaultEquipmentToggles ? Character.DefaultDisabledEquipmentSlots.Clone() : null;
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

                ImGui.Columns();
            }
        }

        dirty |= ImGuiExt.CheckboxTriState("Revert Equipment State", ref defaultRevertEquip, true);


        if (ImGui.Checkbox(useCustomDefaultWeaponToggles ? "##useCustomDefaultEquipToggles" : "Use Custom Default Weapon Toggles", ref useCustomDefaultWeaponToggles)) {
            dirty = true;
            defaultWeaponToggles = useCustomDefaultWeaponToggles ? Character.DefaultDisabledWeaponSlots.Clone() : null;
        }

        if (useCustomDefaultWeaponToggles && defaultWeaponToggles != null) {
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Custom Default Weapon Toggles")) {
                ImGui.Columns(2, "defaultWeaponToggles", false);
                foreach (var es in Common.Set(EquipSlot.MainHand, EquipSlot.OffHand)) {
                    var v = !defaultWeaponToggles.Contains(es);
                    if (ImGui.Checkbox($"{es.PrettyName()}##defaultEnabledEquip", ref v)) {
                        dirty = true;
                        if (v) {
                            defaultWeaponToggles.Remove(es);
                        } else {
                            defaultWeaponToggles.Add(es);
                        }
                    }
                    ImGui.NextColumn();
                }

                ImGui.Columns();
            }
        }


        if (ImGui.Checkbox(useCustomDefaultToggles ? "##useCustomDefaultToggles" : "Use Custom Defaults for Other Toggles", ref useCustomDefaultToggles)) {
            dirty = true;
            defaultToggleTypes = useCustomDefaultToggles ? Character.DefaultEnabledToggles.Clone() : null;
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

                ImGui.Columns();
            }
        }


        if (ImGui.Checkbox(useCustomDefaultLinks ? "##useCustomOutfitLinks" : "Use Custom Defaults for Outfit Links", ref useCustomDefaultLinks)) {
            dirty = true;
            outfitLinksEditor = null;
            defaultLinks = useCustomDefaultLinks ? new CharacterFolder.DefaultLinks {
                Before = Character.DefaultLinkBefore.Clone(),
                After = Character.DefaultLinkAfter.Clone(),
            } : null;
        }

        if (useCustomDefaultLinks && defaultLinks != null) {
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Custom Defaults for Outfit Links")) {
                outfitLinksEditor ??= new OutfitLinksEditor(Character, defaultLinks.Before, defaultLinks.After);
                if (outfitLinksEditor.Draw($"New Outfit inside {CommonDetailsEditor.Name.OrDefault("This Folder")}")) {
                    dirty = true;
                }
            }
        }

        ImGuiEx.EnumCombo("Folder Display Order", ref folderSortStrategy, new Dictionary<FolderSortStrategy, string> {
            {
                FolderSortStrategy.Inherit, $"Inherit ({folder?.GetFolderSortStrategy()})"
            },
        });

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
                    dirty |= CommandEditor.Show(autoCommandAfterOutfit, autoCommandBeforeOutfit);
                }
            }


        }
    }

    private void SaveFolder() {
        if (folder == null) Character.Folders.TryAdd(Folder.Guid, Folder);

        CommonDetailsEditor.ApplyTo(Folder);
        Folder.Hidden = hidden;
        Folder.FolderPolaroidStyle = folderStyle;
        Folder.OutfitPolaroidStyle = outfitStyle;
        Folder.CustomDefaultDisabledEquipmentSlots = defaultEquipmentToggles;
        Folder.CustomDefaultDisabledWeaponSlots = defaultWeaponToggles;
        Folder.CustomDefaultEnabledCustomizeIndexes = defaultAppearanceToggles;
        Folder.CustomDefaultEnabledParameterKinds = defaultParameterToggles;
        Folder.CustomDefaultEnabledToggles = defaultToggleTypes;
        Folder.CustomDefaultLinks = defaultLinks;
        Folder.AutoCommandBeforeOutfit = autoCommandBeforeOutfit;
        Folder.AutoCommandAfterOutfit = autoCommandAfterOutfit;
        Folder.AutoCommandsSkipCharacter = autoCommandsSkipCharacter;
        Folder.CustomDefaultRevertEquip = defaultRevertEquip;
        Folder.CustomDefaultRevertCustomize = defaultRevertCustomize;
        Folder.FolderSortStrategy = folderSortStrategy;

        Character.Dirty = true;
        Character.Save(true);
    }

    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        fileDialogManager.Draw();
        controlFlags |= WindowControlFlags.PreventClose;

        var pad = (ImGui.GetContentRegionAvail().X - SubWindowWidth * ImGuiHelpers.GlobalScale) / 2f;

        dirty |= CommonDetailsEditor.ShowNameAndFolderEditors(SubWindowWidth);

        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();

        using (ImRaii.Child("entryEditor", new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {
            DrawEditor();
            CommonDetailsEditor.ShowCommonDetails(ref controlFlags);
        }

        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(SubWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon($"Save Folder", FontAwesomeIcon.Save, new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                SaveFolder();
                MainWindow.PopPage();
            }
        }
    }
}
