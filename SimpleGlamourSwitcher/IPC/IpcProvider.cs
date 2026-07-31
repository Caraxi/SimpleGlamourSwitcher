using System.Collections.Concurrent;
using Dalamud.Utility;
using ECommons.EzIpcManager;
using SimpleGlamourSwitcher.API;
using SimpleGlamourSwitcher.Configuration;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;
namespace SimpleGlamourSwitcher.IPC;

public class IpcProvider : IDisposable, ISimpleGlamourSwitcherApi {
    private EzIPCDisposalToken[] disposalToken;
    public IpcProvider() {
        disposalToken = EzIPC.Init(this, "SimpleGlamourSwitcher");
    }
    
    private static IpcImage? GetImage(IImageProvider provider) => !provider.TryGetImageFileInfo(out var imageFile) ? null : new IpcImage(imageFile.FullName, provider.ImageDetail.UvMin,  provider.ImageDetail.UvMax);
    
    private ConcurrentDictionary<(Guid? Guid, Type? Type), RevokeableCached<List<IpcEntry>>> entryCache = new();
    
    private List<IpcEntry> GetEntries(Type? type = null, Guid? characterGuid = null) {
        if (entryCache.TryGetValue((characterGuid, type), out var cache)) {
            return cache.Value;
        }

        cache = new RevokeableCached<List<IpcEntry>>(() => {
            var l = new List<IpcEntry>();

            try {
                void ForCharacter(CharacterConfigFile chr) {
                    var entries = chr.GetEntries().GetAwaiter().GetResult();
                    foreach (var (guid, entry) in entries) {
                        if (chr.Guid == CharacterConfigFile.SharedDataGuid && (entry.Folder == Guid.Empty || chr.ParseFolderPath(entry.Folder, false, true).IsNullOrWhitespace())) continue;
                        if (type != null && entry.GetType() != type) continue;
                        l.Add(new IpcEntry(guid, entry.Name, chr.ParseFolderPath(entry.Folder, true, true), GetImage(entry), entry.TypeName));
                    }
                }

                var character = characterGuid == null ? ActiveCharacter : CharacterConfigFile.Load(characterGuid.Value);
                if (character != null) ForCharacter(character);
                if (characterGuid == null && SharedCharacter != null) ForCharacter(SharedCharacter);
            } catch (Exception ex) {
                PluginLog.Error(ex, "Error getting entries.");
            }

            return l;
        });

        entryCache.TryAdd((characterGuid, type), cache);
        return cache.Value;
    }
    
    [EzIPC]
    public (int, int)? ApiVersion() => (1, 0);

    [EzIPC] public List<IpcEntry> GetAvailableOutfitList() => GetEntries(typeof(OutfitConfigFile));
    [EzIPC] public List<IpcEntry> GetAvailableEmoteList() => GetEntries(typeof(EmoteConfigFile));
    [EzIPC] public List<IpcEntry> GetAvailableMinionList() => GetEntries(typeof(MinionConfigFile));
    [EzIPC] public List<IpcEntry> GetAvailableItemList() => GetEntries(typeof(ItemConfigFile));
    [EzIPC] public List<IpcEntry> GetAvailableGenericEntryList() => GetEntries(typeof(GenericEntryConfigFile));
    [EzIPC] public List<IpcEntry> GetAvailableEntryList() => GetEntries();
    [EzIPC] public List<IpcEntry> GetCharacterOutfitList(Guid characterGuid) => GetEntries(typeof(OutfitConfigFile), characterGuid);
    [EzIPC] public List<IpcEntry> GetCharacterEmoteList(Guid characterGuid) => GetEntries(typeof(EmoteConfigFile), characterGuid);
    [EzIPC] public List<IpcEntry> GetCharacterMinionList(Guid characterGuid) => GetEntries(typeof(MinionConfigFile), characterGuid);
    [EzIPC] public List<IpcEntry> GetCharacterItemList(Guid characterGuid) => GetEntries(typeof(ItemConfigFile), characterGuid);
    [EzIPC] public List<IpcEntry> GetCharacterGenericEntryList(Guid characterGuid) => GetEntries(typeof(GenericEntryConfigFile), characterGuid);
    [EzIPC] public List<IpcEntry> GetCharacterEntryList(Guid characterGuid) => GetEntries(null, characterGuid);
    
    [EzIPC] public string Apply(Guid entryGuid) {
        try {
            if (ActiveCharacter == null) return "NotActive";
            var entries = ActiveCharacter.GetEntries().GetAwaiter().GetResult();
            if (entries.TryGetValue(entryGuid, out var entry)) {
                entry.Apply();
                return "OK";
            }

            if (SharedCharacter == null) return "EntryNotFound";
            
            var sharedEntries = SharedCharacter.GetEntries().GetAwaiter().GetResult();
            if (sharedEntries.TryGetValue(entryGuid, out entry)) {
                entry.Apply();
                return "OK";
            }
            
            return "EntryNotFound";
            
        } catch (Exception ex) {
            PluginLog.Error(ex, "Error applying entry.");
            return "Error";
        }
    }
    
    [EzIPC] public Guid? GetActiveCharacterGuid() => ActiveCharacter?.Guid;
    [EzIPC] public Guid? GetSharedCharacterGuid() => SharedCharacter?.Guid;

    [EzIPC] public string? SwitchCharacter(Guid characterGuid) {
        try {
            if (characterGuid == CharacterConfigFile.SharedDataGuid) return "CannotSwitchToSharedCharacter";
            var character = CharacterConfigFile.Load(characterGuid);
            if (character == null) return "CharacterNotFound";
            var success = Config.SwitchCharacter(character.Guid);
            if (!success) return "UnknownError";
            GlamourSystem.ApplyCharacter().ConfigureAwait(false);
            return "OK";
        } catch (Exception ex) {
            PluginLog.Error(ex, "Error applying entry.");
            return "Error";
        }
    }

    private RevokeableCached<List<IpcCharacter>> characterListCache = new(() => {
        var l = new List<IpcCharacter>();
        var characterConfigFiles = CharacterConfigFile.GetCharacterConfigurations().GetAwaiter().GetResult();
        foreach (var (guid, character) in characterConfigFiles) {
            character.TryGetImageFileInfo(out var imageFile);
            l.Add(new IpcCharacter(guid, character.Name, GetImage(character)));
        }
        
        return l;
    });

    [EzIPC] public List<IpcCharacter> GetCharacterList() => characterListCache.Value;
    
    public void Dispose() {
        foreach (var e in disposalToken) e.Dispose();
        characterListCache.Dispose();
        foreach (var el in entryCache) {
            el.Value.Dispose();
        }
        
    }
}
