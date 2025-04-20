using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class ItemIcon {
    private static readonly Dictionary<HumanSlot, Action<HumanSlot, EquipItem, Vector2>> EmptyIcons = new();

    static ItemIcon() {
        EmptyIcons[HumanSlot.Head] = GetDrawBlankIcon(19);
        EmptyIcons[HumanSlot.Body] = GetDrawBlankIcon(20);
        EmptyIcons[HumanSlot.Hands] = GetDrawBlankIcon(21);
        EmptyIcons[HumanSlot.Legs] = GetDrawBlankIcon(23);
        EmptyIcons[HumanSlot.Feet] = GetDrawBlankIcon(24);
        EmptyIcons[HumanSlot.Ears] = GetDrawBlankIcon(25);
        EmptyIcons[HumanSlot.Neck] = GetDrawBlankIcon(26);
        EmptyIcons[HumanSlot.Wrists] = GetDrawBlankIcon(27);
        EmptyIcons[HumanSlot.RFinger] = GetDrawBlankIcon(28);
        EmptyIcons[HumanSlot.LFinger] = GetDrawBlankIcon(28);

        EmptyIcons[HumanSlot.Face] = GetDrawBlankIcon(55);
    }

    private static Action<HumanSlot, EquipItem, Vector2> GetDrawBlankIcon(int part) {
        return (_, _, size) => {
            using var characterUld = UiBuilder.LoadUld("ui/uld/Character.uld");
            var tex = characterUld.LoadTexturePart("ui/uld/Character_hr1.tex", part);
            ImGui.Dummy(size);
            var dl = ImGui.GetWindowDrawList();
            dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.FrameBg), size.X * 0.15f, ImDrawFlags.RoundCornersAll);
            if (tex != null)
                dl.AddImage(tex.ImGuiHandle, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
            else
                dl.AddText(ImGui.GetItemRectMin(), 0xFF0000FF, "x");
        };
    }

    public static void Draw(HumanSlot slot, EquipItem equipItem) {
        var size = new Vector2(ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().FramePadding.Y * 4 + ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Group()) {
            if (equipItem.IconId.Id != 0) {
                var tex = TextureProvider.GetFromGameIcon(equipItem.IconId.Id).GetWrapOrEmpty();
                ImGui.Image(tex.ImGuiHandle, size);
            } else {
                if (EmptyIcons.TryGetValue(slot, out var emptyDrawAction)) {
                    emptyDrawAction(slot, equipItem, size);
                    return;
                }

                ImGui.Dummy(size);
                var dl = ImGui.GetWindowDrawList();
                dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.FrameBg), size.X * 0.15f, ImDrawFlags.RoundCornersAll);
            }
#if DEBUG
            if (ImGui.GetIO().KeyAlt && ImGui.GetIO().KeyShift) {
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin(), 0xFF0000FF, $"{equipItem.IconId.Id}");
            }
#endif
        }
    }
}
