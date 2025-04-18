global using CustomizePlusProfileDataTuple = (
    System.Guid UniqueId,
    string Name,
    string VirtualPath,
    System.Collections.Generic.List<(string Name, ushort WorldId, byte CharacterType, ushort CharacterSubType)>
    Characters,
    int Priority,
    bool IsEnabled
);
using System.Diagnostics.CodeAnalysis;
using ECommons.EzIpcManager;
using OtterGui;

namespace SimpleGlamourSwitcher.IPC;

// ReSharper disable UnassignedReadonlyField
public static class CustomizePlus {
    public static readonly CustomizePlusProfileDataTuple EmptyProfile = (Guid.Empty, string.Empty, string.Empty, [], 0, false);

    // https://github.com/Aether-Tools/CustomizePlus/blob/main/CustomizePlus/Api/Enums/ErrorCode.cs
    public enum ErrorCode {
        Success = 0,

        /// <summary>
        /// Returned when invalid character address was provided
        /// </summary>
        InvalidCharacter = 1,

        /// <summary>
        /// Returned if IPCCharacterProfile could not be deserialized or deserialized into an empty object
        /// </summary>
        CorruptedProfile = 2,

        /// <summary>
        /// Provided character does not have active profiles, provided profile id is invalid or provided profile id is not valid for use in current function
        /// </summary>
        ProfileNotFound = 3,

        /// <summary>
        /// General error telling that one of the provided arguments were invalid.
        /// </summary>
        InvalidArgument = 4,

        UnknownError = 255
    }

    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    private static class Api {
        static Api() {
            EzIPC.Init(typeof(Api), "CustomizePlus");
        }

        [EzIPC("General.GetApiVersion")] public static readonly Func<(int Breaking, int Feature)> GetApiVersion = null!;
        [EzIPC("Profile.GetList")] public static readonly Func<IList<CustomizePlusProfileDataTuple>> GetProfileList = null!;
        [EzIPC("Profile.GetByUniqueId")] public static readonly Func<Guid, (int errorCode, string? profileData)> GetProfileByUniqueId = null!;
        [EzIPC("Profile.DisableByUniqueId")] public static readonly Func<Guid, int> DisableByUniqueId = null!;
        [EzIPC("Profile.EnableByUniqueId")] public static readonly Func<Guid, int> EnableByUniqueId = null!;

        [EzIPC("Profile.GetActiveProfileIdOnCharacter")]
        public static readonly Func<ushort, (int errorCode, Guid? activeProfile)> GetActiveProfileIdOnCharacter = null!;

        [EzIPC("Profile.AddPlayerCharacter")] public static readonly Func<Guid, string, ushort, int> AddPlayerCharacter = null!;

        [EzIPC("Profile.RemovePlayerCharacter")]
        public static readonly Func<Guid, string, ushort, int> RemovePlayerCharacter = null!;
    }

    public static bool IsReady() {
        try {
            return Api.GetApiVersion() is { Breaking: 6, Feature: >= 1 };
        } catch {
            return false;
        }
    }

    public static IList<CustomizePlusProfileDataTuple> GetProfileList() => Api.GetProfileList();

    public static bool TryGetProfileByUniqueId(Guid guid, [NotNullWhen(true)] out string? profileData) {
        var getProfile = Api.GetProfileByUniqueId(guid);
        var error = (ErrorCode)getProfile.errorCode;
        if (error == ErrorCode.Success && getProfile.profileData != null) {
            profileData = getProfile.profileData;
            return true;
        }

        profileData = getProfile.profileData;
        return false;
    }

    public static bool TryGetProfileDataByUniqueId(Guid guid, out CustomizePlusProfileDataTuple profileData) {
        var list = Api.GetProfileList();
        return list.FindFirst(p => p.UniqueId == guid, out profileData);
    }

    public static ErrorCode DisableByUniqueId(Guid guid) => (ErrorCode)Api.DisableByUniqueId(guid);
    public static ErrorCode EnableByUniqueId(Guid guid) => (ErrorCode)Api.EnableByUniqueId(guid);

    public static bool TryGetActiveProfileOnCharacter(ushort index, out CustomizePlusProfileDataTuple profileData) {
        if (!IsReady()) {
            profileData = EmptyProfile;
            return false;
        }

        var getActiveProfile = Api.GetActiveProfileIdOnCharacter(index);
        var errorCode = (ErrorCode)getActiveProfile.errorCode;
        if (errorCode != ErrorCode.Success) {
            profileData = EmptyProfile;
            return false;
        }

        profileData = GetProfileList().FirstOrDefault(p => p.UniqueId == getActiveProfile.activeProfile);
        return profileData.UniqueId == getActiveProfile.activeProfile;
    }

    public static bool TryAddPlayerCharacterToProfile(Guid profile, string characterName, uint worldId) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(worldId, ushort.MaxValue);
        var errorCode = (ErrorCode)Api.AddPlayerCharacter(profile, characterName, (ushort)worldId);
        return errorCode == ErrorCode.Success;
    }

    public static bool TryRemovePlayerCharacterFromProfile(Guid profile, string characterName, uint worldId) {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(worldId, ushort.MaxValue);
        var errorCode = (ErrorCode)Api.RemovePlayerCharacter(profile, characterName, (ushort)worldId);
        return errorCode == ErrorCode.Success;
    }
}
