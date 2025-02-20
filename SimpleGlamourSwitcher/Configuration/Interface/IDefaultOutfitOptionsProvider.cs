using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface IDefaultOutfitOptionsProvider {
    public HashSet<CustomizeIndex> DefaultEnabledCustomizeIndexes { get; }
    public HashSet<HumanSlot> DefaultDisabledEquipmentSlots { get; }
}
