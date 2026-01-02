using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;
using ItemManager = SimpleGlamourSwitcher.Service.ItemManager;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableWeapon : ApplicableItem<EquipSlot> {
    public Penumbra.GameData.Structs.ItemId ItemId;
    public ApplicableStain Stain = new();
    
    public override void ApplyToCharacter(EquipSlot slot, ref bool requestRedraw) {
        if (!Apply) return;

        var equipItem = PluginService.ItemManager.Resolve(slot, ItemId);

        if (PluginConfig.LogActionsToChat) {
            Notice.Show($"Apply to {slot}: {equipItem.Name} [{equipItem.ItemId.Id} / {ItemId}");
        }
        
        PluginLog.Debug($"Apply to {slot}: {equipItem.Name} / {equipItem.ItemId}");

        var ec = GlamourerIpc.SetItem.Invoke(0, (ApiEquipSlot) slot, equipItem.ItemId.Id, Stain.AsList());

        if (ec == GlamourerApiEc.Success) {
            ModManager.ApplyMods(slot, ModConfigs);
        } else {
            PluginLog.Error($"Failed to apply item: {ec}");
        }
        
        if (ActiveCharacter?.CustomizePlusProfile != null) {
            CustomizePlus.ApplyTemplateConfig(ActiveCharacter.CustomizePlusProfile.Value, CustomizePlusTemplateConfigs, slot);
        }
    }

    public override EquipItem GetEquipItem(HumanSlot slot) {
        return PluginService.ItemManager.Resolve(slot.ToEquipSlot(), ItemId);
    }
    public override EquipItem GetEquipItem(EquipSlot slot) {
        return PluginService.ItemManager.Resolve(slot, ItemId);
    }
}
