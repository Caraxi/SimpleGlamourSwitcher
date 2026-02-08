using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace SimpleGlamourSwitcher.IPC;

public static class Heliosphere {
    private static Dictionary<string, string>? heliosphereToPenumbra;
    private readonly static Dictionary<string, string> penumbraToHeliosphere = new();

    public static IReadOnlyDictionary<string, string> HeliosphereMods {
        get {
            heliosphereToPenumbra ??= GetModList();
            return heliosphereToPenumbra;
        }
    }

    private static Dictionary<string, string> GetModList() {
        PluginLog.Debug("Searching for Heliosphere mods...");
        var rootDir = PenumbraIpc.GetModDirectory.Invoke();
        var mods = new Dictionary<string, string>();
        
        var c = 0;
        foreach (var mod in PenumbraIpc.GetModList.Invoke()) {
            var isHeliosphere = File.Exists(Path.Join(rootDir, mod.Key, "heliosphere.json"));
            if (!isHeliosphere) continue;
            var id = GetId(mod.Key);
            if (id == null) continue;
            mods[id] = mod.Key;
            c++;
        }

        return mods;
    }
    
    public static void UpdateModList() {
        heliosphereToPenumbra = GetModList();
    }
    
    public static string? GetId(string modDirectory) {
        try {
            if (penumbraToHeliosphere.TryGetValue(modDirectory, out var id)) return id;
            penumbraToHeliosphere[modDirectory] = string.Empty;
            var dir = Path.Join(PenumbraIpc.GetModDirectory.Invoke(), modDirectory);
            var heliosphereJsonFile = Path.Join(dir, "heliosphere.json");
            if (!File.Exists(heliosphereJsonFile)) return null;
            var jsonString = File.ReadAllText(heliosphereJsonFile);
            var json = JObject.Parse(jsonString);
            if (json.GetValue("Id") is not JValue { Type: JTokenType.String } v) return null;
            id = v.ToString(CultureInfo.InvariantCulture);
            penumbraToHeliosphere[modDirectory] = id;
            return id;
        } catch (Exception ex) {
            PluginLog.Error(ex, "Error parsing heliosphere id");
            return null;
        }
    }

    public static bool TryGetMod(string heliosphereId, [NotNullWhen(true)] out string? modDir) {
        return HeliosphereMods.TryGetValue(heliosphereId, out modDir) && !string.IsNullOrWhiteSpace(modDir);
    }
}
