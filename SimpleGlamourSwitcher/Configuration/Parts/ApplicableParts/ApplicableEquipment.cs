using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableEquipment : ApplicableItem {
    public Penumbra.GameData.Structs.ItemId ItemId;
    public ApplicableStain Stain = new();

    
    public override void ApplyToCharacter(HumanSlot slot, ref bool requestRedraw) {
        if (!Apply) return;

        var equipItem = PluginService.ItemManager.Resolve(slot.ToEquipSlot(), ItemId.Id);

        if (PluginConfig.LogActionsToChat) {
            Notice.Show($"Apply to {slot}: {equipItem.Name} [{equipItem.ItemId.Id} / {ItemId.StripModifiers}");
        }
        
        PluginLog.Debug($"Apply to {slot}: {equipItem.Name} / {equipItem.ItemId}");

        var ec = GlamourerIpc.SetItem.Invoke(0, (ApiEquipSlot) slot.ToEquipSlot(), equipItem.ItemId.Id, Stain.AsList());

        if (ec == GlamourerApiEc.Success) {
            ModManager.ApplyMods(slot, ModConfigs);
        } else {
            PluginLog.Error($"Failed to apply item: {ec}");
        }
    }

    public static ApplicableEquipment FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, HumanSlot slot, GlamourerItem item, Dictionary<MaterialValueIndex, GlamourerMaterial>? materials, Guid penumbraCollection) {
        var equipItem = PluginService.ItemManager.Resolve(slot.ToEquipSlot(), item.ItemId.Id);
        
        return new ApplicableEquipment {
            Apply = item.Apply && !defaultOptionsProvider.DefaultDisabledEquipmentSlots.Contains(slot),
            ItemId = item.ItemId,
            Stain = new ApplicableStain { Apply = item.ApplyStain, Stain = item.Stain, Stain2 = item.Stain2 },
            ModConfigs = OutfitModConfig.GetModListFromEquipment(slot, equipItem, penumbraCollection),
            Materials = ApplicableMaterial.FilterForSlot(materials ?? [], slot)
        };
    }

    public override EquipItem GetEquipItem(HumanSlot slot) {
        return PluginService.ItemManager.Resolve(slot.ToEquipSlot(), ItemId);
    }
}
