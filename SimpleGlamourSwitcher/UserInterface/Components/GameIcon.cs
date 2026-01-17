using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class GameIcon {
    public static void Draw(uint iconId) {
        var size = new Vector2(ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().FramePadding.Y * 4 + ImGui.GetStyle().ItemSpacing.Y);
        using (ImRaii.Group()) {
            if (iconId != 0) {
                try {
                    var tex = TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
                    ImGui.Image(tex.Handle, size);
                } catch (Exception ex) {
                    ImGui.Dummy(size);
                    var dl = ImGui.GetWindowDrawList();
                    dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.FrameBg), size.X * 0.15f, ImDrawFlags.RoundCornersAll);
                    dl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), 0xFF0000FF, size.X * 0.15f, ImDrawFlags.RoundCornersAll);
                    if (ImGui.IsItemHovered()) {
                        using (ImRaii.Tooltip()) {
                            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"{ex.Message}");
                        }
                    }
                }
                
            } else {
                ImGui.Dummy(size);
                var dl = ImGui.GetWindowDrawList();
                dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.FrameBg), size.X * 0.15f, ImDrawFlags.RoundCornersAll);
            }
            #if DEBUG
            if (ImGui.GetIO().KeyAlt && ImGui.GetIO().KeyShift) {
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin(), 0xFF0000FF, $"{iconId}");
            }
            #endif
        }
    }
}