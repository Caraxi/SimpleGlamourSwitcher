using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableCustomizeModable : ApplicableCustomize, IHasModConfigs, IHasCustomizePlusTemplateConfigs {
    public List<OutfitModConfig> ModConfigs { get; set; } = new();
    public List<CustomizeTemplateConfig> CustomizePlusTemplateConfigs { get; set; } = new();
        
    public override void ApplyToCharacter(CustomizeIndex slot, ref bool requestRedraw) {
        if (!Apply) return;
        Notice.Show($"Applying mods for {slot}");
        requestRedraw = true;
        ModManager.ApplyMods(slot, ModConfigs);
        
        if (ActiveCharacter?.CustomizePlusProfile != null) {
            CustomizePlus.ApplyTemplateConfig(ActiveCharacter.CustomizePlusProfile.Value, CustomizePlusTemplateConfigs, slot);
        }
    }
    
    public static ApplicableCustomizeModable FromExistingState(CustomizeIndex slot, GlamourerCustomize? customize, Guid penumbraCollectionId, IDefaultOutfitOptionsProvider defaultOptionsProvider) {
        if (customize?[slot] == null) return new ApplicableCustomizeModable();
        
        return new ApplicableCustomizeModable {
            Apply = customize[slot]!.Apply && defaultOptionsProvider.DefaultEnabledCustomizeIndexes.Contains(slot),
            Value = customize[slot]!.Value,
            ModConfigs = OutfitModConfig.GetModListFromCustomize(slot, customize, penumbraCollectionId),
        };
    }
    // public static ApplicableCustomize FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, CustomizeIndex slot, GlamourerCustomizeOption? customize) {

    public static ApplicableCustomizeModable FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, CustomizeIndex slot, GlamourerCustomize? customize, Guid penumbraCollectionId) {
        if (customize?[slot] == null) return new ApplicableCustomizeModable();
        
        return new ApplicableCustomizeModable {
            Apply = customize[slot]!.Apply && defaultOptionsProvider.DefaultEnabledCustomizeIndexes.Contains(slot),
            Value = customize[slot]!.Value,
            ModConfigs = OutfitModConfig.GetModListFromCustomize(slot, customize, penumbraCollectionId),
        };
    }
    
    
    
}


public interface IHasCustomizePlusTemplateConfigs {
    public List<CustomizeTemplateConfig> CustomizePlusTemplateConfigs { get; set; }
}

public record ApplicableCustomize : Applicable<CustomizeIndex> {
    public byte Value = 0;
    
    public override void ApplyToCharacter(CustomizeIndex slot, ref bool requestRedraw) {
        Notice.Show($"Applying {slot} without mods.");
    }
    
    
    public static ApplicableCustomize FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, CustomizeIndex slot, GlamourerCustomize? customize) {
        if (customize == null) return new ApplicableCustomize();
        
        return new ApplicableCustomize {
            Apply = (customize[slot]?.Apply ?? false) && defaultOptionsProvider.DefaultEnabledCustomizeIndexes.Contains(slot),
            Value = customize[slot]?.Value ?? 0,
        };
    }
    
}
