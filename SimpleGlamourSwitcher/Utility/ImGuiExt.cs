using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.Utility;

public static class ImGuiExt {

    public static void CenterText(string text, Vector2? size = null, bool centerVertically = false, bool centerHorizontally = true, bool shadowed = false, Style? colours = null) {
        size ??= ImGui.GetContentRegionAvail();
        colours ??= Style.Default;
        var textSize  = ImGui.CalcTextSize(text);
        var centerPos = ImGui.GetCursorPos() + size.Value * new Vector2(centerHorizontally ? 0.5f : 0f, centerVertically ? 0.5f : 0f) - textSize * new Vector2(centerHorizontally ? 0.5F : 0f, centerVertically ? 0.5f : 0f);
        ImGui.SetCursorPos(centerPos);
        if (shadowed) {
            ShadowText(text, colours);
        } else {
            using (ImRaii.PushColor(ImGuiCol.Text, colours.Text.U32)) {
                ImGui.TextUnformatted(text); 
            }
        }
    }
    
    public static void ShadowText(string text, Style? colours = null) {
        colours ??= Style.Default;
        var textSize = ImGui.CalcTextSize(text);
        ImGui.Dummy(textSize);
        var dl = ImGui.GetWindowDrawList();
        var pos = ImGui.GetItemRectMin();
        dl.AddShadowedText(pos, text, colours.ShadowText);
    }


    public static void AddShadowedText(this ImDrawListPtr drawList, Vector2 position, string text, ShadowTextStyle? style = null) {
        style ??= Style.Default.ShadowText;

        
        for (var sx = -style.Size; sx < style.Size; sx++) {
            for (var sy = -style.Size; sy < style.Size; sy++) {
                drawList.AddText(position + new Vector2(sx, sy) + style.Offset, style.ShadowColour, text);
            }
        }
        drawList.AddText(position, style.TextColour, text);
        drawList.AddText(position, style.TextColour, text);
    }
    
    public static bool CheckboxTriState(string label, ref bool? value, bool allowSwitchToPartial = false) {
        var v = value ?? false;
        
        try {
            if (ImGui.Checkbox(label, ref v)) {
                value = value switch {
                    null => true,
                    true => false,
                    _ => allowSwitchToPartial ? null : true
                };
                return true;
            }
        } finally {
            if (value == null) {
                ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() with { X = ImGui.GetItemRectMin().X + ImGui.GetItemRectSize().Y } - ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.CheckMark), 3);
            }
        }

        return false;
    }

    public static bool ButtonWithIcon(string label, FontAwesomeIcon icon, Vector2 size) {
        try {
            return ImGui.Button(label, size);
        } finally {
            if (PluginConfig.ShowButtonIcons) {
                using (PluginInterface.UiBuilder.IconFontHandle.Push()) {
                    var textSize = ImGui.CalcTextSize(icon.ToIconString());
                    ImGui.GetWindowDrawList().AddText(UiBuilder.IconFont, UiBuilder.IconFont.FontSize, ImGui.GetItemRectMin() + ImGui.GetItemRectSize() * new Vector2(0, 0.5f) - textSize * new Vector2(-0.5f, 0.5f), ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
                }
            }
            
        }
    }
    
    public static bool SelectableWithNote(string label, string note, bool isSelected = false, IFontHandle? noteFont = null) {
        using (ImRaii.Group()) {
            if (ImGui.Selectable(label, isSelected)) {
                return true;
            }
            
            ImGui.SameLine();
            using ((noteFont ?? PluginInterface.UiBuilder.DefaultFontHandle).Push()) {
                var idSize = ImGui.CalcTextSize(note);
                var dummySize = ImGui.GetContentRegionAvail().X - idSize.X - 10;
                if (dummySize > 0) {
                    ImGui.Dummy(new Vector2(dummySize, 1));
                    ImGui.SameLine();
                }
                ImGui.TextDisabled(note);
            }

            return false;
        }
    }


    private static Vector2? _defaultIconButtonSize;
    public static bool IconButton(string id, FontAwesomeIcon icon, Vector2? buttonSize = null) {
        if (!id.StartsWith("##")) id = $"##{id}";
        try {
            if (_defaultIconButtonSize != null) return ImGui.Button($"{id}", buttonSize ?? _defaultIconButtonSize.Value);
            var f = false;
            ImGui.Checkbox("###dummy", ref f);
            _defaultIconButtonSize = ImGui.GetItemRectSize();
            return false;
        } finally {
            using (PluginService.PluginUi.IconFontHandle.Push()) {
                ImGui.GetWindowDrawList().AddText(UiBuilder.IconFont, UiBuilder.IconFont.FontSize, ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2 - ImGui.CalcTextSize(icon.ToIconString()) / 2, ImGui.GetColorU32(ImGuiCol.Text), icon.ToIconString());
            }
        }
    }

    public static void SameLineInner() {
        ImGui.SameLine();
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = ImGui.GetItemRectMax().X + ImGui.GetStyle().ItemInnerSpacing.X });
    }
    
    public static void SameLineNoSpace() {
        ImGui.SameLine();
        ImGui.SetCursorScreenPos(ImGui.GetCursorScreenPos() with { X = ImGui.GetItemRectMax().X });
    }
}
