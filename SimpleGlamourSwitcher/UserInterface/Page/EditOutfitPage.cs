using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditOutfitPage(CharacterConfigFile character, Guid folderGuid, OutfitConfigFile? outfit) : Page {
    
    public bool IsNewOutfit { get; } = outfit == null;
    public OutfitConfigFile Outfit { get; } = outfit ?? OutfitConfigFile.CreateFromLocalPlayer(character, folderGuid, character.GetOptionsProvider(folderGuid));

    private readonly string folderPath = character.ParseFolderPath(folderGuid);
    private const float SubWindowWidth = 600f;

    private readonly FileDialogManager fileDialogManager = new();

    private OutfitEquipment? equipment;
    private OutfitAppearance? appearance;
    private string outfitName = outfit?.Name ?? string.Empty;
    private string? sortName = outfit?.SortName ?? string.Empty;

    private List<Guid>? linkBefore;
    private List<Guid>? linkAfter;
    private List<AutoCommandEntry> autoCommands = outfit?.AutoCommands ?? [];
    
    private OutfitLinksEditor? linksEditor;
    
    private bool dirty;
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNewOutfit ? "Creating" : "Editing Outfit", shadowed: true);
        ImGuiExt.CenterText(IsNewOutfit ? $"New Outfit in {folderPath}" : $"{folderPath} / {Outfit.Name}", shadowed: true);
    }
    
    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        using (ImRaii.Disabled(dirty && !ImGui.GetIO().KeyShift)) {
            if (ImGuiExt.ButtonWithIcon(dirty ? "Discard Changes": "Back", FontAwesomeIcon.CaretLeft, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                MainWindow.PopPage();
            }
        }
        
        #if DEBUG
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
            dirty = false;
        }
        #endif

        if (dirty && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Hold SHIFT to confirm.");
        }
    }


    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        equipment ??= Outfit.Equipment.Clone();
        appearance ??= Outfit.Appearance.Clone();

        linkBefore = Outfit.ApplyBefore;
        linkAfter = Outfit.ApplyAfter;
        
        
        fileDialogManager.Draw();
        controlFlags |= WindowControlFlags.PreventClose;
        ImGui.Spacing();
        
        var pad = (ImGui.GetContentRegionAvail().X - SubWindowWidth * ImGuiHelpers.GlobalScale) / 2f;
        using (ImRaii.Group()) {
            ImGui.Dummy(new Vector2(pad, 1f));
        }
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(SubWindowWidth * ImGuiHelpers.GlobalScale)) {
            dirty |= CustomInput.InputText("Outfit Name", ref outfitName, 100, errorMessage: outfitName.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.ReadOnlyInputText("Path", folderPath);
        }
        
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        if (ImGui.BeginChild("equipment", new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {

            dirty |= ImGui.Checkbox("##applyAppearance", ref appearance.Apply);
            ImGui.SameLine();
            using (ImRaii.Group()) {
                if (ImGui.CollapsingHeader("Appearance")) {
                    using (ImRaii.PushIndent()) {
                        DrawAppearance();
                    }
                }
            
                if (ImGui.CollapsingHeader("Advanced Appearance")) {
                    using (ImRaii.PushIndent()) {
                        DrawParameters();
                    }
                }
            }
            
            ImGui.Checkbox("##applyEquipment", ref equipment.Apply);
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Equipment")) {
                using (ImRaii.PushIndent()) {
                    DrawEquipment();
                }
            }
            
            if (ImGui.CollapsingHeader("Image")) {
                var folder = character.Folders.GetValueOrDefault(folderGuid);
                var outfitStyle = folder?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
                ImageEditor.Draw(Outfit, outfitStyle, Outfit.Name, ref controlFlags);
            }

            if (ImGui.CollapsingHeader("Outfit Links")) {
                linksEditor ??= new OutfitLinksEditor(character, Outfit, linkBefore, linkAfter);
                dirty |= linksEditor.Draw(outfitName.OrDefault("This Outfit"));
            }
            
            if (PluginConfig.EnableOutfitCommands && ImGui.CollapsingHeader("Commands")) {
                ImGui.TextColoredWrapped(ImGui.GetColorU32(ImGuiCol.TextDisabled), "Execute commands automatically when changing into this outfit.");
                
                ImGui.Spacing();
                
                using (ImRaii.PushId("autoCommands")) {
                    dirty |= CommandEditor.Show(autoCommands);
                }
            }

            
            if (ImGui.CollapsingHeader("Details")) {
                ImGui.Text($"GUID: {Outfit.Guid}");
                dirty |= ImGui.InputTextWithHint("Custom Sort Name", outfitName, ref sortName, 128);
            }
            
        }
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));
        
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(SubWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon("Save Outfit", FontAwesomeIcon.Save, new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                Outfit.Name = outfitName;
                Outfit.SortName = string.IsNullOrWhiteSpace(sortName) ? null : sortName.Trim();
                Outfit.Equipment = equipment ?? Outfit.Equipment;
                Outfit.Appearance = appearance ?? Outfit.Appearance;
                Outfit.ApplyBefore = linkBefore;
                Outfit.ApplyAfter = linkAfter;
                Outfit.AutoCommands = autoCommands;
                Outfit.Save(true);
                MainWindow.PopPage();
            }
        }
    }

    private void DrawAppearance() {
        appearance ??= Outfit.Appearance.Clone();
        dirty |= CustomizeEditor.Show(appearance);
    }

    private void DrawParameters() {
        foreach (var v in System.Enum.GetValues<AppearanceParameterKind>()) {
            DrawParameter(v);
        }
    }

    private void DrawParameter(AppearanceParameterKind kind) {
        appearance ??= Outfit.Appearance.Clone();
        var param = appearance[kind];
        CustomizeEditor.ShowApplyEnableCheckbox(kind.PrettyName(), ref param.Apply, ref appearance.Apply);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
        dirty |= param.ShowEditor($"{kind}##paramEditor_{kind}", kind);
    }

    private void DrawEquipment() {
        foreach (var s in Common.GetGearSlots()) {
            ShowSlot(s);
        }
    }
    
    public void ShowSlot(HumanSlot slot) {
        equipment ??= Outfit.Equipment.Clone();
        
        var equip = equipment[slot];

        using (ImRaii.Group()) {
            using (ImRaii.Group()) {
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));
                using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f), !equipment.Apply)) {
                    dirty |= ImGui.Checkbox($"##enable_{slot}", ref equip.Apply);
                }
        
                if (ImGui.IsItemHovered()) {
                    using (ImRaii.Tooltip()) {
                        ImGui.Text($"Enable {slot.PrettyName()}");
                        if (!equipment.Apply) {
                            ImGui.TextDisabled("This option is will not be applied because the Equipment option is not enabled for this outfit.");
                        }
                    }
                }
            }

            ImGui.SameLine();
            using (ImRaii.Group()) {
                ShowSlot(slot, equip);
            }
        }

        if (slot == HumanSlot.Head) {
            ImGui.SameLine();
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One))
            using (ImRaii.Group()) {
                dirty |= equipment.HatVisible.ShowToggleEditor("Headwear Visible");
                dirty |= equipment.VisorToggle.ShowToggleEditor("Visor Toggle");
            }
        }

        if (slot == HumanSlot.Body) {
            ImGui.SameLine();
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One))
            using (ImRaii.Group()) {
                dirty |= equipment.VieraEarsVisible.ShowToggleEditor("Ears Visible");
                dirty |= equipment.WeaponVisible.ShowToggleEditor("Weapon Visible");
            }
        }
    }
    
    private void ShowSlot(HumanSlot slot, ApplicableItem equipment) {
        var equipItem = equipment.GetEquipItem(slot);
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One))
        using (ImRaii.PushId($"State_{slot}")) {
            ItemIcon.Draw(slot, equipItem);
            ImGui.SameLine();

            var s = new Vector2(300 * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);

            using (ImRaii.Group()) {
                ImGui.SetNextItemWidth(s.X - (equipment.Materials is { Count: > 0 } ? s.Y + ImGui.GetStyle().ItemSpacing.X : 0) - (equipment is ApplicableEquipment ? s.Y * 2 + ImGui.GetStyle().ItemSpacing.X * 2 : 0));

                ImGui.BeginGroup();

                if (equipment is ApplicableEquipment applicableEquipment) {
                    if (ItemPicker.Show($"##{slot}", slot, ref equipItem)) {
                        applicableEquipment.ItemId = equipItem.ItemId;
                        dirty = true;
                    }
                } else if (equipment is ApplicableBonus applicableBonus) {
                    if (ItemPicker.Show($"##{slot}", slot, ref equipItem)) {
                        applicableBonus.BonusItemId = equipItem.Id.Id;
                        dirty = true;
                    }
                } else {
                    var name = equipItem.Name;
                    ImGui.InputText("##itemName", ref name, 64, ImGuiInputTextFlags.ReadOnly);
                }
                
                if (equipment.Materials is { Count: > 0 }) {
                    ImGui.SameLine();
                    using (ImRaii.PushFont(UiBuilder.IconFont)) {
                        if (ImGui.Button(FontAwesomeIcon.Palette.ToIconString(), new Vector2(s.Y))) { }
                    }

                    if (ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();

                        ImGui.Text($"{slot.PrettyName()} Advanced Dyes");
                        ImGui.Separator();

                        using (ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero))
                        using (ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(3, ImGui.GetStyle().CellPadding.Y))) {
                            if (ImGui.BeginTable("materialsTable", 4)) {
                                foreach (var material in equipment.Materials) {
                                    ImGui.TableNextColumn();
                                    var t = $"{material.MaterialValueIndex.MaterialString()} {material.MaterialValueIndex.RowString()}";
                                    ImGui.SetNextItemWidth(ImGui.CalcTextSize(t).X + ImGui.GetStyle().FramePadding.X * 2);
                                    ImGui.InputText("##material", ref t, 128, ImGuiInputTextFlags.ReadOnly);
                                    ImGui.TableNextColumn();
                                    ImGui.ColorButton("Diffuse", new Vector4(material.DiffuseR, material.DiffuseG, material.DiffuseB, 1));
                                    ImGui.SameLine();
                                    ImGui.ColorButton("Specular", new Vector4(material.SpecularR, material.SpecularG, material.SpecularB, 1));
                                    ImGui.SameLine();
                                    ImGui.ColorButton("Emissive", new Vector4(material.EmissiveR, material.EmissiveG, material.EmissiveB, 1));
                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{material.Gloss}");
                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted($"{material.SpecularA * 100}%");
                                }

                                ImGui.EndTable();
                            }
                        }

                        ImGui.EndTooltip();
                    }
                }

                if (equipment is ApplicableEquipment ae) {
                    ImGui.SameLine();
                    dirty |= StainPicker.Show($"{slot}, Stain 1##{slot}_stain1", ref ae.Stain.Stain, new Vector2(s.Y));
                    ImGui.SameLine();
                    dirty |= StainPicker.Show($"{slot}, Stain 2##{slot}_stain2", ref ae.Stain.Stain2, new Vector2(s.Y));
                }

                ImGui.EndGroup();
                
                dirty |= ModListDisplay.Show(equipment, $"{slot.PrettyName()}");
            }
        }
    }
}
