using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public interface IHasModConfigs {
    public List<OutfitModConfig> ModConfigs { get; set; }
    
    private readonly static Cached<Dictionary<string, string>> CachedModList = new(TimeSpan.FromSeconds(5), () => PenumbraIpc.GetModList.Invoke());
    
    private static bool TryParseModName(string modDirectory, out string modName) {
        if (!CachedModList.Value.TryGetValue(modDirectory, out modName!)) {
            modName = modDirectory;
            return false;
        }
        return true;
    }

    public bool UpdateHeliosphereMods() {
        var anyChanged = false;
        for (var i = 0; i < ModConfigs.Count; i++) {
            var  m = ModConfigs[i];
            if (TryParseModName(m.ModDirectory, out _)) continue;
            if (m.HeliosphereId == null) {
                continue;
            }
            
            if (Heliosphere.TryGetMod(m.HeliosphereId, out var mod)) {
                ModConfigs[i] = m with { ModDirectory = mod };
                anyChanged = true;
            }
        }

        return anyChanged;
    }
    
    
    public bool IsValid() {
        return ModConfigs.Count == 0 || ModConfigs.All(m => TryParseModName(m.ModDirectory, out _));
    }
    
}
