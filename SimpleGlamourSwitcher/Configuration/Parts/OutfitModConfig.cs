using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using CustomizeIndex = Penumbra.GameData.Enums.CustomizeIndex;
using Race = Penumbra.GameData.Enums.Race;

namespace SimpleGlamourSwitcher.Configuration.Parts;

public record OutfitModConfig(string ModDirectory, bool Enabled, int Priority, Dictionary<string, List<string>> Settings) {
    public static implicit operator OutfitModConfig((string ModDirectory, bool Enabled, int Priority, Dictionary<string, List<string>> Settings) a) {
        return new OutfitModConfig(a.ModDirectory, a.Enabled, a.Priority, a.Settings);
    }

    public static implicit operator (string ModDirectory, bool Enabled, int Priority, Dictionary<string, List<string>> Settings)(OutfitModConfig a) {
        return (a.ModDirectory, a.Enabled, a.Priority, a.Settings);
    }

    public static List<OutfitModConfig> GetModListFromEquipment(HumanSlot slot, EquipItem equipItem, Guid penumbraCollection) {
        
        var list = new List<OutfitModConfig>();

        var mods = PenumbraIpc.CheckCurrentChangedItem(equipItem.Name);
        
        foreach (var mod in mods) {
            var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(penumbraCollection, mod.ModDirectory);
            if (getModSettings.Item1 != PenumbraApiEc.Success || getModSettings.Item2 == null) continue;
            var modSettings = getModSettings.Item2.Value;
            list.Add(new OutfitModConfig(mod.ModDirectory, modSettings.Item1, modSettings.Item2, modSettings.Item3));
        }
        
        return list;
    }
    
    public static List<OutfitModConfig> GetModListFromCustomize(CustomizeIndex slot, GlamourerCustomize customize, Guid penumbraCollection) {
        var list = new List<OutfitModConfig>();


        List<(string ModDirectory, string ModName)> mods = [];
        switch (slot) {
            case CustomizeIndex.Hairstyle:
                var modelRaceName = customize.Race.Value == 1 ? customize.Clan.Value == 2 ? ModelRace.Highlander.ToName() : ModelRace.Midlander.ToName() : ((Race)customize.Race.Value).ToName();

                var gender = customize.Gender.Value switch {
                    0 => Gender.Male.ToName(),
                    1 => Gender.Female.ToName(),
                    _ => Gender.Unknown.ToName(),
                };
                
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"Customization: {modelRaceName} {gender} Hair {customize.Hairstyle.Value}"));
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"Customization: {gender} {modelRaceName} Hair {customize.Hairstyle.Value}"));
                break;
        }
        
        
        foreach (var mod in mods) {
            if (list.Any(x => x.ModDirectory == mod.ModDirectory)) continue;
            var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(penumbraCollection, mod.ModDirectory);
            if (getModSettings.Item1 != PenumbraApiEc.Success || getModSettings.Item2 == null) continue;
            var modSettings = getModSettings.Item2.Value;
            list.Add(new OutfitModConfig(mod.ModDirectory, modSettings.Item1, modSettings.Item2, modSettings.Item3));
        }
        
        
        
        

        return list;
    }
    
    public static List<OutfitModConfig> GetModListFromMinion(uint minionId, Guid penumbraCollection) {
        
        var list = new List<OutfitModConfig>();
        if (minionId == 0) return list;
        var name = SeStringEvaluator.EvaluateObjStr(ObjectKind.Companion, minionId);
        if (string.IsNullOrWhiteSpace(name)) return list;

        var mods = PenumbraIpc.CheckCurrentChangedItem($"{name} (Companion)");
        
        foreach (var mod in mods) {
            var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(penumbraCollection, mod.ModDirectory);
            if (getModSettings.Item1 != PenumbraApiEc.Success || getModSettings.Item2 == null) continue;
            var modSettings = getModSettings.Item2.Value;
            list.Add(new OutfitModConfig(mod.ModDirectory, modSettings.Item1, modSettings.Item2, modSettings.Item3));
        }
        
        return list;
    }
    

    [JsonIgnore]
    public IReadOnlyDictionary<string, IReadOnlyList<string>> ReadOnlySettings {
        get {
            var d = new Dictionary<string, IReadOnlyList<string>>();
            foreach (var (k, v) in Settings) d.Add(k, v);
            return d;
        }
    }


}