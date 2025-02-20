using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableStain : Applicable<HumanSlot> {
    public byte Stain;
    public byte Stain2;
    
    
    public IReadOnlyList<byte> AsList() => [Stain, Stain2];

    public override void ApplyToCharacter(HumanSlot slot, ref bool requestRedraw) {
        
    }
}