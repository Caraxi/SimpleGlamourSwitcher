using Glamourer.Api.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC.Glamourer;

using API = Glamourer.Api.IpcSubscribers;

namespace SimpleGlamourSwitcher.IPC;

public static class GlamourerIpc {
    public static readonly API.GetDesignList GetDesignList = new(PluginInterface); 
    public static readonly API.GetStateBase64 GetStateBase64 = new(PluginInterface); 
    public static readonly API.ApplyState ApplyState = new(PluginInterface);
    public static readonly API.ApplyDesign ApplyDesign = new(PluginInterface);
    private static readonly API.GetState GetStateInternal = new(PluginInterface);
    public static readonly API.SetItem SetItem = new(PluginInterface);
    public static readonly API.SetBonusItem SetBonusItem = new(PluginInterface);
    public static readonly Action<OutfitAppearance> ApplyCustomization = ApplyCustomizationInvoke;

    
    private static void ApplyCustomizationInvoke(OutfitAppearance appearance) {
        var state = GetState(0);
        if (state == null) return;
        
        
        var obj = new JObject();
        var customize = new JObject();

        if (appearance.Clan.Apply) {
            
            customize.Add("Race", new JObject() {
                { "Apply", true },
                { "Value", appearance.Clan.Value switch {
                    1 or 2 => 1,
                    3 or 4 => 2,
                    5 or 6 => 3,
                    7 or 8 => 4,
                    9 or 10 => 5,
                    11 or 12 => 6,
                    13 or 14 => 7,
                    15 or 16 => 8,
                    _ => 1,
                } }
            });
            customize.Add("Clan", new JObject() {
                { "Apply", true },
                { "Value", appearance.Clan.Value }
            });
        } else {
            customize.Add("Race", new JObject() {
                { "Apply", true },
                { "Value", state.Customize.Clan.Value switch {
                    1 or 2 => 1,
                    3 or 4 => 2,
                    5 or 6 => 3,
                    7 or 8 => 4,
                    9 or 10 => 5,
                    11 or 12 => 6,
                    13 or 14 => 7,
                    15 or 16 => 8,
                    _ => 1,
                } }
            });
            customize.Add("Clan", new JObject() {
                { "Apply", true },
                { "Value", state.Customize.Clan.Value }
            });
        }
        
        
        foreach (var v in Enum.GetValues<CustomizeIndex>()) {
            if (v is CustomizeIndex.Clan or CustomizeIndex.Race) continue;
            var c = appearance[v];
            var jValue = new JObject {
                { "Apply", c.Apply },
                { "Value", c.Value }
            };
            
            customize.Add($"{v}", jValue);
        }
        
        obj.Add("Customize", customize);
        obj.Add("Equipment", new JObject());
        obj.Add("FileVersion", 1);

        ApplyState.Invoke(obj, 0, 0, ApplyFlag.Customization);
    }
    
    public static GlamourerState? GetState(int objectIndex) {
        var state = GetStateInternal.Invoke(objectIndex);
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
