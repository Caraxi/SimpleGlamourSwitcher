using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public abstract record ApplicableItem : Applicable<HumanSlot>, IHasModConfigs {
    public List<OutfitModConfig> ModConfigs { get; set; } = new();


    public abstract EquipItem GetEquipItem(HumanSlot slot);

    public abstract override void ApplyToCharacter(HumanSlot slot, ref bool requestRedraw);
}
