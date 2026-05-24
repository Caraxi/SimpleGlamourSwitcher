using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using API = Penumbra.Api.IpcSubscribers;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using ECommons;
using Lumina.Excel.Sheets;


namespace SimpleGlamourSwitcher.IPC;

public static class PenumbraIpc {
    public static readonly EventSubscriber<string> ModAdded = API.ModAdded.Subscriber(PluginInterface, _ => InvalidateCache());
    public static readonly EventSubscriber<string> ModDeleted = API.ModDeleted.Subscriber(PluginInterface, _ => InvalidateCache());
    public static readonly EventSubscriber<string, string> ModMoved = API.ModMoved.Subscriber(PluginInterface, (_, _) => InvalidateCache(), QueueModMovedUpdate);
    public static readonly EventSubscriber<string, string, Dictionary<Assembly, (bool, string)>> ModUsageQueried = API.ModUsageQueried.Subscriber(PluginInterface, OnModUsageQueried);


    private static readonly Dictionary<string, string> ModMovedParseList = new();
    private static CancellationTokenSource? _modMovedCancellationTokenSource = new();

    public static void QueueModMovedUpdate(string oldDir, string newDir) {
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
            var characters = await CharacterConfigFile.GetCharacterConfigurations(CharacterConfigFile.Filters.ShowHiddenCharacter, token);
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

    /// <inheritdoc cref="Penumbra.Api.IpcSubscribers.ModSettingChanged"/>
    public static readonly EventSubscriber<ModSettingChange, Guid, string, bool> ModSettingChanged = API.ModSettingChanged.Subscriber(PluginInterface, OnModSettingChanged);

    private static void OnModSettingChanged(ModSettingChange type, Guid collectionId, string modDir, bool inherited) {
        if (string.IsNullOrEmpty(modDir)) return;
        InvalidateCache();
    }

    public static void Dispose() {
        ModAdded.Dispose();
        ModDeleted.Dispose();
        ModMoved.Dispose();
        ModUsageQueried.Dispose();
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
        ModUsageQueried.Enable();
    }

    private class CustomAssembly(string customName) : Assembly {
        public static CustomAssembly Instance { get; } = new("S.G.S.");
        private AssemblyName CustomName { get; } = new(GetExecutingAssembly().FullName ?? customName) { Name = customName };
        public override AssemblyName GetName() => CustomName;
    }

    private static void OnModUsageQueried(string modName, string modDirectory, Dictionary<Assembly, (bool, string)> arg3) {
        try {
            if (DateTime.Now - _modUsageUpdateTime > TimeSpan.FromSeconds(30)) {
                UpdateModUsageCache().Wait();
            }
        } catch (Exception e) {
            PluginLog.Error(e, "Error parsing Mod Usage");
        }

        if (!ModUsageCache.TryGetValue(modDirectory, out var usageList)) return;

        foreach (var (_, usage) in usageList) {
            if (usage.UsageNote.Count == 1) {
                arg3.TryAdd(CustomAssembly.Instance, (false, $"Used by {usage.CharacterName}/{usage.UsageNote.First()}"));
            } else {
                var str = new StringBuilder();
                str.AppendLine($"Used by {usage.CharacterName}");
                foreach (var u in usage.UsageNote) {
                    str.AppendLine($" - {u}");
                }

                arg3.TryAdd(CustomAssembly.Instance, (false, str.ToString()));
            }
        }
    }

    private static DateTime _modUsageUpdateTime = DateTime.MinValue;
    private static readonly Dictionary<string, Dictionary<Guid, (string CharacterName, HashSet<string> UsageNote)>> ModUsageCache = [];

    private static async Task UpdateModUsageCache() {
        ModUsageCache.Clear();
        _modUsageUpdateTime = DateTime.Now;
        var time = Stopwatch.StartNew();
        foreach (var (characterGuid, characterFile) in await CharacterConfigFile.GetCharacterConfigurations(CharacterConfigFile.Filters.ShowHiddenCharacter)) {
            PluginLog.Verbose($"Loading Mod Usage for {characterFile.Name} [{characterGuid}]");

            foreach (var (entryGuid, entry) in await characterFile.GetEntries()) {
                PluginLog.Verbose($" - Loading Mod Usage for {characterFile.Name} - {entry.Name} [{entryGuid}]");

                switch (entry) {
                    case IHasModConfigs m:
                        Parse(m);
                        break;
                    case OutfitConfigFile outfitConfigFile: {
                        foreach (var (n, applicable) in outfitConfigFile.Appearance) {
                            if (applicable is IHasModConfigs mApplicable) {
                                Parse(mApplicable, n);
                            }
                        }

                        foreach (var (n, applicable) in outfitConfigFile.Equipment) {
                            if (applicable is IHasModConfigs m) {
                                Parse(m, n);
                            }
                        }

                        foreach (var (classJobId, weaponSet) in outfitConfigFile.Weapons.ClassWeapons) {
                            var classJob = DataManager.GetExcelSheet<ClassJob>().GetRowOrDefault(classJobId)?.Name.ExtractText() ?? $"ClassJob#{classJobId}";
                            Parse(weaponSet.MainHand, $"{classJob} MainHand");
                            Parse(weaponSet.OffHand, $"{classJob} OffHand");
                        }
                        break;
                    }
                    default:
                        PluginLog.Error($"Unhandled Entry Type {entry.GetType().FullName}");
                        break;
                }
                continue;

                void Parse(IHasModConfigs modContainer, string? slotName = null) {
                    foreach (var mod in modContainer.ModConfigs) {
                        var modCache = ModUsageCache.GetOrCreate(mod.ModDirectory, []);
                        var charCache = modCache.GetOrCreate(characterGuid, (characterFile.Name, []));

                        if (string.IsNullOrWhiteSpace(slotName)) {
                            charCache.UsageNote.Add($"{characterFile.ParseFolderPath(entry.Folder, false, true, true)}{entry.Name}");
                        } else {
                            charCache.UsageNote.Add($"{characterFile.ParseFolderPath(entry.Folder, false, true, true)}{entry.Name} [{slotName}]");
                        }
                    }
                }
            }
        }

        _modUsageUpdateTime = DateTime.Now;
        PluginLog.Info($"Updated Mod Usage Cache in {time.Elapsed.TotalMilliseconds} milliseconds.");
    }

    public static readonly API.GetCollections GetCollections = new(PluginInterface);
    public static readonly API.GetCollection GetCollection = new(PluginInterface);
    public static readonly API.GetCollectionForObject GetCollectionForObject = new(PluginInterface);
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
