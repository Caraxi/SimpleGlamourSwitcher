using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace SimpleGlamourSwitcher.Service;

public static unsafe class AutomationManager {
    
    private static Hook<RaptureGearsetModule.Delegates.EquipGearset>? _equipGearsetHook;
    
    
    public static void Initialize() {
        // RaptureGearsetModule.Instance()->EquipGearset()
        _equipGearsetHook = GameInteropProvider.HookFromAddress<RaptureGearsetModule.Delegates.EquipGearset>(RaptureGearsetModule.Addresses.EquipGearset.Value, EquipGearsetDetour);
        _equipGearsetHook.Enable();
    }

    private static int EquipGearsetDetour(RaptureGearsetModule* thisPtr, int gearsetId, byte glamourPlateId) {
        try {
            Framework.RunOnTick(() => {
                ActiveCharacter?.ApplyAutomation(isGearsetSwitch: true).ConfigureAwait(false);
            }, delay: TimeSpan.FromSeconds(1));
        } catch(Exception ex) {
            PluginLog.Error(ex, "Error handling EquipGearset");
        }
        return _equipGearsetHook!.Original(thisPtr, gearsetId, glamourPlateId);
    }

    public static void Dispose() {
        _equipGearsetHook?.Disable();
        _equipGearsetHook?.Dispose();
    }
    
    
}
