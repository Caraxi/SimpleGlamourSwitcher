using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.IPC.Glamourer;

// ReSharper disable UnassignedField.Global
public class GlamourerEquipment {
    public GlamourerItem MainHand = new();
    public GlamourerItem OffHand = new();
    public GlamourerItem Head = new();
    public GlamourerItem Body = new();
    public GlamourerItem Hands = new();
    public GlamourerItem Legs = new();
    public GlamourerItem Feet = new();
    public GlamourerItem Ears = new();
    public GlamourerItem Neck = new();
    public GlamourerItem Wrists = new();
    public GlamourerItem RFinger = new();
    public GlamourerItem LFinger = new();

    public GlamourerVisibility Hat = new();
    public GlamourerVisorState Visor = new();
    public GlamourerVisibility Weapon = new();

    public IEnumerable<(EquipSlot slot, GlamourerItem)> Items {
        get {
            yield return (EquipSlot.Head, Head);
            yield return (EquipSlot.Body, Body);
            yield return (EquipSlot.Hands, Hands);
            yield return (EquipSlot.Legs, Legs);
            yield return (EquipSlot.Feet, Feet);
            yield return (EquipSlot.Ears, Ears);
            yield return (EquipSlot.Neck, Neck);
            yield return (EquipSlot.Wrists, Wrists);
            yield return (EquipSlot.RFinger, RFinger);
            yield return (EquipSlot.LFinger, LFinger);
            yield return (EquipSlot.MainHand, MainHand);
            yield return (EquipSlot.OffHand, OffHand);
        }
    }
}
