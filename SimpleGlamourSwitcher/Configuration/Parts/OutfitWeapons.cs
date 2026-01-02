using System.Collections;
using ECommons;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.Utility;
using ItemManager = SimpleGlamourSwitcher.Service.ItemManager;

namespace SimpleGlamourSwitcher.Configuration.Parts;

public record OutfitClassWeapons : Applicable {
    public ApplicableWeapon MainHand = new();
    public ApplicableWeapon OffHand = new();
    
    public override void ApplyToCharacter(ref bool requestRedraw) {
        if (!Apply) return;
        MainHand.ApplyToCharacter(EquipSlot.MainHand, ref requestRedraw);
        OffHand.ApplyToCharacter(EquipSlot.OffHand, ref requestRedraw);
    }
    
    public ApplicableWeapon this[EquipSlot slot] {
        get {
            switch (slot) {
                case EquipSlot.MainHand: return MainHand;
                case EquipSlot.OffHand: return OffHand;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "Only weapons/tools are supported.");
            }
        }
    }
}


public static class E {
    
}

[JsonObject]
public record OutfitWeapons : Applicable {
    public Dictionary<uint, OutfitClassWeapons> ClassWeapons = new(); 
    
    public override void ApplyToCharacter(ref bool requestRedraw) {
        if (!Apply) return;
        PluginLog.Verbose("ApplyToCharacter");
        var activeBaseClass = PlayerStateService.ClassJob.ValueNullable?.ClassJobParent.ValueNullable;
        if (activeBaseClass != null && ClassWeapons.TryGetValue(activeBaseClass.Value.RowId, out var cjWeapons) && cjWeapons.Apply) {
            cjWeapons.ApplyToCharacter(ref requestRedraw);
        }
    }

    public static OutfitWeapons FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, GlamourerState glamourerState, Guid effectiveCollectionId) {
        var glamourerEquipment = glamourerState.Equipment;
        var mainHand = glamourerEquipment.MainHand;
        var mainHandItem = PluginService.ItemManager.Resolve(EquipSlot.MainHand, mainHand.ItemId);

        if (!mainHandItem.Valid) return new OutfitWeapons();
        if (!DataManager.GetExcelSheet<Item>().TryGetRow(mainHandItem.ItemId.Id, out var mainHandData)) return new OutfitWeapons();


        var weaponDict = new Dictionary<uint, OutfitClassWeapons>();


        foreach (var classJob in DataManager.GetExcelSheet<ClassJob>().OrderBy(c => c.Role).Where(c => c.RowId != 0 && c.ClassJobParent.RowId == c.RowId)) {
            if (!mainHandData.IsEquipableWeaponOrToolForClassSlot(classJob, EquipSlot.MainHand)) continue;
            
            var ocw = new OutfitClassWeapons() {
                MainHand = GetWeaponState(defaultOptionsProvider, EquipSlot.MainHand, glamourerEquipment.MainHand, glamourerState.Materials, effectiveCollectionId)
            };

            var mhItem = DataManager.GetExcelSheet<Item>().GetRowOrDefault(ocw.MainHand.ItemId.Id);
            
            if (mhItem != null && mhItem.Value.ToEquipType().Offhand() is not (FullEquipType.Unknown or FullEquipType.Gig )) {
                ocw.OffHand = GetWeaponState(defaultOptionsProvider, EquipSlot.OffHand, glamourerEquipment.OffHand, glamourerState.Materials, effectiveCollectionId);
            } else {
                ocw.OffHand = new ApplicableWeapon { ItemId = ItemManager.NothingId(EquipSlot.OffHand), Apply = false };
            }

            ocw.Apply = ocw.MainHand.Apply || ocw.OffHand.Apply;
            weaponDict.Add(classJob.RowId, ocw);
        }
        
        return new OutfitWeapons {
            Apply = weaponDict.Any(w => w.Value.Apply),
            ClassWeapons = weaponDict
        };
    }

    private static ApplicableWeapon GetWeaponState(IDefaultOutfitOptionsProvider defaultOptionsProvider, EquipSlot equipSlot, GlamourerItem item, Dictionary<MaterialValueIndex, GlamourerMaterial>? materials, Guid penumbraCollection) {
        var equipItem = PluginService.ItemManager.Resolve(equipSlot, item.ItemId);
        
        if (!equipItem.Valid || equipItem.Id.IsBonusItem) {
            PluginLog.Warning($"Invalid item in {equipSlot}. Skipping.");
            return new ApplicableWeapon { Apply = false, ItemId = ItemManager.NothingId(equipSlot)};
        }

        return new ApplicableWeapon {
            Apply = !defaultOptionsProvider.DefaultDisabledWeaponSlots.Contains(equipSlot),
            ItemId = equipItem.ItemId,
            Stain = new ApplicableStain { Apply = item.ApplyStain, Stain = item.Stain, Stain2 = item.Stain2 },
            ModConfigs = OutfitModConfig.GetModListFromWeapon(equipSlot, equipItem, penumbraCollection),
            Materials = ApplicableMaterial.FilterForSlot(materials ?? [], equipSlot)
        };
    }
    
}
