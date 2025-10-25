using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class CustomInput {
    
    public static bool InputText(string label, ref string text, uint maxLength, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, string errorMessage = "", TextInputStyle? style = null, FontAwesomeIcon? icon = null) {
        style ??= Style.Default.TextInput;
        var width = ImGui.CalcItemWidth();

        var extraPadding = new Vector2(0);

        if (icon != null) {
            using (PluginUi.IconFontHandle.Push()) {
                extraPadding.X += ImGui.CalcTextSize(icon.Value.ToIconString()).X;
                extraPadding.X += ImGui.GetStyle().ItemInnerSpacing.X;
            }
        }
        
        
        using (ImRaii.Group())
        using (ImRaii.PushId(label))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, style.BorderSize))
        using(ImRaii.PushColor(ImGuiCol.Border, style.BorderColour.U32))
        using(ImRaii.PushColor(ImGuiCol.Text, style.TextColour.U32))
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, style.FramePadding + extraPadding)) {
            try {
                if (style.PadTop) {
                    using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                        ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));    
                    }
                }
                
                ImGui.SetNextItemWidth(width);
                return ImGui.InputText("##customInputText", ref text, (int)maxLength, flags);
            } finally {

                if (icon != null) {
                    using (PluginUi.IconFontHandle.Push()) {
                        ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding - extraPadding, ImGui.GetColorU32(ImGuiCol.Text), icon.Value.ToIconString());
                    }
                }
                
                ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() - new Vector2(-8f, ImGui.GetTextLineHeight() / 2f), label.Split("##")[0], style.Label);
                if (!string.IsNullOrWhiteSpace(errorMessage)) {
                    var errorSize = ImGui.CalcTextSize(errorMessage);
                    ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X - errorSize.X - 8f, -ImGui.GetTextLineHeight() / 2f),  errorMessage, style.ErrorMessage);
                }
            }
        }
    }
    
    public static void ReadOnlyInputText(string label, string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, string errorMessage = "", TextInputStyle? style = null, FontAwesomeIcon? icon = null) {
        InputText(label, ref text, (uint)text.Length, flags | ImGuiInputTextFlags.ReadOnly, errorMessage, style, icon);
    }

    private static string comboSearchString = string.Empty;

    public static bool Combo(string label, string preview, Func<bool> drawContents, ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightLargest, string? errorMessage = "", ComboStyle? style = null, FontAwesomeIcon? icon = null) {
        // No Search Combo
        return Combo(label, preview, _ => drawContents(), comboFlags, errorMessage, style, false, icon);
    }

    public static bool Combo(string label, string preview, Func<string, bool> drawContents, ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightLargest, string? errorMessage = "", ComboStyle? style = null, bool showSearchBar = true, FontAwesomeIcon? icon = null) {
        style ??= Style.Default.Combo;
        var ret = false;

        var comboWidth = ImGui.CalcItemWidth();
        using (ImRaii.Group())
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, style.BorderSize))
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, style.FramePadding)) 
        using (ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, style.BorderSize))
        using (ImRaii.PushId(label)) {
            if (style.PadTop) {
                using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                    ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));    
                }
            }
            
            var comboOpen = false;
            using (ImRaii.PushColor(ImGuiCol.Text, style.PreviewColour.U32)) {
                ImGui.SetNextItemWidth(comboWidth);

                if (icon == null) {
                    comboOpen = ImGui.BeginCombo("##customCombo", preview, comboFlags);
                } else {
                    var iconSize = 0f;
                    var dl = ImGui.GetWindowDrawList();
                    var p = ImGui.GetCursorScreenPos();
                    using (PluginService.PluginUi.IconFontHandle.Push()) {
                        iconSize = ImGui.CalcTextSize(icon.Value.ToIconString()).X;
                        comboOpen = ImGui.BeginCombo("##customCombo", icon.Value.ToIconString(), comboFlags);
                    }
                    
                    dl.AddText(p + new Vector2(iconSize + ImGui.GetStyle().ItemInnerSpacing.X, 0) + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Text), preview);
                }
                
                
            }
            
            if (comboOpen) {
                if (showSearchBar) {
                    if (ImGui.IsWindowAppearing()) {
                        comboSearchString = string.Empty;
                        ImGui.SetKeyboardFocusHere();
                    }
                    using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 0))
                    using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4, 2))) {
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                        ImGui.InputTextWithHint("##search", "Search...", ref comboSearchString, 100);
                    }
                    ImGui.Separator();
                }
                

                if (ImGui.BeginChild("##comboContents", new Vector2(ImGui.GetContentRegionAvail().X, style.PopoutHeight * ImGuiHelpers.GlobalScale))) {
                    if (drawContents.Invoke(showSearchBar ? comboSearchString : string.Empty)) {
                        ImGui.CloseCurrentPopup();
                        ret = true;
                    }
                }
                ImGui.EndChild();
                
                ImGui.EndCombo();
            }
            
            ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() - new Vector2(-8f, ImGui.GetTextLineHeight() / 2f), label.Split("##")[0], style.Label);
            if (!string.IsNullOrWhiteSpace(errorMessage)) {
                var errorSize = ImGui.CalcTextSize(errorMessage);
                ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X - errorSize.X - 8f, -ImGui.GetTextLineHeight() / 2f),  errorMessage, style.ErrorMessage);
            }


           
            return ret;
        }
        
        
    }
}
