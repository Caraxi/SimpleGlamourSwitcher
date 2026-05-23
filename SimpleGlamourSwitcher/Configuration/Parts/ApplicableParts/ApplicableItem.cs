using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public abstract record ApplicableItem<T> : Applicable<T>, IHasModConfigs, IHasCustomizePlusTemplateConfigs {
    public List<OutfitModConfig> ModConfigs { get; set; } = new();
    public List<CustomizeTemplateConfig> CustomizePlusTemplateConfigs { get; set; } = new();
    public List<ApplicableMaterial> Materials = new();

    public abstract EquipItem GetEquipItem(HumanSlot slot);
    public abstract EquipItem GetEquipItem(EquipSlot slot);

    public abstract override void ApplyToCharacter(T slot, ref bool requestRedraw);

    public override bool TryUpdate(Applicable newValues, UpdateApplicableFlags flags = UpdateApplicableFlags.None) {
        if (newValues is not ApplicableItem<T> n) return false;

        ModConfigs = n.ModConfigs.Clone();
        CustomizePlusTemplateConfigs = n.CustomizePlusTemplateConfigs.Clone();
        Materials = n.Materials.Clone();
        
        return base.TryUpdate(newValues, flags);
    }
}
