namespace SimpleGlamourSwitcher.Configuration.Parts;

/*
public record OutfitMods {
    public List<OutfitModConfig> Head = [];
    public List<OutfitModConfig> Body = [];
    public List<OutfitModConfig> Hands = [];
    public List<OutfitModConfig> Legs = [];
    public List<OutfitModConfig> Feet = [];
    public List<OutfitModConfig> Ears = [];
    public List<OutfitModConfig> Neck = [];
    public List<OutfitModConfig> Wrists = [];
    public List<OutfitModConfig> RFinger = [];
    public List<OutfitModConfig> LFinger = [];
    
    public ref List<OutfitModConfig> this[EquipSlot slot] {
        get {
            switch (slot) {
                case EquipSlot.Head: return ref Head;
                case EquipSlot.Body: return ref Body;
                case EquipSlot.Hands: return ref Hands;
                case EquipSlot.Legs: return ref Legs;
                case EquipSlot.Feet: return ref Feet;
                case EquipSlot.Ears: return ref Ears;
                case EquipSlot.Neck: return ref Neck;
                case EquipSlot.Wrists: return ref Wrists;
                case EquipSlot.RFinger: return ref RFinger;
                case EquipSlot.LFinger: return ref LFinger;
                case EquipSlot.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "Only equipments are supported.");
            }
        }
    }

    public static OutfitMods FromEquipment(OutfitEquipment instanceEquipment, Guid penumbraCollection) {
        var instance = new OutfitMods();
        
        foreach (var slot in Common.GetGearSlots()) {
            instance[slot] = OutfitModConfig.GetModListFromEquipment(slot, instanceEquipment[slot], penumbraCollection);
        }
        return instance;
    }

    public void Apply() {
        foreach (var slot in Common.GetGearSlots()) {
            ModManager.ApplyMods(slot, this[slot]);
        }
    }
}
*/