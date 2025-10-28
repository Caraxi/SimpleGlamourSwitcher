using Glamourer.Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Utility;
using API = Glamourer.Api.IpcSubscribers;

namespace SimpleGlamourSwitcher.IPC;

public static class GlamourerIpc {
    public static readonly API.GetDesignList GetDesignList = new(PluginInterface);
    public static readonly API.GetStateBase64 GetStateBase64 = new(PluginInterface);
    public static readonly API.ApplyState ApplyState = new(PluginInterface);
    public static readonly API.ApplyDesign ApplyDesign = new(PluginInterface);
    public static readonly API.GetState GetStateJson = new(PluginInterface);
    public static readonly API.SetItem SetItem = new(PluginInterface);
    public static readonly API.SetBonusItem SetBonusItem = new(PluginInterface);
    public static readonly API.SetMetaState SetMetaState = new(PluginInterface);
    public static readonly API.RevertState RevertState = new(PluginInterface);

    public static JObject? GetCustomizationJObject(OutfitAppearance appearance, OutfitEquipment outfitEquipment) {
        
        var state = GetState(0);
        if (state == null) return null;

        var stateMaterials = state.Materials ?? new Dictionary<MaterialValueIndex, GlamourerMaterial>();
        
        // var appearance = config.Appearance;

        var obj = new JObject();
        var customize = new JObject();
        var parameters = new JObject();
        var equipment = new JObject();
        var materials = new JObject();
        
        if (appearance.Apply) {
            if (appearance.Clan.Apply) {
                customize.Add("Race", new JObject() {
                    { "Apply", true }, {
                        "Value", appearance.Clan.Value switch {
                            1 or 2 => 1,
                            3 or 4 => 2,
                            5 or 6 => 3,
                            7 or 8 => 4,
                            9 or 10 => 5,
                            11 or 12 => 6,
                            13 or 14 => 7,
                            15 or 16 => 8,
                            _ => 1,
                        }
                    }
                });
                customize.Add("Clan", new JObject() { { "Apply", true }, { "Value", appearance.Clan.Value } });
            }

            foreach (var v in Enum.GetValues<CustomizeIndex>()) {
                if (v is CustomizeIndex.Clan or CustomizeIndex.Race) continue;
                var c = appearance[v];
                var jValue = new JObject { { "Apply", c.Apply }, { "Value", c.Value } };

                customize.Add($"{v}", jValue);
            }

            foreach (var v in Enum.GetValues<AppearanceParameterKind>()) {
                var p = appearance[v];
                if (p.Apply) parameters.Add($"{v}", p.ToJObject());
            }
        }

        
        // Required Customize Values
        if (!customize.ContainsKey("Race") || !customize.ContainsKey("Clan")) {
            customize["Race"] = new JObject {
                { "Apply", true }, {
                    "Value", state.Customize.Clan.Value switch {
                        1 or 2 => 1,
                        3 or 4 => 2,
                        5 or 6 => 3,
                        7 or 8 => 4,
                        9 or 10 => 5,
                        11 or 12 => 6,
                        13 or 14 => 7,
                        15 or 16 => 8,
                        _ => 1,
                    }
                }
            };
            
            customize["Clan"] =  new JObject { { "Apply", true }, { "Value", state.Customize.Clan.Value } };
            
        }

        if (outfitEquipment.Apply) {
            if (outfitEquipment.HatVisible.Apply) {
                equipment["Hat"] = new JObject { { "Apply", true }, { "Show", outfitEquipment.HatVisible.Toggle } };
            }

            if (outfitEquipment.VisorToggle.Apply) {
                equipment["Visor"] = new JObject { { "Apply", true }, { "IsToggled", outfitEquipment.VisorToggle.Toggle } };
            }
            
            if (outfitEquipment.WeaponVisible.Apply) {
                equipment["Weapon"] = new JObject { { "Apply", true }, { "Show", outfitEquipment.WeaponVisible.Toggle } };
            }
            
            if (outfitEquipment.VieraEarsVisible.Apply) {
                equipment["VieraEars"] = new JObject { { "Apply", true }, { "Show", outfitEquipment.VieraEarsVisible.Toggle } };
            }
            
            var revertMaterial = new JObject {
                { "Revert", true },
                { "Enabled", true }
            };
            
            foreach (var slot in Common.GetGearSlots()) {
                if (outfitEquipment[slot].Apply) {
                    foreach (var (mIndex, mValue) in stateMaterials.Where(k => k.Key.ToHumanSlot() == slot)) {
                        materials[mIndex.Key.ToString("X16")] = revertMaterial;
                    }

                    foreach (var material in outfitEquipment[slot].Materials) {
                        materials[material.Index] = new JObject {
                            ["DiffuseR"] = material.DiffuseR,
                            ["DiffuseG"] = material.DiffuseG,
                            ["DiffuseB"] = material.DiffuseB,
                            ["SpecularR"] = material.SpecularR,
                            ["SpecularG"] = material.SpecularG,
                            ["SpecularB"] = material.SpecularB,
                            ["SpecularA"] = material.SpecularA,
                            ["EmissiveR"] = material.EmissiveR,
                            ["EmissiveG"] = material.EmissiveG,
                            ["EmissiveB"] = material.EmissiveB,
                            ["Gloss"] = material.Gloss,
                            ["Enabled"] = material.Apply,
                            ["Revert"] = false
                        };
                    }
                }
            }
        }
        
        obj.Add("Customize", customize);
        obj.Add("Equipment", equipment);
        obj.Add("Parameters", parameters);
        obj.Add("Materials", materials);
        obj.Add("FileVersion", 1);

        return obj;
    }

    public static async Task ApplyOutfit(OutfitAppearance appearance, OutfitEquipment equipment) {
        if (appearance is { RevertToGame: true, Apply: true } || equipment is { RevertToGame: true, Apply: true }) {
            ApplyFlag flags = 0;
            if (appearance is { RevertToGame: true, Apply: true }) flags |= ApplyFlag.Customization;
            if (equipment is { RevertToGame: true, Apply: true }) flags |= ApplyFlag.Equipment;
            await Framework.RunOnTick(() => {
                RevertState.Invoke(0, flags: flags);
            });
            await Framework.DelayTicks(1);
        }
        
        var obj = GetCustomizationJObject(appearance, equipment);
        if (obj == null) return;

        ApplyState.Invoke(obj, 0, 0, ApplyFlag.Customization);
    }

    public static GlamourerState? GetState(int objectIndex) {
        var state = GetStateJson.Invoke(objectIndex);
        if (state.Item1 != GlamourerApiEc.Success) return null;
        return state.Item2;
    }

    public static readonly DirectoryInfo DesignsFolder = new(Path.Join(PluginInterface.ConfigFile.Directory!.FullName, "Glamourer", "designs"));

    public static GlamourerDesignFile? GetDesign(Guid guid) {
        var designFile = Path.Join(DesignsFolder.FullName, $"{guid}.json");
        if (!File.Exists(designFile)) return null;
        var designJson = File.ReadAllText(designFile);
        var design = JsonConvert.DeserializeObject<GlamourerDesignFile>(designJson);
        return design;
    }
}
