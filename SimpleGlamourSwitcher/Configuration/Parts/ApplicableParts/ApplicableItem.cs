using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public abstract record ApplicableItem : Applicable<HumanSlot>, IHasModConfigs, IHasCustomizePlusTemplateConfigs {
    public List<OutfitModConfig> ModConfigs { get; set; } = new();
    public List<CustomizeTemplateConfig> CustomizePlusTemplateConfigs { get; set; } = new();
    public List<ApplicableMaterial> Materials = new();

    public abstract EquipItem GetEquipItem(HumanSlot slot);

    public abstract override void ApplyToCharacter(HumanSlot slot, ref bool requestRedraw);
}
