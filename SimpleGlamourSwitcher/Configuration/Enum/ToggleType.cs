using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Glamourer.Api.Enums;

namespace SimpleGlamourSwitcher.Configuration.Enum;

public enum ToggleType {
    [JsonPropertyName("Show")] HatVisible,
    [JsonPropertyName("IsToggled")] VisorToggle,
    [JsonPropertyName("Show")] WeaponVisible,
    [JsonPropertyName("Show")] VieraEarsVisible,
}

public static class ToggleTypeExtensions {
    public static MetaFlag ToMetaFlag(this ToggleType tt) {
        return tt switch {
            ToggleType.HatVisible => MetaFlag.HatState,
            ToggleType.VisorToggle => MetaFlag.VisorState,
            ToggleType.WeaponVisible => MetaFlag.WeaponState,
            ToggleType.VieraEarsVisible => MetaFlag.EarState,
            _ => 0
        };
    }
}