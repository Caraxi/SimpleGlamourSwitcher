using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Configuration.Enum;

public enum SlotType {
    HeadEquip,
    BodyEquip,
    HandsEquip,
    LegsEquip,
    FeetEquip,
    EarsEquip,
    NeckEquip,
    WristEquip,
    RightRingEquip,
    LeftRingEquip,
    MainHandEquip,
    OffHandEquip,
    HairStyle,
    FaceWear,
}

public static class SlotTypeExtensions {
    public static EquipSlot? ToEquipSlot(this SlotType slotType) {
        return slotType switch {
            SlotType.HeadEquip => EquipSlot.Head,
            SlotType.BodyEquip => EquipSlot.Body,
            SlotType.HandsEquip => EquipSlot.Hands,
            SlotType.LegsEquip => EquipSlot.Legs,
            SlotType.FeetEquip => EquipSlot.Feet,
            SlotType.EarsEquip => EquipSlot.Ears,
            SlotType.NeckEquip => EquipSlot.Neck,
            SlotType.WristEquip => EquipSlot.Wrists,
            SlotType.RightRingEquip => EquipSlot.RFinger,
            SlotType.LeftRingEquip => EquipSlot.LFinger,
            SlotType.MainHandEquip => EquipSlot.MainHand,
            SlotType.OffHandEquip => EquipSlot.OffHand,
            _ => null
        };
    }
}