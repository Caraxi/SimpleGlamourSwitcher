using ECommons.EzIpcManager;

namespace SimpleGlamourSwitcher.IPC;

// ReSharper disable UnassignedReadonlyField
public static class HonorificIpc {
    static HonorificIpc() {
        EzIPC.Init(typeof(HonorificIpc), "Honorific");
    }
    
    [EzIPC] public static readonly Action<string, uint>? SetLocalPlayerIdentity = null!;
    [EzIPC] private static readonly Func<(uint Major, uint Minor)>? ApiVersion = null!;
    
    public static bool IsReady() {
        try {
            if (ApiVersion == null) return false;
            var v = ApiVersion();
            if (v.Major != 3) return false;
            if (v.Minor < 2) return false;
            return true;
        } catch {
            return false;
        }
    }
}
