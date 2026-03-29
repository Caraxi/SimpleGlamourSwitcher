using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;

namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface IDefaultOutfitOptionsProvider {
    public HashSet<CustomizeIndex> DefaultEnabledCustomizeIndexes { get; }
    public HashSet<AppearanceParameterKind> DefaultEnabledParameterKinds { get; }
    public HashSet<HumanSlot> DefaultDisabledEquipmentSlots { get; }
    public HashSet<EquipSlot> DefaultDisabledWeaponSlots { get; }
    public HashSet<ToggleType> DefaultEnabledToggles { get; }
    public bool DefaultRevertEquip { get; }
    public bool DefaultRevertCustomize { get; }
    
    public List<Guid> DefaultLinkBefore { get; }
    public List<Guid> DefaultLinkAfter { get; }
}

public class DefaultOptions : IDefaultOutfitOptionsProvider {
    public HashSet<CustomizeIndex> DefaultEnabledCustomizeIndexes { get; private init; } = [];
    public HashSet<AppearanceParameterKind> DefaultEnabledParameterKinds { get; private init; } =  [];
    public HashSet<HumanSlot> DefaultDisabledEquipmentSlots { get; private init; } =  [];
    public HashSet<EquipSlot> DefaultDisabledWeaponSlots { get; private init; } =  [];
    public HashSet<ToggleType> DefaultEnabledToggles { get; private init; } =  [];
    public bool DefaultRevertEquip { get; private init; } =  false;
    public bool DefaultRevertCustomize { get; private init; } = false;
    public List<Guid> DefaultLinkBefore { get; private init; } = [];
    public List<Guid> DefaultLinkAfter { get; private init; } = [];

    public static DefaultOptions Equipment { get; } = new() { 
        DefaultEnabledToggles = [ToggleType.WeaponVisible, ToggleType.HatVisible, ToggleType.VisorToggle, ToggleType.VieraEarsVisible]
    };
    
}
