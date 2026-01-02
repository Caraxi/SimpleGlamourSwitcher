using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.IPC.Glamourer;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableMaterial : Applicable<MaterialValueIndex> {
    [JsonIgnore] public MaterialValueIndex MaterialValueIndex;

    public string Index {
        get => MaterialValueIndex.Key.ToString("X16");
        set => MaterialValueIndex = value;
    }

    public float DiffuseR;
    public float DiffuseG;
    public float DiffuseB;
    public float SpecularR;
    public float SpecularG;
    public float SpecularB;
    public float SpecularA;
    public float EmissiveR;
    public float EmissiveG;
    public float EmissiveB;
    public float Gloss;

    public static List<ApplicableMaterial> FilterForSlot(Dictionary<MaterialValueIndex, GlamourerMaterial> materials, HumanSlot slot) {
        var list = new List<ApplicableMaterial>();

        foreach (var (mvi, material) in materials) {
            if (mvi.ToHumanSlot() != slot) continue;
            if (!material.Enabled) continue;
            if (material.Revert) continue;
            list.Add(new ApplicableMaterial {
                Apply = true,
                MaterialValueIndex = mvi,
                DiffuseR = material.DiffuseR,
                DiffuseG = material.DiffuseG,
                DiffuseB = material.DiffuseB,
                SpecularR = material.SpecularR,
                SpecularG = material.SpecularG,
                SpecularB = material.SpecularB,
                SpecularA = material.SpecularA,
                EmissiveR = material.EmissiveR,
                EmissiveG = material.EmissiveG,
                EmissiveB = material.EmissiveB,
                Gloss = material.Gloss,
            });
        }

        return list;
    }
    
    public static List<ApplicableMaterial> FilterForSlot(Dictionary<MaterialValueIndex, GlamourerMaterial> materials, EquipSlot slot) {
        var list = new List<ApplicableMaterial>();

        foreach (var (mvi, material) in materials) {
            if (mvi.ToEquipSlot() != slot) continue;
            if (!material.Enabled) continue;
            if (material.Revert) continue;
            list.Add(new ApplicableMaterial {
                Apply = true,
                MaterialValueIndex = mvi,
                DiffuseR = material.DiffuseR,
                DiffuseG = material.DiffuseG,
                DiffuseB = material.DiffuseB,
                SpecularR = material.SpecularR,
                SpecularG = material.SpecularG,
                SpecularB = material.SpecularB,
                SpecularA = material.SpecularA,
                EmissiveR = material.EmissiveR,
                EmissiveG = material.EmissiveG,
                EmissiveB = material.EmissiveB,
                Gloss = material.Gloss,
            });
        }

        return list;
    }

    public override void ApplyToCharacter(MaterialValueIndex slot, ref bool requestRedraw) { }
}
