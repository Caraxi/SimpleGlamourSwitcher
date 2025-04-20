using System.Collections.Concurrent;
using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;
using ItemManager = SimpleGlamourSwitcher.Service.ItemManager;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class ItemPicker {
    private static ConcurrentDictionary<HumanSlot, List<EquipItem>> _items = new();

    private static void PopulateItemList(HumanSlot slot) {
        var items = new List<EquipItem>();

        switch (slot) {
            case HumanSlot.Face: {
                items.Add(EquipItem.BonusItemNothing(BonusItemFlag.Glasses));
                items.AddRange(DictBonusItems.Values.Where(b => b.Type == FullEquipType.Glasses));
                break;
            }

            default: {
                var equipSlot = slot.ToEquipSlot();
                if (equipSlot == EquipSlot.Unknown) break;

                if (equipSlot == EquipSlot.LFinger) equipSlot = EquipSlot.RFinger;

                var preSort = new List<EquipItem>();
                foreach (var i in DataManager.GetExcelSheet<Item>().Where(i => i.EquipSlotCategory.RowId == (uint)equipSlot)) {
                    var item = EquipItem.FromArmor(i);
                    if (item.Valid) preSort.Add(item);
                }

                items.Add(ItemManager.NothingItem(equipSlot));
                items.Add(ItemManager.SmallClothesItem(equipSlot));
                items.AddRange(preSort.OrderBy(i => i.Name.ToLowerInvariant()));

                break;
            }
        }

        _items[slot] = items;
    }

    private static string _itemSearch = string.Empty;
    private static bool _doScroll;

    public static bool Show(string label, HumanSlot slot, ref EquipItem item) {
        var edit = false;
        if (!Common.GetGearSlots().Contains(slot)) throw new ArgumentOutOfRangeException(nameof(slot), $"{slot}", $"{slot} is not a valid item slot.");

        using (ImRaii.PushColor(ImGuiCol.Border, 0xFFFFFFFF))
        using (ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2)) {
            if (ImGui.BeginCombo(label, item.Name, ImGuiComboFlags.HeightLargest)) {
                var appearing = ImGui.IsWindowAppearing();

                if (appearing) {
                    _doScroll = true;
                    ImGui.SetKeyboardFocusHere();
                    _itemSearch = string.Empty;
                }
                
                #if DEBUG
                if (ImGui.GetIO().KeyAlt) {
                    ImGui.TextDisabled($"Current: {item.Id}");
                }
                #endif

                ImGui.InputTextWithHint("##search", "Search...", ref _itemSearch, 100);

                ImGui.Separator();

                if (ImGui.BeginChild("##itemList", new Vector2(ImGui.GetContentRegionAvail().X, 400 * ImGuiHelpers.GlobalScale))) {
                    if (_items.TryGetValue(slot, out var list)) {
                        foreach (var i in list) {
                            if (!string.IsNullOrWhiteSpace(_itemSearch) && !i.Name.Contains(_itemSearch, StringComparison.InvariantCultureIgnoreCase)) continue;

                            if (_doScroll && i.Id == item.Id) {
                                ImGui.SetScrollHereY(0.5f);
                                _doScroll = false;
                            }

                            if (ImGui.Selectable($"{i.Name}", i.Id == item.Id)) {
                                item = i;
                                edit = true;
                                ImGui.CloseCurrentPopup();
                            }
                            
                            #if DEBUG
                            if (ImGui.GetIO().KeyAlt) {
                                ImGui.SameLine();
                                ImGui.TextDisabled($"{i.Id}");
                            }
                            #endif
                            
                        }

                        if (list.Count > 0) {
                            _doScroll = false;
                        }
                    } else {
                        list = [];
                        Task.Run(() => { PopulateItemList(slot); }).ConfigureAwait(false);
                        _items[slot] = list;
                    }
                }

                ImGui.EndChild();

                ImGui.EndCombo();
            }
        }

        return edit;
    }
}
