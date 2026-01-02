using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Data;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;
using ItemManager = SimpleGlamourSwitcher.Service.ItemManager;
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
    private OutfitWeapons? weapons;
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
        weapons ??= Outfit.Weapons.Clone();

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
                        ImGui.Checkbox("Revert to Game State##appearance", ref appearance.RevertToGame);
                        DrawAppearance();
                    }
                }
            
                if (ImGui.CollapsingHeader("Advanced Appearance")) {
                    using (ImRaii.PushIndent()) {
                        DrawParameters();
                    }
                }
            }
            
            dirty |= ImGui.Checkbox("##applyEquipment", ref equipment.Apply);
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Equipment")) {
                using (ImRaii.PushIndent()) {
                    ImGui.Checkbox("Revert to Game State##equipment", ref equipment.RevertToGame);
                    DrawEquipment();
                }
            }
            
            dirty |= ImGui.Checkbox("##applyWeapons", ref weapons.Apply);
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Weapons / Tools")) {
                using (ImRaii.PushIndent()) {
                    DrawWeapons();
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
                var guid = Outfit.Guid.ToString();
                ImGui.InputText("GUID", ref guid, 128, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
                sortName ??= string.Empty;
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
                Outfit.Weapons =  weapons ?? Outfit.Weapons;
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


    
    private void DrawWeapons() {
        weapons ??= Outfit.Weapons.Clone();
        
        foreach (var (classJobId, classWeapons) in weapons.ClassWeapons) {
            var classJob = DataManager.GetExcelSheet<ClassJob>().GetRow(classJobId);
            using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f), !weapons.Apply)) {
                dirty |= ImGui.Checkbox($"##applyWeapons_ClassJob#{classJobId}", ref classWeapons.Apply);
            }
            
            if (ImGui.IsItemHovered()) {
                using (ImRaii.Tooltip()) {
                    ImGui.Text($"Enable {classJob.Name.ExtractText()}");
                    if (!weapons.Apply) {
                        ImGui.TextDisabled("This option is will not be applied because the Weapons option is not enabled for this outfit.");
                    }
                }
            }
            
            ImGui.SameLine();

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.X * 2));
            var headerOpen = ImGui.CollapsingHeader($"##classJob#{classJobId}", ImGuiTreeNodeFlags.SpanAvailWidth);

            var icon = TextureProvider.GetFromGameIcon(new GameIconLookup(62100 + classJobId)).GetWrapOrDefault();

            if (icon != null) {
                ImGui.GetWindowDrawList().AddImage(icon.Handle, ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().Y, 0), ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().Y * 2, ImGui.GetItemRectSize().Y));
            }
            
            ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().Y * 2 + ImGui.GetStyle().FramePadding.X, ImGui.GetStyle().FramePadding.Y), ImGui.GetColorU32(ImGuiCol.Text), classJob.NameEnglish.ExtractText());
            
            if (headerOpen) {
                using (ImRaii.PushIndent()) {
                    ShowSlot(EquipSlot.MainHand, classJobId, classWeapons);

                    var mhEquipItem = classWeapons.MainHand.GetEquipItem(EquipSlot.MainHand);
                    var mhItem = DataManager.GetExcelSheet<Item>().GetRowOrDefault(mhEquipItem.ItemId.Id);
                    if (mhItem != null && mhItem.Value.ToEquipType().Offhand() is not (FullEquipType.Unknown or FullEquipType.Gig)) {
                        ShowSlot(EquipSlot.OffHand, classJobId, classWeapons);
                    }
                }
            }
        }
        
        var unAddedRoles = DataManager.GetExcelSheet<ClassJob>().OrderBy(c => c.Role)
            .Where(c => c.RowId != 0 && c.ClassJobParent.RowId == c.RowId && !weapons.ClassWeapons.ContainsKey(c.RowId)).ToArray();
        if (unAddedRoles.Length > 0) {
            ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2));
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Add Class")) {
                using (ImRaii.PushIndent()) 
                using (ImRaii.PushIndent()) {
                    byte? pRole = null;
                
                    foreach (var c in unAddedRoles) {
                        var icon = TextureProvider.GetFromGameIcon(new GameIconLookup(62100 + c.RowId)).GetWrapOrDefault();
                        if (icon == null) continue;
                        if (pRole == c.Role) ImGui.SameLine();
                        pRole = c.Role;
                    
                        ImGui.Image(icon.Handle, new Vector2(ImGui.GetTextLineHeightWithSpacing() * 1.5f + ImGui.GetStyle().FramePadding.Y * 2));
                        if (ImGui.IsItemClicked()) {
                            var defaults = ItemPicker.GetDefaultWeapon(c);
                            var classWeapons = new OutfitClassWeapons {
                                Apply = true,
                                MainHand = new ApplicableWeapon() {
                                    Apply = true,
                                    ItemId = defaults.MainHand.ItemId,
                                },
                                OffHand = new ApplicableWeapon() {
                                    Apply = true,
                                    ItemId = defaults.OffHand.ItemId,
                                }
                            };

                            weapons.ClassWeapons.Add(c.RowId, classWeapons);
                        }
                        if (ImGui.IsItemHovered()) {
                            ImGui.SetTooltip($"Add {c.NameEnglish.ExtractText()}");
                        }
                    }
                }
            }
        }
    }

    private void ShowSlot(EquipSlot slot, uint classJobId, OutfitClassWeapons classWeapons) {
        weapons ??= Outfit.Weapons.Clone();
        var equip = classWeapons[slot];
        var classJob = DataManager.GetExcelSheet<ClassJob>().GetRow(classJobId);
        using (ImRaii.Group()) {
            using (ImRaii.Group()) {
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));
                using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f), !weapons.Apply || !classWeapons.Apply)) {
                    dirty |= ImGui.Checkbox($"##enable_ClassJob#{classJobId}_{slot}", ref equip.Apply);
                }
        
                if (ImGui.IsItemHovered()) {
                    using (ImRaii.Tooltip()) {
                        ImGui.Text($"Enable {classJob.NameEnglish} {slot.PrettyName()}");
                        if (!weapons.Apply) {
                            ImGui.TextDisabled("This option is will not be applied because the Weapons option is not enabled for this outfit.");
                        } else if (!classWeapons.Apply) {
                            ImGui.TextDisabled($"This option is will not be applied because the {classJob.NameEnglish.ExtractText()} option is not enabled for this outfit.");
                        }
                    }
                }
            }

            ImGui.SameLine();
            using (ImRaii.Group()) {
                ShowSlot(slot, equip, classJob);
            }
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
    
    private void ShowSlot(HumanSlot slot, ApplicableItem<HumanSlot> equipment) {
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


    
    private void ShowSlot(EquipSlot slot, ApplicableWeapon weapon, ClassJob classJob) {
        
        // var equipItem = weapon.GetEquipItem(slot);

        var itemData = DataManager.GetExcelSheet<Item>().GetRowOrDefault(weapon.ItemId.Id);
        
        var equipItem = itemData == null ? ItemManager.NothingItem(slot) : slot switch {
            EquipSlot.MainHand => EquipItem.FromMainhand(itemData.Value),
            EquipSlot.OffHand => EquipItem.FromOffhand(itemData.Value),
            _ => throw new Exception("Only weapons/tools supported")
        };
        
        
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One))
        using (ImRaii.PushId($"State_{slot}")) {
            ItemIcon.Draw(slot, equipItem);
            ImGui.SameLine();

            var s = new Vector2(300 * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);

            using (ImRaii.Group()) {
                ImGui.SetNextItemWidth(s.X - (weapon.Materials is { Count: > 0 } ? s.Y + ImGui.GetStyle().ItemSpacing.X : 0) - (s.Y * 2 + ImGui.GetStyle().ItemSpacing.X * 2));

                ImGui.BeginGroup();

                
                
                if (ItemPicker.ShowWeaponPicker($"##{slot}##ClassJob{classJob.RowId}", slot, classJob, ref equipItem)) {
                    PluginLog.Warning($"Changed Item: {equipItem.Name}");
                    weapon.ItemId = PluginService.ItemManager.Resolve(slot, equipItem.ItemId.Id).ItemId;
                    
                    dirty = true;
                }
   
                if (weapon.Materials is { Count: > 0 }) {
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
                                foreach (var material in weapon.Materials) {
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

                if (weapon is { } ae) {
                    ImGui.SameLine();
                    dirty |= StainPicker.Show($"{slot}, Stain 1##{slot}_stain1", ref ae.Stain.Stain, new Vector2(s.Y));
                    ImGui.SameLine();
                    dirty |= StainPicker.Show($"{slot}, Stain 2##{slot}_stain2", ref ae.Stain.Stain2, new Vector2(s.Y));
                }

                ImGui.EndGroup();
                
                dirty |= ModListDisplay.Show(weapon, $"{slot.PrettyName()}");
            }
        }
    }
}
