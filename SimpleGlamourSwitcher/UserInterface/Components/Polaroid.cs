using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class Polaroid {

    public static Vector2 GetActualSize(PolaroidStyle style) {
        return style.ImageSize + style.FramePadding * 2 + style.FramePadding * Vector2.UnitY + new Vector2(0, ImGui.GetTextLineHeightWithSpacing());
    }

    public static bool Button(IDalamudTextureWrap? image, ImageDetail imageDetail, string text, Guid guid, PolaroidStyle? style = null) {
        style ??= PolaroidStyle.Default;
        var totalSize = GetActualSize(style);
        
        ImGui.Dummy(totalSize);
        var clicked = ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left);
        var hovered = ImGui.IsItemHovered();
        var active = hovered && ImGui.IsMouseDown(ImGuiMouseButton.Left);


        if (clicked || active) {
            style = style with { FrameColour = style.FrameActiveColour };
        } else if (hovered) {
            style = style with { FrameColour = style.FrameHoveredColour };
        }
        
        DrawPolaroid(image, imageDetail, text, style);
        
        return clicked;
    }

    public static void Draw(IDalamudTextureWrap? image, ImageDetail imageDetail, string text, PolaroidStyle? style = null) {
        style ??= PolaroidStyle.Default;
        var totalSize = GetActualSize(style);
        ImGui.Dummy(totalSize);
        DrawPolaroid(image, imageDetail, text, style);
    }
    
    private static void DrawPolaroid(IDalamudTextureWrap? image, ImageDetail imageDetail, string text, PolaroidStyle? style = null) {
        style ??= PolaroidStyle.Default;
        var tl = ImGui.GetItemRectMin();
        var br = ImGui.GetItemRectMax();
        var drawList = ImGui.GetWindowDrawList();
        drawList.PushClipRect(tl, br);
        drawList.AddRectFilled(tl, br, style.FrameColour, style.FrameRounding);
        drawList.AddRectFilled(tl + style.FramePadding, tl + style.FramePadding + style.ImageSize, style.BlankImageColour, style.FrameRounding, ImDrawFlags.RoundCornersTop);
        if (image != null) {
            drawList.AddImageRounded(image.Handle, tl + style.FramePadding, tl + style.FramePadding + style.ImageSize, imageDetail.UvMin, imageDetail.UvMax, uint.MaxValue, style.FrameRounding, ImDrawFlags.RoundCornersTop);
            
        }
        
        var textSize = ImGui.CalcTextSize(text);
        var labelPosition = tl + style.FramePadding + style.ImageSize * Vector2.UnitY + style.FramePadding * Vector2.UnitY + style.ImageSize * new Vector2(0.5f, 0f) - textSize * new Vector2(0.5f, 0f);
        var labelDrawList = drawList;
        
        if (textSize.X > br.X - tl.X && ImGui.IsItemHovered()) {
            labelDrawList = ImGui.GetForegroundDrawList();
            labelDrawList.AddRectFilled(labelPosition - style.FramePadding, labelPosition + textSize + style.FramePadding, style.FrameColour, style.FrameRounding, ImDrawFlags.RoundCornersAll);
        } 
        
        for (var sx = -style.LabelShadowSize; sx < style.LabelShadowSize; sx++) {
            for (var sy = -style.LabelShadowSize; sy < style.LabelShadowSize; sy++) {
                labelDrawList.AddText(labelPosition + new Vector2(sx, sy) + style.LabelShadowOffset, style.LabelShadowColour, text);
            }
        }
        labelDrawList.AddText(labelPosition , style.LabelColour, text);
        drawList.PopClipRect();
    }
}
