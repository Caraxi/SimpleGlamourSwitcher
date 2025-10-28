using Glamourer.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableBonus : ApplicableItem {
    public ulong BonusItemId;
    
    public override void ApplyToCharacter(HumanSlot slot, ref bool requestRedraw) {
        if (!Apply) return;
        var equipItem = GetEquipItem(slot);
        
        PluginLog.Debug($"Apply to {slot}: {equipItem.Name} / {equipItem.ItemId}");
        
        ModManager.ApplyMods(slot, ModConfigs);
        switch (slot) {
            case HumanSlot.Face:
                GlamourerIpc.SetBonusItem.Invoke(0, ApiBonusSlot.Glasses, BonusItemId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(slot), slot, null);
        }
    }

    public static ApplicableBonus FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, HumanSlot slot, GlamourerBonuses glamourerStateBonus, Dictionary<MaterialValueIndex, GlamourerMaterial>? materials, Guid penumbraCollection) {
        switch (slot) {
            case HumanSlot.Face: {
                if (glamourerStateBonus.Glasses?.BonusId == 0) {
                    return new ApplicableBonus { Apply = !defaultOptionsProvider.DefaultDisabledEquipmentSlots.Contains(HumanSlot.Face) };
                }

                var bonusItem = PluginService.ItemManager.Resolve(BonusItemFlag.Glasses, glamourerStateBonus.Glasses?.BonusId ?? 0);
                return new ApplicableBonus {
                    Apply = !defaultOptionsProvider.DefaultDisabledEquipmentSlots.Contains(HumanSlot.Face),
                    BonusItemId = glamourerStateBonus.Glasses?.BonusId ?? 0,
                    ModConfigs = OutfitModConfig.GetModListFromEquipment(HumanSlot.Face, bonusItem, penumbraCollection),
                    Materials = ApplicableMaterial.FilterForSlot(materials ?? [], slot),
                };
            }
            default: {
                throw new System.NotImplementedException();
            }
        }
    }

    public override EquipItem GetEquipItem(HumanSlot slot) {
        var bonusSlot = slot.ToBonusSlot();
        switch (bonusSlot) {
            case BonusItemFlag.Unknown:
            case BonusItemFlag.UnkSlot:
                throw new NotImplementedException();
        default:
                return PluginService.ItemManager.Resolve(bonusSlot, BonusItemId);
        }
    }
}
