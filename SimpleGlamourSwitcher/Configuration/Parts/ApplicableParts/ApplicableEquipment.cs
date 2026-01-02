using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;
using ItemManager = SimpleGlamourSwitcher.Service.ItemManager;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableEquipment : ApplicableItem<HumanSlot> {
    public Penumbra.GameData.Structs.ItemId ItemId;
    public ApplicableStain Stain = new();

    
    public override void ApplyToCharacter(HumanSlot slot, ref bool requestRedraw) {
        if (!Apply) return;

        var equipItem = PluginService.ItemManager.Resolve(slot.ToEquipSlot(), ItemId);

        if (PluginConfig.LogActionsToChat) {
            Notice.Show($"Apply to {slot}: {equipItem.Name} [{equipItem.ItemId.Id} / {ItemId}");
        }
        
        PluginLog.Debug($"Apply to {slot}: {equipItem.Name} / {equipItem.ItemId}");

        var ec = GlamourerIpc.SetItem.Invoke(0, (ApiEquipSlot) slot.ToEquipSlot(), equipItem.ItemId.Id, Stain.AsList());

        if (ec == GlamourerApiEc.Success) {
            ModManager.ApplyMods(slot, ModConfigs);
        } else {
            PluginLog.Error($"Failed to apply item: {ec}");
        }
        
        if (ActiveCharacter?.CustomizePlusProfile != null) {
            CustomizePlus.ApplyTemplateConfig(ActiveCharacter.CustomizePlusProfile.Value, CustomizePlusTemplateConfigs, slot);
        }
    }

    public static ApplicableEquipment FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, HumanSlot slot, GlamourerItem item, Dictionary<MaterialValueIndex, GlamourerMaterial>? materials, Guid penumbraCollection) {
        var equipItem = PluginService.ItemManager.Resolve(slot.ToEquipSlot(), item.ItemId);
        
        if (!equipItem.Valid || equipItem.Id.IsBonusItem) {
            PluginLog.Warning($"Invalid item in {slot}. Skipping.");
            return new ApplicableEquipment { Apply = false, ItemId = ItemManager.NothingId(slot.ToEquipSlot())};
        }
        
        return new ApplicableEquipment {
            Apply = item.Apply && !defaultOptionsProvider.DefaultDisabledEquipmentSlots.Contains(slot),
            ItemId = equipItem.ItemId,
            Stain = new ApplicableStain { Apply = item.ApplyStain, Stain = item.Stain, Stain2 = item.Stain2 },
            ModConfigs = OutfitModConfig.GetModListFromEquipment(slot, equipItem, penumbraCollection),
            Materials = ApplicableMaterial.FilterForSlot(materials ?? [], slot)
        };
    }

    public override EquipItem GetEquipItem(HumanSlot slot) {
        return PluginService.ItemManager.Resolve(slot.ToEquipSlot(), ItemId);
    }
    public override EquipItem GetEquipItem(EquipSlot slot) {
        return PluginService.ItemManager.Resolve(slot, ItemId);
    }
}
