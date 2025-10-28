using Lumina.Excel.Sheets;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC;

namespace SimpleGlamourSwitcher.Service;

public static class ModManager {
    private const string SourceName = "Simple Glamour Switcher";
    private static readonly Dictionary<int, List<OutfitModConfig>> AppliedMods = new();
    private const int KeyBase = 0x_53_47_53_0;

    private static Dictionary<string, int> IdentifierToKey => PluginConfig.ModSlotIdentifier;
    private static Dictionary<int, string>? _keyToIdentifier;
    
    private static Dictionary<int, string> ReverseIdentifierDict() {
        var dict = new Dictionary<int, string>();
        foreach (var (identifier, key) in IdentifierToKey.ToArray()) {
            if (dict.TryAdd(key, identifier)) continue;
            IdentifierToKey.Remove(identifier);
        }

        return dict;
    }
    
    private static int GetIdentifier(string identifier) {
        _keyToIdentifier ??= ReverseIdentifierDict();
        
        if (IdentifierToKey.TryGetValue(identifier, out var key)) return -(KeyBase + key);

        var k = IdentifierToKey.Count;
        while (_keyToIdentifier.ContainsKey(k) && k < 100000) k++; // If this somehow hits 100,000 we have a problem...
        
        if (IdentifierToKey.TryAdd(identifier, k)) {
            _keyToIdentifier.Remove(k);
            _keyToIdentifier.Add(k, identifier);
            PluginConfig.Save(true);
        }

        return -(KeyBase + k);
    }



    public static int TempIdentificationKey(this HumanSlot slot) => GetIdentifier($"HumanSlot.{slot}");
    public static int TempIdentificationKey(this CustomizeIndex customizeIndex) => GetIdentifier($"CustomizeIndex.{customizeIndex}");
    private static int TempIdentificationKey(this Companion companion) => GetIdentifier($"Companion.{companion.RowId}");
    private static int TempIdentificationKey(this EmoteIdentifier emote) => GetIdentifier($"EmoteIdentifier.{emote}");

    public static void RemoveAllMods() {
        foreach (var slot in AppliedMods.Keys.ToArray()) RemoveMods(slot);
    }
    
    private static void RemoveMods(int key) {
        PluginLog.Debug($"Removing mods for Key: {key}");
        AppliedMods.Remove(key);
        PenumbraIpc.RemoveAllTemporaryModSettingsPlayer.Invoke(0, key);
    }
    
    public static void ApplyMods(HumanSlot slot, IEnumerable<OutfitModConfig> outfitModConfigs) {
        PluginLog.Debug($"Applying mods for {slot}");
        ApplyMods(slot.TempIdentificationKey(), outfitModConfigs);
    }

    public static void ApplyMods(CustomizeIndex customizeIndex, IEnumerable<OutfitModConfig> outfitModConfigs) {
        PluginLog.Debug($"Applying mods for {customizeIndex}");
        ApplyMods(customizeIndex.TempIdentificationKey(), outfitModConfigs);
    }

    public static void ApplyMods(Companion companion, IEnumerable<OutfitModConfig> outfitModConfigs) {
        PluginLog.Debug($"Applying mods for companion - {companion.Singular.ExtractText()}");
        ApplyMods(companion.TempIdentificationKey(), outfitModConfigs);
    }
    
    public static void ApplyMods(EmoteIdentifier emote, List<OutfitModConfig> outfitModConfigs) {
        PluginLog.Debug($"Applying mods for emote - {emote.Name}");
        ApplyMods(emote.TempIdentificationKey(), outfitModConfigs);
    }

    public static void ApplyMods(string identifier, List<OutfitModConfig> outfitModConfigs) {
        PluginLog.Debug($"Applying mods for {identifier}");
        ApplyMods(GetIdentifier($"Generic.{identifier}"), outfitModConfigs);
    }

    private static void ApplyMods(int key, IEnumerable<OutfitModConfig> outfitModConfigs) {
        
        RemoveMods(key);
        AppliedMods[key] = [];
        foreach (var modConfig in outfitModConfigs) {
            #if DEBUG
            _keyToIdentifier ??= ReverseIdentifierDict();
            var source = SourceName;
            var keyIndex = Math.Abs(key) - KeyBase;
            if (_keyToIdentifier.TryGetValue(keyIndex, out var identifier)) {
                source += $"{key} [{keyIndex}] {identifier}";
            } else {
                source += $"UnknownIdentifier{key}";
            }
            
            PenumbraIpc.SetTemporaryModSettingsPlayer.Invoke(0, modConfig.ModDirectory, false, modConfig.Enabled, modConfig.Priority, modConfig.ReadOnlySettings, source, key);
            #else
            PenumbraIpc.SetTemporaryModSettingsPlayer.Invoke(0, modConfig.ModDirectory, false, modConfig.Enabled, modConfig.Priority, modConfig.ReadOnlySettings, SourceName, key);
            #endif
            
            AppliedMods[key].Add(modConfig);
        }
    }
}
