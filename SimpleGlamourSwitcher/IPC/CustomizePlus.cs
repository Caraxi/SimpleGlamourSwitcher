global using CustomizePlusProfileDataTuple = (
    System.Guid UniqueId,
    string Name,
    string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)> Characters,
    int Priority,
    bool IsEnabled
);


using ECommons.EzIpcManager;


namespace SimpleGlamourSwitcher.IPC;



// ReSharper disable UnassignedReadonlyField
public static class CustomizePlus {
    
    static CustomizePlus() {
        EzIPC.Init(typeof(CustomizePlus), "CustomizePlus");
    }
    
    [EzIPC("Profile.GetList")] public static readonly Func<IList<CustomizePlusProfileDataTuple>>? GetProfileList;
    [EzIPC("Profile.GetByUniqueId")] public static readonly Func<Guid, (int errorCode, string? profileData)>? GetProfileByUniqueId;
    [EzIPC("Profile.SetTemporaryProfileOnCharacter")] public static readonly Func<ushort, string, (int errorCode, Guid? guid)>? SetTemporaryProfileOnCharacter;
    [EzIPC("Profile.DeleteTemporaryProfileOnCharacter")] public static readonly Func<ushort, int>? DeleteTemporaryProfileOnCharacter;
}
