using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC;

namespace SimpleGlamourSwitcher.Service;

public static class ModManager {
    public const string SourceName = "Simple Glamour Switcher";

    private static readonly Dictionary<int, List<OutfitModConfig>> AppliedMods = new();

    private static int TempIdentificationKey(this HumanSlot slot) => TempIdentificationKey(0x100 + (int) slot);
    private static int TempIdentificationKey(this CustomizeIndex customizeIndex) => TempIdentificationKey(0x200 + (int) customizeIndex);
    private static int TempIdentificationKey(int @base) => (int)(@base | 0x85357000);
    
    public static void Dispose() {
       RemoveAllMods();
    }

    public static void RemoveAllMods() {
        foreach (var slot in AppliedMods.Keys.ToArray()) RemoveMods(slot);
    }
    
    public static void RemoveMods(HumanSlot slot) {
        RemoveMods(slot.TempIdentificationKey());
    }

    public static void RemoveMods(CustomizeIndex customizeIndex) {
        RemoveMods(customizeIndex.TempIdentificationKey());
    }

    private static void RemoveMods(int key) {
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

    private static void ApplyMods(int key, IEnumerable<OutfitModConfig> outfitModConfigs) {
        RemoveMods(key);
        AppliedMods[key] = [];
        foreach (var modConfig in outfitModConfigs) {
            PenumbraIpc.SetTemporaryModSettingsPlayer.Invoke(0, modConfig.ModDirectory, false, modConfig.Enabled, modConfig.Priority, modConfig.ReadOnlySettings, SourceName, key);
            AppliedMods[key].Add(modConfig);
        }
    }
}
