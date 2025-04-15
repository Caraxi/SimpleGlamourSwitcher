using ECommons.EzIpcManager;

namespace SimpleGlamourSwitcher.IPC;

public static class HonorificIpc {
    static HonorificIpc() {
        EzIPC.Init(typeof(HonorificIpc), "Honorific");
    }
    
    [EzIPC] public static readonly Action<string, uint>? SetLocalPlayerIdentity;
}
