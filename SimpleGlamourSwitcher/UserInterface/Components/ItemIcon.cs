using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class ItemIcon {
    private readonly static Dictionary<HumanSlot, (string TexturePath, Vector2 uvMin, Vector2 uvMax)> EmptyIcons = new() {
        { HumanSlot.Head, ("ui/uld/Character_hr1.tex", new Vector2(0.2500f, 0.3273f), new Vector2(0.3750f, 0.4727f)) },
        { HumanSlot.Body, ("ui/uld/Character_hr1.tex", new Vector2(0.3750f, 0.3273f), new Vector2(0.5000f, 0.4727f)) },
        { HumanSlot.Hands, ("ui/uld/Character_hr1.tex", new Vector2(0.5000f, 0.3273f), new Vector2(0.6250f, 0.4727f)) },
        { HumanSlot.Legs, ("ui/uld/Character_hr1.tex", new Vector2(0.7500f, 0.3273f), new Vector2(0.8750f, 0.4727f)) },
        { HumanSlot.Feet, ("ui/uld/Character_hr1.tex", new Vector2(0.0000f, 0.4727f), new Vector2(0.1250f, 0.6182f)) },
        { HumanSlot.Ears, ("ui/uld/Character_hr1.tex", new Vector2(0.1250f, 0.4727f), new Vector2(0.2500f, 0.6182f)) },
        { HumanSlot.Neck, ("ui/uld/Character_hr1.tex", new Vector2(0.2500f, 0.4727f), new Vector2(0.3750f, 0.6182f)) },
        { HumanSlot.Wrists, ("ui/uld/Character_hr1.tex", new Vector2(0.3750f, 0.4727f), new Vector2(0.5000f, 0.6182f)) },
        { HumanSlot.RFinger, ("ui/uld/Character_hr1.tex", new Vector2(0.5000f, 0.4727f), new Vector2(0.6250f, 0.6182f)) },
        { HumanSlot.LFinger, ("ui/uld/Character_hr1.tex", new Vector2(0.5000f, 0.4727f), new Vector2(0.6250f, 0.6182f)) },
        { HumanSlot.Face, ("ui/uld/Character_hr1.tex", new Vector2(0.0000f, 0.8545f), new Vector2(0.1250f, 1.0000f)) },
    };
    
    private readonly static Dictionary<EquipSlot, (string TexturePath, Vector2 uvMin, Vector2 uvMax)> EmptyEquipSlotIcons = new() {
        { EquipSlot.Head, ("ui/uld/Character_hr1.tex", new Vector2(0.2500f, 0.3273f), new Vector2(0.3750f, 0.4727f)) },
        { EquipSlot.Body, ("ui/uld/Character_hr1.tex", new Vector2(0.3750f, 0.3273f), new Vector2(0.5000f, 0.4727f)) },
        { EquipSlot.Hands, ("ui/uld/Character_hr1.tex", new Vector2(0.5000f, 0.3273f), new Vector2(0.6250f, 0.4727f)) },
        { EquipSlot.Legs, ("ui/uld/Character_hr1.tex", new Vector2(0.7500f, 0.3273f), new Vector2(0.8750f, 0.4727f)) },
        { EquipSlot.Feet, ("ui/uld/Character_hr1.tex", new Vector2(0.0000f, 0.4727f), new Vector2(0.1250f, 0.6182f)) },
        { EquipSlot.Ears, ("ui/uld/Character_hr1.tex", new Vector2(0.1250f, 0.4727f), new Vector2(0.2500f, 0.6182f)) },
        { EquipSlot.Neck, ("ui/uld/Character_hr1.tex", new Vector2(0.2500f, 0.4727f), new Vector2(0.3750f, 0.6182f)) },
        { EquipSlot.Wrists, ("ui/uld/Character_hr1.tex", new Vector2(0.3750f, 0.4727f), new Vector2(0.5000f, 0.6182f)) },
        { EquipSlot.RFinger, ("ui/uld/Character_hr1.tex", new Vector2(0.5000f, 0.4727f), new Vector2(0.6250f, 0.6182f)) },
        { EquipSlot.LFinger, ("ui/uld/Character_hr1.tex", new Vector2(0.5000f, 0.4727f), new Vector2(0.6250f, 0.6182f)) },
        { EquipSlot.MainHand, ("ui/uld/Character_hr1.tex", new Vector2(0f, 0.3273f), new Vector2(0.1250f, 0.4727f)) },
        { EquipSlot.OffHand, ("ui/uld/Character_hr1.tex", new Vector2(0.1250f, 0.3273f), new Vector2(0.2500f, 0.4727f)) },
    };

    private static void Draw(EquipItem equipItem, (string TexturePath, Vector2 uvMin, Vector2 uvMax)? emptySlotTexture) {
        var size = new Vector2(ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().FramePadding.Y * 4 + ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Group()) {
            if (equipItem.IconId.Id != 0) {
                var tex = TextureProvider.GetFromGameIcon(equipItem.IconId.Id).GetWrapOrEmpty();
                ImGui.Image(tex.Handle, size);
            } else {
                ImGui.Dummy(size);
                var dl = ImGui.GetWindowDrawList();
                dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.FrameBg), size.X * 0.15f, ImDrawFlags.RoundCornersAll);

                if (emptySlotTexture != null) {
                    var tex = TextureProvider.GetFromGame(emptySlotTexture.Value.TexturePath).GetWrapOrDefault();
                    if (tex != null) {
                        dl.AddImage(tex.Handle, ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), emptySlotTexture.Value.uvMin, emptySlotTexture.Value.uvMax);
                    }
                }
                

            }
#if DEBUG
            if (ImGui.GetIO().KeyAlt && ImGui.GetIO().KeyShift) {
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin(), 0xFF0000FF, $"{equipItem.IconId.Id}");
            }
#endif
        }
    }
    
    public static void Draw(HumanSlot slot, EquipItem equipItem) {
        Draw(equipItem, EmptyIcons.GetValueOrDefault(slot));
    }
    
    public static void Draw(EquipSlot slot, EquipItem equipItem) {
        Draw(equipItem, EmptyEquipSlotIcons.GetValueOrDefault(slot));
    }
}
