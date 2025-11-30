using Penumbra.Api.Enums;
using Penumbra.Api.Helpers;
using API = Penumbra.Api.IpcSubscribers;


namespace SimpleGlamourSwitcher.IPC;

public static class PenumbraIpc {


    public static readonly EventSubscriber<string> ModAdded = API.ModAdded.Subscriber(PluginInterface, _ => InvalidateCache());
    public static readonly EventSubscriber<string> ModDeleted = API.ModDeleted.Subscriber(PluginInterface, _ => InvalidateCache());
    public static readonly EventSubscriber<string> ModMoved = API.ModAdded.Subscriber(PluginInterface, _ => InvalidateCache());
    public static readonly EventSubscriber<ModSettingChange, Guid, string, bool> ModSettingChanged = API.ModSettingChanged.Subscriber(PluginInterface, (_, _, _, _) => InvalidateCache());
    
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
    