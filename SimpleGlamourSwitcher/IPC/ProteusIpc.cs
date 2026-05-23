global using ProteusIpcOverlayDetail = (
    string ModDirectory, 
    string Name, 
    int Priority, 
    System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>>? Options
);
using System.Diagnostics.CodeAnalysis;
using ECommons;
using ECommons.EzIpcManager;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.IPC.Proteus;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.IPC;

public static class ProteusIpc {

    public const int CacheTimeSeconds = 5;

    static ProteusIpc() {
        CleanupManager.Cleanup += CleanupCache;
    }
    
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    private static class Api {
        static Api() {
            EzIPC.Init(typeof(Api), "Proteus");
        }
        
        [EzIPC] public static readonly Func<(int Major, int Minor)> ApiVersion = null!;
        [EzIPC] public static readonly Func<List<ProteusIpcOverlayDetail>> GetActiveOverlays = null!;
        [EzIPC] public static readonly Func<List<ProteusIpcOverlayDetail>> GetOverlays = null!;
        [EzIPC] public static readonly Func<string, string?, string?, string> GetColorTable = null!;
        [EzIPC] public static readonly Func<string, string?, string?, string, bool> SetColorTable = null!;
    }
    
    public static bool IsReady() {
        try {
            var v = Api.ApiVersion();
            if (v.Major != 1) return false;
            if (v.Minor < 0) return false;
            return true;
        } catch {
            return false;
        }
    }

    public static Cached<Dictionary<string, ProteusOverlayMod>> ActiveOverlays { get; } = new(TimeSpan.FromSeconds(CacheTimeSeconds), GetActiveOverlays);
    public static Cached<Dictionary<string, ProteusOverlayMod>> AllOverlays { get; } = new(TimeSpan.FromSeconds(CacheTimeSeconds), GetOverlays);
    
    private static Dictionary<ProteusModOptionDescriptor, NullableCached<ProteusColorTable>> colourTableCache = new();
    
    private static Dictionary<string, ProteusOverlayMod> GetActiveOverlays() {
        try {
            PluginLog.Debug("Getting Active Proteus Overlays");
            return IsReady() ? Api.GetActiveOverlays().DistinctBy(o => o.ModDirectory.ToLowerInvariant()).Select(o => (ProteusOverlayMod)o).ToDictionary(o => o.ModDirectory.ToLowerInvariant(), o => o) : [];
        } catch {
            return [];
        }
    }
    
    private static Dictionary<string, ProteusOverlayMod> GetOverlays() {
        try {
            PluginLog.Debug("Getting All Proteus Overlays");
            return IsReady() ? Api.GetOverlays().DistinctBy(o => o.ModDirectory.ToLowerInvariant()).Select(o => (ProteusOverlayMod)o).ToDictionary(o => o.ModDirectory.ToLowerInvariant(), o => o) : [];
        } catch {
            return [];
        }
    }

    private static void CleanupCache() {
        PluginLog.Verbose("Cleanup Proteus Cache");
        colourTableCache.RemoveAll(kvp => !kvp.Value.HasValue && kvp.Value.Age > TimeSpan.FromMinutes(1));
    }
    

    public static ProteusColorTable? GetColourTable(ProteusModOptionDescriptor modOption) {
        if (colourTableCache.TryGetValue(modOption, out var cache)) {
            return cache.Value;
        }
        
        cache = new NullableCached<ProteusColorTable>(TimeSpan.FromSeconds(CacheTimeSeconds).Add(TimeSpan.FromSeconds(Random.Shared.NextDouble())), () => {
            try {
                PluginLog.Debug($"Getting Proteus Colour Table: [{modOption}]");

                var colorTable = Api.GetColorTable(modOption.ModDirectory, modOption.GroupName, modOption.OptionName);
                return new ProteusColorTable(colorTable);
            } catch {
                return null;
            }
            
        });
        
        colourTableCache[modOption] = cache;

        return cache.Value;
    }

    public static bool SetColorTable(ProteusModOptionDescriptor modOption, ProteusColorTable? colourTable) {
        if (!IsReady()) return false;
        var json = colourTable == null || colourTable.Rows.Count == 0 ? string.Empty : JsonConvert.SerializeObject(colourTable.Rows);
        PluginLog.Debug($"Sending Proteus.SetColorTable({modOption.ModDirectory},  {modOption.GroupName}, {modOption.OptionName}, {json})");
        return Api.SetColorTable(modOption.ModDirectory, modOption.GroupName, modOption.OptionName, json);
    }
    
    public static ProteusColorTable? GetColourTable(string modDirectory, string? optionGroup = null, string? option = null) {
        return GetColourTable(new ProteusModOptionDescriptor(modDirectory, optionGroup, option));
    }

    private static Dictionary<string, (DateTime Recheck, bool Result)> isProteusModCache = new();
    public static bool IsProteusMod(string modDirectory) {
        try {
            if (isProteusModCache.TryGetValue(modDirectory, out var v) && v.Recheck > DateTime.Now) return v.Result;
            var penumbraRootDir = PenumbraIpc.GetModDirectory.Invoke();
            var proteusMetadataFile = Path.Join(penumbraRootDir, modDirectory, "Proteus", "metadata.json");
            var exists = File.Exists(proteusMetadataFile);
            isProteusModCache[modDirectory] = (DateTime.Now + TimeSpan.FromSeconds(15) + TimeSpan.FromMinutes(Random.Shared.NextDouble()), exists);
            return exists;
        } catch {
            return false;
        }
    }
}
