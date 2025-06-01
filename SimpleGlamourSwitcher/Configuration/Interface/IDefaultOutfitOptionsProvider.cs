using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;

namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface IDefaultOutfitOptionsProvider {
    public HashSet<CustomizeIndex> DefaultEnabledCustomizeIndexes { get; }
    public HashSet<AppearanceParameterKind> DefaultEnabledParameterKinds { get; }
    public HashSet<HumanSlot> DefaultDisabledEquipmentSlots { get; }
    public HashSet<ToggleType> DefaultEnabledToggles { get; }
    
    public List<Guid> DefaultLinkBefore { get; }
    public List<Guid> DefaultLinkAfter { get; }
    
}
