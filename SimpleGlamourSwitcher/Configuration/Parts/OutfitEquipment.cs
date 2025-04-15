using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Parts;

public record OutfitEquipment : Applicable {
    
    public ApplicableEquipment Head = new();
    public ApplicableEquipment Body = new();
    public ApplicableEquipment Hands = new();
    public ApplicableEquipment Legs = new();
    public ApplicableEquipment Feet = new();
    public ApplicableEquipment Ears = new();
    public ApplicableEquipment Neck = new();
    public ApplicableEquipment Wrists = new();
    public ApplicableEquipment RFinger = new();
    public ApplicableEquipment LFinger = new();
    public ApplicableBonus Face = new();

    public ApplicableToggle HatVisible = new();
    public ApplicableToggle VisorToggle = new();
    
    public ApplicableItem this[HumanSlot slot] {
        get {
            switch (slot) {
                case HumanSlot.Head: return Head;
                case HumanSlot.Body: return Body;
                case HumanSlot.Hands: return Hands;
                case HumanSlot.Legs: return Legs;
                case HumanSlot.Feet: return Feet;
                case HumanSlot.Ears: return Ears;
                case HumanSlot.Neck: return Neck;
                case HumanSlot.Wrists: return Wrists;
                case HumanSlot.RFinger: return RFinger;
                case HumanSlot.LFinger: return LFinger;
                case HumanSlot.Face: return Face;
                case HumanSlot.Unknown:
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot), slot, "Only equipments are supported.");
            }
        }
    }

    public ApplicableToggle this[ToggleType toggleType] {
        get {
            switch (toggleType) {
                case ToggleType.HatVisible: return HatVisible;
                case ToggleType.VisorToggle: return VisorToggle;
                default:
                    throw new ArgumentOutOfRangeException(nameof(toggleType), toggleType, "Unsupported toggle type.");
            }
        }
    }
    
    
    
    public override void ApplyToCharacter(ref bool requestRedraw) {
        if (!Apply) return;
        
        PluginLog.Verbose("ApplyToCharacter");
        foreach (var slot in Common.GetGearSlots()) {
            PluginLog.Verbose($"ApplyToCharacter {slot}");
            this[slot].ApplyToCharacter(slot, ref requestRedraw);
        }

        foreach (var toggle in System.Enum.GetValues<ToggleType>()) {
            PluginLog.Verbose($"ApplyToCharacter: {toggle}");
            this[toggle].ApplyToCharacter(toggle, ref requestRedraw);
        }
        
    }

    public static OutfitEquipment FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, GlamourerState glamourerState, Guid effectiveCollectionId) {
        var glamourerEquipment = glamourerState.Equipment;
        return new OutfitEquipment {
            Apply = defaultOptionsProvider.DefaultDisabledEquipmentSlots.Count < 11,
            Head = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Head, glamourerEquipment.Head, effectiveCollectionId),
            Body = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Body, glamourerEquipment.Body, effectiveCollectionId),
            Hands = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Hands, glamourerEquipment.Hands, effectiveCollectionId),
            Legs = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Legs, glamourerEquipment.Legs, effectiveCollectionId),
            Feet = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Feet, glamourerEquipment.Feet, effectiveCollectionId),
            Ears = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Ears, glamourerEquipment.Ears, effectiveCollectionId),
            Neck = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Neck, glamourerEquipment.Neck, effectiveCollectionId),
            Wrists = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Wrists, glamourerEquipment.Wrists, effectiveCollectionId),
            RFinger = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.RFinger, glamourerEquipment.RFinger, effectiveCollectionId),
            LFinger = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.LFinger, glamourerEquipment.LFinger, effectiveCollectionId),
            Face = ApplicableBonus.FromExistingState(defaultOptionsProvider, HumanSlot.Face, glamourerState.Bonus, effectiveCollectionId),
            
            HatVisible = ApplicableToggle.FromExistingState(defaultOptionsProvider, ToggleType.HatVisible, glamourerEquipment.Hat.Apply, glamourerEquipment.Hat.Show),
            VisorToggle = ApplicableToggle.FromExistingState(defaultOptionsProvider, ToggleType.VisorToggle, glamourerEquipment.Visor.Apply, glamourerEquipment.Visor.IsToggled),
        };
    }
}
