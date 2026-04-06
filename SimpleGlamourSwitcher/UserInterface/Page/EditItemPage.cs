using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditItemPage(CharacterConfigFile character, Guid folderGuid, ItemConfigFile? item) : EntryEditorPage<ItemConfigFile>(character, folderGuid, item) {
    public override string TypeName => "Item";
    private ApplicableItem<HumanSlot>? applicable;
    private HumanSlot slot = item?.Slot ?? HumanSlot.Body;

    protected override void DrawEditor(ref WindowControlFlags controlFlags) {
        applicable ??= slot == HumanSlot.Face ? Entry.Bonus.Clone() ?? ApplicableBonus.FromNothing() : Entry.Equipment.Clone() ?? ApplicableEquipment.FromNothing(slot);
        ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("Slot", $"{slot.ToName()}", ImGuiComboFlags.HeightLarge)) {
            ImGui.TextDisabled("Changing slot will cause the item to be lost. Hold SHIFT to confirm");
            foreach (var s in Common.GetGearSlots()) {
                if (ImGui.Selectable($"{s.ToName()}", slot == s, ImGui.GetIO().KeyShift ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled)) {
                    if (slot != s) applicable = s == HumanSlot.Face ? ApplicableBonus.FromNothing() : ApplicableEquipment.FromNothing(s);
                    slot = s;
                }
            }

            ImGui.EndCombo();
        }

        EquipmentDisplay.ShowSlot(applicable, slot, false, EquipmentDisplayFlags.NoApplyToggles, Character, FolderGuid);
    }

    protected override void SaveEntry() {
        Entry.Item = applicable ?? Entry.Item;
        Entry.Slot = slot;
    }
}
