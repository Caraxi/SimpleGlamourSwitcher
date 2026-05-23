using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableStain : Applicable<HumanSlot> {
    public byte Stain;
    public byte Stain2;
    
    
    public IReadOnlyList<byte> AsList() => [Stain, Stain2];

    public override void ApplyToCharacter(HumanSlot slot, ref bool requestRedraw) {
        
    }

    public override bool TryUpdate(Applicable newValues, UpdateApplicableFlags flags = UpdateApplicableFlags.None) {
        if (newValues is not ApplicableStain n) return false;
        Stain = n.Stain;
        Stain2 = n.Stain2;
        return base.TryUpdate(newValues, flags);
    }
}