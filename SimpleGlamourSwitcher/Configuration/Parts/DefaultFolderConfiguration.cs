using Newtonsoft.Json;
using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Configuration.Parts;

public class DefaultFolderConfiguration {
    public Guid Outfit = Guid.Empty;
    public Dictionary<HumanSlot, Guid> SingleItem = new();
    
    [JsonIgnore] public Guid this[HumanSlot slot] {
        get => SingleItem.GetValueOrDefault(slot, Guid.Empty);
        set {
            if (value == Guid.Empty) {
                SingleItem.Remove(slot);
            } else {
                SingleItem[slot] = value;
            }
        }
    }
}
