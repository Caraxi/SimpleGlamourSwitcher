using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImSharp;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Windows;
using SimpleGlamourSwitcher.Utility;
using ItemManager = SimpleGlamourSwitcher.Service.ItemManager;

namespace SimpleGlamourSwitcher.UserInterface.Components;

[Flags]
public enum EquipmentDisplayFlags : uint {
    None = 0,
    NoApplyToggles = 1,
    NoCustomizePlus = 2,
    NoModEditing = 4,
    ContextNoSetToCurrent = 8,
    ContextNoClearSlot = 16,
    
    EnableCustomItemPicker = 32,
    Simple = NoApplyToggles | NoCustomizePlus | NoModEditing | ContextNoSetToCurrent,
}

public static class EquipmentDisplay {
    
    private static LazyAsync<OrderedDictionary<Guid, ItemConfigFile>> items = new(() => ActiveCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, ItemConfigFile>()) : ActiveCharacter.GetEntries<ItemConfigFile>());
    private static LazyAsync<OrderedDictionary<Guid, ItemConfigFile>> sharedItems = new(() => SharedCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, ItemConfigFile>()) : SharedCharacter.GetEntries<ItemConfigFile>());

    static EquipmentDisplay() {
        PluginState.InvalidateEntryCache += ClearCustomItemCache;
    }

    private static void ClearCustomItemCache() {
        items = new(() => ActiveCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, ItemConfigFile>()) : ActiveCharacter.GetEntries<ItemConfigFile>());
        sharedItems = new(() => SharedCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, ItemConfigFile>()) : SharedCharacter.GetEntries<ItemConfigFile>());
    }

    public static bool DrawEquipment(OutfitEquipment equipment, EquipmentDisplayFlags flags = EquipmentDisplayFlags.None, CharacterConfigFile? character = null, Guid? folderGuid = null) {
        var dirty = false;
        foreach (var s in Common.GetGearSlots()) {
            dirty |= ShowSlot(equipment, s, flags, character, folderGuid);
        }

        return dirty;
    }

    public static bool ShowSlot(ApplicableItem<HumanSlot> equip, HumanSlot slot, bool disable = false, EquipmentDisplayFlags flags = EquipmentDisplayFlags.None, CharacterConfigFile? character = null, Guid? folderGuid = null) {
        var dirty = false;

        using (ImRaii.Group()) {
            if (!flags.HasFlag(EquipmentDisplayFlags.NoApplyToggles)) {
                using (ImRaii.Group()) {
                    ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));
                    using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f), disable)) {
                        dirty |= ImGui.Checkbox($"##enable_{slot}", ref equip.Apply);
                    }
            
                    if (ImGui.IsItemHovered()) {
                        using (ImRaii.Tooltip()) {
                            ImGui.Text($"Enable {slot.PrettyName()}");
                            if (disable) {
                                ImGui.TextDisabled("This option is will not be applied because the Equipment option is not enabled for this outfit.");
                            }
                        }
                    }
                }

                ImGui.SameLine();
            }
            using (ImRaii.Group()) {
                dirty |= ShowSlot(slot, equip, flags, character, folderGuid);
            }
        }
        
        
        return dirty;
    }
    
    private static bool ShowSlot(OutfitEquipment equipment, HumanSlot slot, EquipmentDisplayFlags flags, CharacterConfigFile? character, Guid? folderGuid) {
        var dirty = false;
        var equip = equipment[slot];
        
        dirty |= ShowSlot(equip, slot, !equipment.Apply, flags, character, folderGuid);

        if (slot == HumanSlot.Head) {
            ImGui.SameLine();
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
            using (ImRaii.Group()) {
                dirty |= equipment.HatVisible.ShowToggleEditor("Headwear Visible", flags.HasFlag(EquipmentDisplayFlags.NoApplyToggles));
                dirty |= equipment.VisorToggle.ShowToggleEditor("Visor Toggle", flags.HasFlag(EquipmentDisplayFlags.NoApplyToggles));
            }
            ImGui.Spacing();
        }

        if (slot == HumanSlot.Body) {
            ImGui.SameLine();
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
            using (ImRaii.Group()) {
                dirty |= equipment.VieraEarsVisible.ShowToggleEditor("Ears Visible", flags.HasFlag(EquipmentDisplayFlags.NoApplyToggles));
                dirty |= equipment.WeaponVisible.ShowToggleEditor("Weapon Visible", flags.HasFlag(EquipmentDisplayFlags.NoApplyToggles));
            }
            ImGui.Spacing();
        }

        return dirty;
    }
    
    private static bool ShowSlot(HumanSlot slot, ApplicableItem<HumanSlot> equipment, EquipmentDisplayFlags flags, CharacterConfigFile? character, Guid? folderGuid) {
        var dirty = false;
        var equipItem = equipment.GetEquipItem(slot);
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One))
        using (ImRaii.PushId($"State_{slot}")) {
            var s = new Vector2(300 * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);
            ItemIcon.Draw(slot, equipItem);
            if (flags.HasFlag(EquipmentDisplayFlags.EnableCustomItemPicker)) {
                if (flags.HasFlag(EquipmentDisplayFlags.EnableCustomItemPicker) && ImGui.IsPopupOpen($"CustomItemPicker_{slot}")) {
                    var dl = ImGui.GetWindowDrawList();
                    dl.AddRectFilled(
                        ImGui.GetItemRectMin() - Vector2.One,
                        ImGui.GetItemRectMax() + new Vector2(8 * ImGuiHelpers.GlobalScale, 1),
                        ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudViolet));
                    
                    dl.DrawEmptySlotIcon(slot, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

                }
                
                if (ImGui.IsItemHovered()) {
                    var dl = ImGui.GetWindowDrawList();
                    dl.AddRect(ImGui.GetItemRectMin() - Vector2.One, ImGui.GetItemRectMax() + Vector2.One, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }
                
                if (ImGui.IsItemClicked()) {
                    ImGui.OpenPopup($"CustomItemPicker_{slot}");
                    ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() with { Y = -1 } + new Vector2(5 * ImGuiHelpers.GlobalScale, 0));
                }
                
                using (ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2))
                using (ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudViolet))
                using (var popup = ImRaii.Popup($"CustomItemPicker_{slot}")) {
                    if (popup.Success) {
                        using (ImRaii.Child($"CustomItemPickerScroll_{slot}", (s * 1.35f) with { Y = s.Y * 10 }, false, ImGuiWindowFlags.HorizontalScrollbar)) {

                            if (ImGui.IsWindowHovered()) {
                                ImGui.SetScrollX(ImGui.GetScrollX() - (ImGui.GetIO().MouseWheel * 50 * ImGuiHelpers.GlobalScale));
                            }
                            
                            items.CreateValueIfNotCreated();
                            sharedItems.CreateValueIfNotCreated();

                            if (!items.IsValueCreated || !sharedItems.IsValueCreated) {
                                ImGui.TextDisabled("Loading Items...");
                            } else {
                                var any = false;

                                foreach (var entry in items.Value.Values.Concat(sharedItems.Value.Values).Where(i => i.Slot == slot).OrderBy(i => i.SortName).ThenBy(i => i.Name, StringComparer.InvariantCultureIgnoreCase)) {
                                    
                                    if (any) ImGui.SameLine();
                                    
                                    any = true;
                                    var folder = entry.ConfigFile?.Folders.GetValueOrDefault(entry.Folder);
                                    var style = folder?.OutfitPolaroidStyle ?? entry.ConfigFile?.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
                                    
                                    if (Polaroid.Button((entry as IImageProvider)?.GetImage(), entry.ImageDetail, entry.Name, entry.Guid, style.FitTo(ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin()))) {
                                        entry.Apply().ConfigureAwait(false);
                                    }
                                }
                                
                                if (!any) {
                                    ImGui.TextDisabled($"No custom {slot.ToName()} saved.");
                                }
                            }
                        }
                    }
                }
            }
            
            var contextCharacter = character ?? ActiveCharacter;
            if (contextCharacter != null && !flags.CheckAll(EquipmentDisplayFlags.ContextNoSetToCurrent | EquipmentDisplayFlags.ContextNoClearSlot)) {
                dirty |= HandleSlotContextMenu($"{slot.ToName()}##ItemContext", slot, equipment, contextCharacter, folderGuid, flags, (a) => a.Equipment[slot]);

            }

            ImGui.SameLine();
            using (ImRaii.Group()) {
                ImGui.SetNextItemWidth(s.X - s.Y + ImGui.GetStyle().ItemSpacing.X - (equipment is ApplicableEquipment ? s.Y * 2 + ImGui.GetStyle().ItemSpacing.X * 2 : 0));

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

                AdvancedMaterialsDisplay.ShowAdvancedMaterialsDisplay(equipment, $"{slot.PrettyName()}");
                if (equipment is ApplicableEquipment ae) {
                    ImGui.SameLine();
                    dirty |= StainPicker.Show($"{slot}, Stain 1##{slot}_stain1", ref ae.Stain.Stain, new Vector2(s.Y));
                    ImGui.SameLine();
                    dirty |= StainPicker.Show($"{slot}, Stain 2##{slot}_stain2", ref ae.Stain.Stain2, new Vector2(s.Y));
                }

                ImGui.EndGroup();
                
                dirty |= ModListDisplay.Show(equipment, $"{slot.PrettyName()}", displayOnly: flags.HasFlag(EquipmentDisplayFlags.NoModEditing), includeCustomizePlus: !flags.HasFlag(EquipmentDisplayFlags.NoCustomizePlus));
            }
        }
        return dirty;
    }

    private static bool HandleSlotContextMenu(string label, HumanSlot slot, Applicable applicable, CharacterConfigFile character, Guid? folderGuid = null, EquipmentDisplayFlags flags = EquipmentDisplayFlags.None, Func<OutfitConfigFile, Applicable>? getApplicable = null ) {
        var dirty = false;
        if (ImGui.BeginPopupContextItem($"Context_{label}")) {
            ImGui.Text(label.Split("##")[0]);
            ImGui.Separator();


            if (!flags.HasFlag(EquipmentDisplayFlags.ContextNoSetToCurrent)) {
                if (getApplicable != null && applicable is ApplicableBonus or ApplicableEquipment && ImGui.MenuItem("Replace with Currently Equipped")) {
                    dirty = true;
                    try {
                        var o = OutfitConfigFile.CreateFromLocalPlayer(character, folderGuid ?? Guid.Empty, character.GetOptionsProvider(folderGuid ?? Guid.Empty));
                        var m = getApplicable(o);

                        if (applicable is ApplicableItem<HumanSlot> originalApplicableItem && m is ApplicableItem<HumanSlot> newApplicableItem) {
                            originalApplicableItem.Materials = newApplicableItem.Materials;
                            originalApplicableItem.ModConfigs = newApplicableItem.ModConfigs;
                            switch (applicable) {
                                case ApplicableEquipment originalEquipment when m is ApplicableEquipment newEquipment:
                                    originalEquipment.ItemId = newEquipment.ItemId;
                                    originalEquipment.Stain = newEquipment.Stain;
                                    break;
                                case ApplicableBonus originalBonus when m is ApplicableBonus newBonus:
                                    originalBonus.BonusItemId = newBonus.BonusItemId;
                                    break;
                            }

                        }
                    
                    } catch (Exception ex) {
                        PluginLog.Error(ex, "Error replacing equipment");
                        //
                    }
                }
            }

            if (!flags.HasFlag(EquipmentDisplayFlags.ContextNoClearSlot)) {
                if (getApplicable != null && applicable is ApplicableBonus or ApplicableEquipment && ImGui.MenuItem($"Clear {slot.ToName()} Equipment")) {
                    dirty = true;
                    try {
                        if (applicable is ApplicableEquipment equipment) {
                            equipment.ItemId = ItemManager.NothingId(slot.ToEquipSlot());
                        } else if (applicable is ApplicableBonus bonus) {
                            bonus.BonusItemId = 0;
                        }
                    
                    } catch (Exception ex) {
                        PluginLog.Error(ex, "Error replacing equipment");
                        //
                    }
                }
            }

            ImGui.EndPopup();
        }

        return dirty;
    }
}
