using ECommons.EzIpcManager;

namespace SimpleGlamourSwitcher.IPC;

// ReSharper disable UnassignedReadonlyField
public static class HeelsIpc {
    static HeelsIpc() {
        EzIPC.Init(typeof(HeelsIpc), "SimpleHeels");
    }
    
    [EzIPC] public static readonly Action<string, uint>? SetLocalPlayerIdentity = null!;
    [EzIPC] private static readonly Func<(int Major, int Minor)>? ApiVersion = null!;
    
    public static bool IsReady() {
        try {
            if (ApiVersion == null) return false;
            var v = ApiVersion();
            if (v.Major != 2) return false;
            if (v.Minor < 3) return false;
            return true;
        } catch {
            return false;
        }
    }
}
