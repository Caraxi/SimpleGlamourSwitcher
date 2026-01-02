using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public abstract record ApplicableItem<T> : Applicable<T>, IHasModConfigs, IHasCustomizePlusTemplateConfigs {
    public List<OutfitModConfig> ModConfigs { get; set; } = new();
    public List<CustomizeTemplateConfig> CustomizePlusTemplateConfigs { get; set; } = new();
    public List<ApplicableMaterial> Materials = new();

    public abstract EquipItem GetEquipItem(HumanSlot slot);
    public abstract EquipItem GetEquipItem(EquipSlot slot);

    public abstract override void ApplyToCharacter(T slot, ref bool requestRedraw);
}
