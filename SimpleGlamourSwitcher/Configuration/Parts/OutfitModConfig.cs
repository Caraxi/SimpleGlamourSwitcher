using Dalamud.Game.ClientState.Objects.Enums;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;
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
        if (PluginConfig.DisableAutoModsEquip.Contains(slot)) return list;

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
        if (PluginConfig.DisableAutoModsCustomize.Contains(slot)) return list;
        
        List<(string ModDirectory, string ModName)> mods = [];
        
        var modelRaceName = customize.Race.Value == 1 ? customize.Clan.Value == 2 ? ModelRace.Highlander.ToName() : ModelRace.Midlander.ToName() : ((Race)customize.Race.Value).ToName();
        
        var gender = customize.Gender.Value switch {
            0 => Gender.Male.ToName(),
            1 => Gender.Female.ToName(),
            _ => Gender.Unknown.ToName(),
        };

        var clan = (SubRace)customize.Clan.Value;
        var clanName = clan.ToName();
        
        switch (slot) {
            case CustomizeIndex.Hairstyle:
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"Customization: {modelRaceName} {gender} Hair {customize.Hairstyle.Value}"));
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"Customization: {gender} {modelRaceName} Hair {customize.Hairstyle.Value}"));
                break;
            case CustomizeIndex.TailShape:
                if (customize.TailShape == null) break;
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"Customization: {modelRaceName} {gender} Tail {customize.TailShape.Value}"));
                break;
            case CustomizeIndex.Height:
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"{clanName} {gender} Maximum Size"));
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"{clanName} {gender} Minimum Size"));
                break;
            case CustomizeIndex.Face:
                if (customize.Face == null) break;
                var faceIndex = customize.Face?.Value;
                if (clan is SubRace.Duskwight or SubRace.Dunesfolk or SubRace.KeeperOfTheMoon or SubRace.Hellsguard or SubRace.Xaela or SubRace.Lost or SubRace.Veena) {
                    faceIndex += 100;
                }
                
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"Customization: {modelRaceName} {gender} Face {faceIndex}"));
                
                break;
            case CustomizeIndex.FacePaint:
                if (customize.FacePaint == null) break;
                mods.AddRange(PenumbraIpc.CheckCurrentChangedItem($"Customization: Face Decal {customize.FacePaint.Value}"));
                break;
            case CustomizeIndex.Clan:
            case CustomizeIndex.SkinColor:
                // Automatic detection not supported
                break;
            default:
                Chat.PrintError($"Invalid Customize Index for GetModListFromCustomize: {slot}");
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
    
    public static List<OutfitModConfig> GetModListFromEmote(EmoteIdentifier emoteIdentifier, Guid penumbraCollection) {
        var list = new List<OutfitModConfig>();
        
        if (emoteIdentifier is { EmoteModeId: 0, EmoteId: 0 }) {
            // Idle (Not Currently Supported)
        } else if (emoteIdentifier is { EmoteModeId: 1 or 2 or 3 }) {
            // Sitting or Sleeping (Not Currently Supported)
        } else {
            var emoteId = emoteIdentifier.EmoteId;
            if (emoteIdentifier.EmoteModeId > 0) {
                var emoteMode = DataManager.GetExcelSheet<EmoteMode>().GetRowOrDefault(emoteIdentifier.EmoteModeId);
                if (emoteMode != null) {
                    emoteId = emoteMode.Value.StartEmote.RowId;
                }
            }
            if (emoteId > 0) {
                var emote = DataManager.GetExcelSheet<Emote>().GetRowOrDefault(emoteId);
                if (emote != null) {
                    var emoteName = emote.Value.Name.ExtractText();
                    if (!string.IsNullOrWhiteSpace(emoteName)) {
                        var mods = PenumbraIpc.CheckCurrentChangedItem($"Emote: {emote.Value.Name.ExtractText()}");
                        foreach (var mod in mods) {
                            var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(penumbraCollection, mod.ModDirectory);
                            if (getModSettings.Item1 != PenumbraApiEc.Success || getModSettings.Item2 == null) continue;
                            var modSettings = getModSettings.Item2.Value;
                            list.Add(new OutfitModConfig(mod.ModDirectory, modSettings.Item1, modSettings.Item2, modSettings.Item3));
                        }
                    }
                }
            }
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