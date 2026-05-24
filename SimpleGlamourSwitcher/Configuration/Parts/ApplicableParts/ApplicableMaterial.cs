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


    public string Mode = "Legacy";
    public float DiffuseR;
    public float DiffuseG;
    public float DiffuseB;
    public float SpecularR;
    public float SpecularG;
    public float SpecularB;
    public float EmissiveR;
    public float EmissiveG;
    public float EmissiveB;

    // Legacy
    public float SpecularA;
    public float Gloss;

    // Dawntrail
    public float Metalness;
    public float Roughness;
    public float Sheen;
    public float SheenTint;
    public float SheenAperture;

    public static List<ApplicableMaterial> FilterForSlot(Dictionary<MaterialValueIndex, GlamourerMaterial> materials, HumanSlot slot) {
        var list = new List<ApplicableMaterial>();

        foreach (var (mvi, material) in materials) {
            if (mvi.ToHumanSlot() != slot) continue;
            if (!material.Enabled) continue;
            if (material.Revert) continue;
            list.Add(new ApplicableMaterial {
                Apply = true,
                MaterialValueIndex = mvi,
                Mode = material.Mode,
                DiffuseR = material.DiffuseR,
                DiffuseG = material.DiffuseG,
                DiffuseB = material.DiffuseB,
                SpecularR = material.SpecularR,
                SpecularG = material.SpecularG,
                SpecularB = material.SpecularB,
                EmissiveR = material.EmissiveR,
                EmissiveG = material.EmissiveG,
                EmissiveB = material.EmissiveB,

                // Dawntrail
                Metalness = material.Metalness,
                Roughness = material.Roughness,
                Sheen = material.Sheen,
                SheenTint = material.SheenTint,
                SheenAperture = material.SheenAperture,

                // Legacy
                Gloss = material.Gloss,
                SpecularA = material.SpecularA,
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
                Mode = material.Mode,
                DiffuseR = material.DiffuseR,
                DiffuseG = material.DiffuseG,
                DiffuseB = material.DiffuseB,
                SpecularR = material.SpecularR,
                SpecularG = material.SpecularG,
                SpecularB = material.SpecularB,
                EmissiveR = material.EmissiveR,
                EmissiveG = material.EmissiveG,
                EmissiveB = material.EmissiveB,

                // Dawntrail
                Metalness = material.Metalness,
                Roughness = material.Roughness,
                Sheen = material.Sheen,
                SheenTint = material.SheenTint,
                SheenAperture = material.SheenAperture,

                // Legacy
                Gloss = material.Gloss,
                SpecularA = material.SpecularA,
            });
        }

        return list;
    }

    public override void ApplyToCharacter(MaterialValueIndex slot, ref bool requestRedraw) { }
}
