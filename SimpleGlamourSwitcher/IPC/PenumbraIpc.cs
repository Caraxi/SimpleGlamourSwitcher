using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using API = Penumbra.Api.IpcSubscribers;


namespace SimpleGlamourSwitcher.IPC;

public static class PenumbraIpc {
    public static readonly EventSubscriber<string> ModAdded = API.ModAdded.Subscriber(PluginInterface, _ => InvalidateCache(), (a) => Chat.Print(a));
    public static readonly EventSubscriber<string> ModDeleted = API.ModDeleted.Subscriber(PluginInterface, _ => InvalidateCache(), (a) => Chat.Print(a));
    public static readonly EventSubscriber<string, string> ModMoved = API.ModMoved.Subscriber(PluginInterface, (_, _) => InvalidateCache(), QueueModMovedUpdate);


    private readonly static Dictionary<string, string> ModMovedParseList = new();
    private static CancellationTokenSource? _modMovedCancellationTokenSource = new();
    
    public static void QueueModMovedUpdate(string oldDir, string newDir) {
        Chat.Print($"Mod Moved: {oldDir} -> {newDir}");
        _modMovedCancellationTokenSource?.Cancel();
        ModMovedParseList[oldDir] = newDir;
        _modMovedCancellationTokenSource = new CancellationTokenSource();
        Task.Delay(TimeSpan.FromSeconds(1), _modMovedCancellationTokenSource.Token).ContinueWith(ProcessModMoved);
    }
    
    private static async void ProcessModMoved(Task previous) {
        try {
            if (!previous.IsCompletedSuccessfully) return;
            var token = _modMovedCancellationTokenSource?.Token ?? CancellationToken.None;
            
            PluginLog.Info($"Processing Moved Mod(s)...");
            var characters = await CharacterConfigFile.GetCharacterConfigurations(CharacterConfigFile.Filters.ShowHiddenCharacter, cancellationToken: token);
            foreach (var chr in characters) {
                if (token.IsCancellationRequested) return;
                PluginLog.Info($"Processing Character: {chr.Value.Name}");

                var entries = await chr.Value.GetEntries(token);

                foreach (var entry in entries) {
                    if (token.IsCancellationRequested) return;
                    PluginLog.Verbose($"Processing {entry.Value.GetType().Name}: {entry.Value.Name} [{entry.Value.Guid}]");


                    bool UpdateModConfigs(string label, List<OutfitModConfig> modConfigs) {
                        var any = false;
                        for (var i = 0; i < modConfigs.Count; i++) {
                            var modConfig = modConfigs[i];
                            if (ModMovedParseList.TryGetValue(modConfig.ModDirectory, out var newDir)) {
                                modConfigs[i] = modConfig with { ModDirectory = newDir };
                                PluginLog.Info($"Updated Mod Config for '{label}' - {modConfig.ModDirectory} -> {newDir}");
                                any = true;
                            }
                        }

                        return any;
                    }

                    var edited = false;
                    switch (entry.Value) {
                        case OutfitConfigFile outfit:

                            foreach (var (name, applicable) in outfit.Appearance) {
                                if (applicable is IHasModConfigs m) {
                                    edited |= UpdateModConfigs($"{outfit.Name} [{name}]", m.ModConfigs);
                                }
                            }
                            
                            foreach (var (name, applicable) in outfit.Equipment) {
                                if (applicable is IHasModConfigs m) {
                                    edited |= UpdateModConfigs($"{outfit.Name} [{name}]", m.ModConfigs);
                                }
                            }

                            foreach (var (cj, weaponSet) in outfit.Weapons.ClassWeapons) {
                                edited |= UpdateModConfigs($"{outfit.Name} [{cj}, MainHand]", weaponSet.MainHand.ModConfigs);
                                edited |= UpdateModConfigs($"{outfit.Name} [{cj}, OffHand]", weaponSet.OffHand.ModConfigs);
                            }
                            
                            break;
                        case EmoteConfigFile emote:
                            edited |= UpdateModConfigs(emote.Name, emote.ModConfigs);
                            break;
                        case MinionConfigFile minion:
                            edited |= UpdateModConfigs(minion.Name, minion.ModConfigs);
                            break;
                        case GenericEntryConfigFile generic:
                            edited |= UpdateModConfigs(generic.Name, generic.ModConfigs);
                            break;
                        default:
                            PluginLog.Error($"Error. {entry.Value.GetType().Name} is not supported for automatic migration.");
                            break;
                    }
                    
                    if (edited) {
                        entry.Value.Save(true);
                    }
                }
            }
        } catch (Exception e) {
            PluginLog.Error(e, "Error processing penumbra mod moved.");
            // ignored
        }
    }

