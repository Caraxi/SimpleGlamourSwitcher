using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Lumina.Excel.Sheets;
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

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditOutfitPage(CharacterConfigFile character, Guid folderGuid, OutfitConfigFile? outfit) : EntryEditorPage<OutfitConfigFile>(character, folderGuid, outfit) {
    public override string TypeName => "Outfit";
    
    private OutfitEquipment? equipment;
    private OutfitAppearance? appearance;
    private OutfitWeapons? weapons;

    private List<Guid>? linkBefore;
    private List<Guid>? linkAfter;
    private OutfitLinksEditor? linksEditor;

    protected override void DrawEditor(ref WindowControlFlags controlFlags) {
        equipment ??= Entry.Equipment.Clone();
        appearance ??= Entry.Appearance.Clone();
        weapons ??= Entry.Weapons.Clone();
        linkBefore ??= Entry.ApplyBefore.Clone();
        linkAfter ??= Entry.ApplyAfter.Clone();
        
        Dirty |= ImGui.Checkbox("##applyAppearance", ref appearance.Apply);
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
            
        Dirty |= ImGui.Checkbox("##applyEquipment", ref equipment.Apply);
        ImGui.SameLine();
        if (ImGui.CollapsingHeader("Equipment")) {
            using (ImRaii.PushIndent()) {
                ImGui.Checkbox("Revert to Game State##equipment", ref equipment.RevertToGame);
                DrawEquipment();
            }
        }
            
        Dirty |= ImGui.Checkbox("##applyWeapons", ref weapons.Apply);
        ImGui.SameLine();
        if (ImGui.CollapsingHeader("Weapons / Tools")) {
            using (ImRaii.PushIndent()) {
                DrawWeapons();
            }
        }
            
        if (ImGui.CollapsingHeader("Outfit Links")) {
            linksEditor ??= new OutfitLinksEditor(Character, Entry, linkBefore, linkAfter);
            Dirty |= linksEditor.Draw(CommonDetailsEditor.Name.OrDefault("This Outfit"));
        }
        
    }

    protected override void SaveEntry() {
        Entry.Equipment = equipment ?? Entry.Equipment;
        Entry.Appearance = appearance ?? Entry.Appearance;
        Entry.Weapons =  weapons ?? Entry.Weapons;
        Entry.ApplyBefore = linkBefore ?? Entry.ApplyBefore;
        Entry.ApplyAfter = linkAfter ?? Entry.ApplyAfter;
    }
    
    private void DrawAppearance() {
        appearance ??= Entry.Appearance.Clone();
        Dirty |= CustomizeEditor.Show(appearance);
    }

    private void DrawParameters() {
        foreach (var v in Enum.GetValues<AppearanceParameterKind>()) {
            DrawParameter(v);
        }
    }

    private void DrawParameter(AppearanceParameterKind kind) {
        appearance ??= Entry.Appearance.Clone();
        var param = appearance[kind];
        CustomizeEditor.ShowApplyEnableCheckbox(kind.PrettyName(), ref param.Apply, ref appearance.Apply);
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
        Dirty |= param.ShowEditor($"{kind}##paramEditor_{kind}", kind);
    }

    private void DrawEquipment() {
        equipment ??= Entry.Equipment.Clone();
        EquipmentDisplay.DrawEquipment(equipment, EquipmentDisplayFlags.None, Character, FolderGuid); 
    }
    
    private void DrawWeapons() {
        weapons ??= Entry.Weapons.Clone();
        
        foreach (var (classJobId, classWeapons) in weapons.ClassWeapons) {
            var classJob = DataManager.GetExcelSheet<ClassJob>().GetRow(classJobId);
            using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f), !weapons.Apply)) {
                Dirty |= ImGui.Checkbox($"##applyWeapons_ClassJob#{classJobId}", ref classWeapons.Apply);
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
        weapons ??= Entry.Weapons.Clone();
        var equip = classWeapons[slot];
        var classJob = DataManager.GetExcelSheet<ClassJob>().GetRow(classJobId);
        using (ImRaii.Group()) {
            using (ImRaii.Group()) {
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));
                using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f), !weapons.Apply || !classWeapons.Apply)) {
                    Dirty |= ImGui.Checkbox($"##enable_ClassJob#{classJobId}_{slot}", ref equip.Apply);
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
                    
                    Dirty = true;
                }

                AdvancedMaterialsDisplay.ShowAdvancedMaterialsDisplay(weapon, $"{classJob.Abbreviation.ExtractText()} {slot.PrettyName()}");
                if (weapon is { } ae) {
                    ImGui.SameLine();
                    Dirty |= StainPicker.Show($"{classJob.Abbreviation.ExtractText()} {slot}, Stain 1##{slot}_stain1", ref ae.Stain.Stain, new Vector2(s.Y));
                    ImGui.SameLine();
                    Dirty |= StainPicker.Show($"{classJob.Abbreviation.ExtractText()} {slot}, Stain 2##{slot}_stain2", ref ae.Stain.Stain2, new Vector2(s.Y));
                }

                ImGui.EndGroup();
                
                Dirty |= ModListDisplay.Show(weapon, $"{classJob.Abbreviation.ExtractText()} {slot.PrettyName()}");
            }
        }
    }
}
