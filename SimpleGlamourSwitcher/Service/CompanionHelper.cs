using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace SimpleGlamourSwitcher.Service;

public static class CompanionHelper {
    public static async Task<uint> GetActiveCompanionId() {
        uint activeCompanion = 0;
        await Framework.RunOnFrameworkThread(() => {
            unsafe {
                var chr = (Character*)ClientState.LocalPlayer?.Address;
                if (chr != null) {
                    activeCompanion = chr->CompanionData.CompanionObject != null
                        ? chr->CompanionData.CompanionObject->BaseId
                        : chr->CompanionData.CompanionId;
                }
            }
        });
        
        return activeCompanion;
    }
}