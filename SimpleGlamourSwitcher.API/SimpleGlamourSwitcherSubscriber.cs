using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace SimpleGlamourSwitcher.API;

public class SimpleGlamourSwitcherSubscriber(IDalamudPluginInterface pi): ISimpleGlamourSwitcherApi, IDisposable {
    private ConcurrentDictionary<string, ICallGateSubscriber> subscribers = new();
    private ICallGateSubscriber<T> GetSubscriber<T>(string name) {
        if (subscribers.TryGetValue(name, out var s) && s is ICallGateSubscriber<T> subscriber) {
            return subscriber;
        }

        subscriber = pi.GetIpcSubscriber<T>($"SimpleGlamourSwitcher.{name}");
        subscribers[name] = subscriber;
        return subscriber;
    }
    
    private ICallGateSubscriber<T1, TRet> GetSubscriber<T1, TRet>(string name) {
        if (subscribers.TryGetValue(name, out var s) && s is ICallGateSubscriber<T1, TRet> subscriber) {
            return subscriber;
        }

        subscriber = pi.GetIpcSubscriber<T1, TRet>($"SimpleGlamourSwitcher.{name}");
        subscribers[name] = subscriber;
        return subscriber;
    }

    private TRet? Func<TRet>([CallerMemberName] string name = "") {
        try {
            return GetSubscriber<TRet>(name).InvokeFunc();
        } catch {
            return default;
        }
    }
    
    private TRet? Func<T1, TRet>(T1 v1, [CallerMemberName] string name = "") {
        try {
            return GetSubscriber<T1, TRet>(name).InvokeFunc(v1);
        } catch {
            return default;
        }
    }
    
    private void Action<T1>(T1 v1, [CallerMemberName] string name = "") {
        try {
            GetSubscriber<T1, object>(name).InvokeAction(v1);
        } catch {
            //
        }
    }

    
    
    public (int, int)? ApiVersion() => Func<(int, int)>();
    
    public List<IpcEntry>? GetAvailableEntryList() => Func<List<IpcEntry>>();
    public List<IpcEntry>? GetAvailableOutfitList() =>Func<List<IpcEntry>>();
    public List<IpcEntry>? GetAvailableEmoteList() => Func<List<IpcEntry>>();
    public List<IpcEntry>? GetAvailableMinionList() => Func<List<IpcEntry>>();
    public List<IpcEntry>? GetAvailableItemList() => Func<List<IpcEntry>>();
    public List<IpcEntry>? GetAvailableGenericEntryList() => Func<List<IpcEntry>>();
    public List<IpcEntry>? GetCharacterEntryList(Guid characterGuid) => Func<Guid, List<IpcEntry>>(characterGuid);
    public List<IpcEntry>? GetCharacterOutfitList(Guid characterGuid) => Func<Guid, List<IpcEntry>>(characterGuid);
    public List<IpcEntry>? GetCharacterEmoteList(Guid characterGuid) => Func<Guid, List<IpcEntry>>(characterGuid);
    public List<IpcEntry>? GetCharacterMinionList(Guid characterGuid) =>Func<Guid, List<IpcEntry>>(characterGuid);
    public List<IpcEntry>? GetCharacterItemList(Guid characterGuid) => Func<Guid, List<IpcEntry>>(characterGuid);
    public List<IpcEntry>? GetCharacterGenericEntryList(Guid characterGuid) => Func<Guid, List<IpcEntry>>(characterGuid);
    
    public List<IpcCharacter>? GetCharacterList() => Func<List<IpcCharacter>>();
    public string? SwitchCharacter(Guid characterGuid) => Func<Guid, string?>(characterGuid);
    public string? Apply(Guid entryGuid) => Func<Guid, string?>(entryGuid);

    public Guid? GetActiveCharacterGuid() => Func<Guid>();
    public Guid? GetSharedCharacterGuid() => Func<Guid>();

    public void Dispose() {
        
    }
}
