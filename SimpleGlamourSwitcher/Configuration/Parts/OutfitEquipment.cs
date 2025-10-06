using System.Collections;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Parts;

[JsonObject]
public record OutfitEquipment : Applicable, IEnumerable<(string SlotName, Applicable SlotData)> {
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
    public ApplicableToggle WeaponVisible = new();
    public ApplicableToggle VieraEarsVisible = new();
    
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
                case ToggleType.WeaponVisible: return WeaponVisible;
                case ToggleType.VieraEarsVisible: return VieraEarsVisible;
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
            Head = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Head, glamourerEquipment.Head, glamourerState.Materials, effectiveCollectionId),
            Body = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Body, glamourerEquipment.Body, glamourerState.Materials,effectiveCollectionId),
            Hands = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Hands, glamourerEquipment.Hands, glamourerState.Materials,effectiveCollectionId),
            Legs = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Legs, glamourerEquipment.Legs, glamourerState.Materials,effectiveCollectionId),
            Feet = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Feet, glamourerEquipment.Feet, glamourerState.Materials,effectiveCollectionId),
            Ears = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Ears, glamourerEquipment.Ears, glamourerState.Materials,effectiveCollectionId),
            Neck = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Neck, glamourerEquipment.Neck, glamourerState.Materials,effectiveCollectionId),
            Wrists = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.Wrists, glamourerEquipment.Wrists, glamourerState.Materials,effectiveCollectionId),
            RFinger = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.RFinger, glamourerEquipment.RFinger, glamourerState.Materials,effectiveCollectionId),
            LFinger = ApplicableEquipment.FromExistingState(defaultOptionsProvider, HumanSlot.LFinger, glamourerEquipment.LFinger, glamourerState.Materials,effectiveCollectionId),
            Face = ApplicableBonus.FromExistingState(defaultOptionsProvider, HumanSlot.Face, glamourerState.Bonus, glamourerState.Materials, effectiveCollectionId),
            
            HatVisible = ApplicableToggle.FromExistingState(defaultOptionsProvider, ToggleType.HatVisible, glamourerEquipment.Hat.Apply, glamourerEquipment.Hat.Show),
            VisorToggle = ApplicableToggle.FromExistingState(defaultOptionsProvider, ToggleType.VisorToggle, glamourerEquipment.Visor.Apply, glamourerEquipment.Visor.IsToggled),
            WeaponVisible = ApplicableToggle.FromExistingState(defaultOptionsProvider, ToggleType.WeaponVisible, glamourerEquipment.Weapon.Apply, glamourerEquipment.Weapon.Show),
            VieraEarsVisible = ApplicableToggle.FromExistingState(defaultOptionsProvider, ToggleType.VieraEarsVisible, glamourerEquipment.VieraEars.Apply, glamourerEquipment.VieraEars.Show),
        };
    }

    public IEnumerator<(string, Applicable)> GetEnumerator() {
        yield return ("Head", Head);
        yield return ("Body", Body);
        yield return ("Hands", Hands);
        yield return ("Legs", Legs);
        yield return ("Feet", Feet);
        yield return ("Ears", Ears);
        yield return ("Neck", Neck);
        yield return ("Wrists", Wrists);
        yield return ("RFinger", RFinger);
        yield return ("LFinger", LFinger);
        yield return ("Glasses", Face);
        yield return ("Hat Visible Toggle", HatVisible);
        yield return ("Visor Toggle", VisorToggle);
        yield return ("Weapon Visible Toggle", WeaponVisible);
        yield return ("Ears Visible Toggle", VieraEarsVisible);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
