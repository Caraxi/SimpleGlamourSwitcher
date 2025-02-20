﻿using System.Numerics;
using ImGuiNET;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

namespace SimpleGlamourSwitcher.Utility;

public static class ImGuiExt {

    public static void CenterText(string text, Vector2? size = null, bool centerVertically = false, bool centerHorizontally = true, bool shadowed = false, Style? colours = null) {
        size ??= ImGui.GetContentRegionAvail();
        var textSize  = ImGui.CalcTextSize(text);
        var centerPos = ImGui.GetCursorPos() + size.Value * new Vector2(centerHorizontally ? 0.5f : 0f, centerVertically ? 0.5f : 0f) - textSize * new Vector2(centerHorizontally ? 0.5F : 0f, centerVertically ? 0.5f : 0f);
        ImGui.SetCursorPos(centerPos);
        if (shadowed) {
            ShadowText(text, colours);
        } else {
            ImGui.TextUnformatted(text); 
        }
       
    }
    
    public static void ShadowText(string text, Style? colours = null) {
        colours ??= Style.Default;
        
        var textSize = ImGui.CalcTextSize(text);

        ImGui.Dummy(textSize);

        var dl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetItemRectMin();


        for (var x = -2; x <= 2; x++) {
            for (var y = -2; y <= 2; y++) {
                dl.AddText(pos + new Vector2(x, y), colours.TextShadow ?? ImGui.GetColorU32(ImGuiCol.BorderShadow), text);
            }
        }
        
        dl.AddText(pos, colours.Text, text);
        


    }


    public static void AddShadowedText(this ImDrawListPtr drawList, Vector2 position, string text, Style? style = null) => drawList.AddShadowedText(position, text, (style ?? Style.Default).ShadowTextText);
    
    public static void AddShadowedText(this ImDrawListPtr drawList, Vector2 position, string text, ShadowTextStyle style) {
        
        for (var sx = -style.Size; sx < style.Size; sx++) {
            for (var sy = -style.Size; sy < style.Size; sy++) {
                drawList.AddText(position + new Vector2(sx, sy) + style.Offset, style.ShadowColour, text);
            }
        }
        drawList.AddText(position, style.TextColour, text);
    }
    
    public static bool CheckboxTriState(string label, ref bool? value) {
        var v = value ?? false;
        
        try {
            if (ImGui.Checkbox(label, ref v)) {
                value = value switch {
                    null => true,
                    true => false,
                    _ => null
                };
                return true;
            }
        } finally {
            if (value == null) {
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() with { X = ImGui.GetItemRectMin().X + ImGui.GetItemRectSize().Y } - ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.CheckMark));
            }
        }

        return false;
    }
    
    
    
}