    public static readonly EventSubscriber<ModSettingChange, Guid, string, bool> ModSettingChanged = API.ModSettingChanged.Subscriber(PluginInterface, OnModSettingChanged);

    private static void OnModSettingChanged(ModSettingChange type, Guid collectionId, string modDir, bool inherited) {
        if (string.IsNullOrEmpty(modDir)) return;
        InvalidateCache();
    }

    public static void Dispose() {
        ModAdded.Dispose();
        ModDeleted.Dispose();
        ModMoved.Dispose();
        ModSettingChanged.Dispose();
        _checkCurrentChangedItem = null;
    }

    private static void InvalidateCache() {
        PluginLog.Verbose("Clearing Penumbra Caches");
       _checkCurrentChangedItem = null;
       Heliosphere.UpdateModList();
    }

    public static void EnableEvents() {
        ModAdded.Enable();
        ModDeleted.Enable();
        ModMoved.Enable();
    }
    
    public static readonly API.GetCollections GetCollections = new(PluginInterface);
    public static readonly API.GetCollection GetCollection = new(PluginInterface);
    public static readonly API.GetCollectionForObject  GetCollectionForObject = new(PluginInterface);
    public static readonly API.SetCollectionForObject SetCollectionForObject = new(PluginInterface);
    public static readonly API.Legacy.SetCollectionForObject LegacySetCollectionForObject = new(PluginInterface);
    public static readonly API.SetCollection SetCollection = new(PluginInterface);
    public static readonly API.GetCurrentModSettings GetCurrentModSettings = new(PluginInterface);
    public static readonly API.ResolvePlayerPaths ResolvePlayerPaths = new(PluginInterface);
    public static readonly API.GetModDirectory GetModDirectory = new(PluginInterface);
    
    private static readonly API.CheckCurrentChangedItemFunc CheckCurrentChangedItemFunc = new(PluginInterface);
    private static Func<string, (string ModDirectory, string ModName)[]>? _checkCurrentChangedItem;

    public static (string ModDirectory, string ModName)[] CheckCurrentChangedItem(string changedItem) {
        _checkCurrentChangedItem ??= CheckCurrentChangedItemFunc.Invoke();
        return _checkCurrentChangedItem(changedItem);
    }
    
    public static readonly API.GetCurrentModSettingsWithTemp GetCurrentModSettingsWithTemp = new(PluginInterface);
    
    public static readonly API.CreateTemporaryCollection CreateTemporaryCollection = new(PluginInterface);
    public static readonly API.AssignTemporaryCollection AssignTemporaryCollection = new(PluginInterface);
    
    public static readonly API.RedrawObject RedrawObject = new(PluginInterface);
    
    
    public static readonly API.GetChangedItemsForCollection GetChangedItemsForCollection = new(PluginInterface);
    public static readonly API.GetChangedItemAdapterDictionary GetChangedItemAdapterDictionary = new(PluginInterface);
    public static readonly API.GetGameObjectResourceTrees GetGameObjectResourceTrees = new(PluginInterface);
    public static readonly API.GetModList GetModList = new(PluginInterface);
    public static readonly API.GetAllModSettings GetAllModSettings = new(PluginInterface);
    
    public static readonly API.SetTemporaryModSettingsPlayer SetTemporaryModSettingsPlayer = new(PluginInterface);
    public static readonly API.SetTemporaryModSettings SetTemporaryModSettings = new(PluginInterface);
    
    public static readonly API.RemoveTemporaryModSettings RemoveTemporaryModSettings = new(PluginInterface);
    public static readonly API.RemoveTemporaryModSettingsPlayer RemoveTemporaryModSettingsPlayer = new(PluginInterface);
    
    public static readonly API.RemoveAllTemporaryModSettings RemoveAllTemporaryModSettings = new(PluginInterface);
    public static readonly API.RemoveAllTemporaryModSettingsPlayer RemoveAllTemporaryModSettingsPlayer = new(PluginInterface);
    
    public static readonly API.OpenMainWindow OpenMainWindow = new(PluginInterface);
}
    