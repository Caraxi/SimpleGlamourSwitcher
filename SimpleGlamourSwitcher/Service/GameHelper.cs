using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.Interop;

namespace SimpleGlamourSwitcher.Service;

public static unsafe class GameHelper {
    public static (byte Id, string Name)? GetGearsetByIndex(int index) {
        if (ClientState.LocalContentId == 0) return null;
        var currentGearset = RaptureGearsetModule.Instance()->GetGearset(index);
        return (currentGearset->Id, currentGearset->NameString);
    }

    public static (byte Id, string Name)? GetGearsetById(byte id) {
        if (ClientState.LocalContentId == 0) return null;
        foreach (var e in RaptureGearsetModule.Instance()->Entries.PointerEnumerator()) {
            if (e->Id == id) return (e->Id, e->NameString);
        }
        
        return null;
    }

    public static (byte Id, string Name)? GetActiveGearset() {
        return GetGearsetByIndex(RaptureGearsetModule.Instance()->CurrentGearsetIndex);
    }

    public static string PlayerNameString => PlayerState.Instance()->CharacterNameString;
}
