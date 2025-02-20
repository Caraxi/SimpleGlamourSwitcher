using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class CustomInput {
    
    public static bool InputText(string label, ref string text, uint maxLength, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, string errorMessage = "", TextInputStyle? style = null) {
        style ??= Style.Default.TextInput;
        
        using (ImRaii.PushId(label))
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 3))
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(32, 16))) {
            try {
                using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                    ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));    
                }
                
                return ImGui.InputText("##customInputText", ref text, maxLength, flags);
            } finally {
                ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() - new Vector2(-8f, ImGui.GetTextLineHeight() / 2f), label.Split("##")[0], style.Label);
                if (!string.IsNullOrWhiteSpace(errorMessage)) {
                    var errorSize = ImGui.CalcTextSize(errorMessage);
                    ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().X - errorSize.X - 8f, -ImGui.GetTextLineHeight() / 2f),  errorMessage, style.ErrorMessage);
                }
            }
        }
    }
    
    public static void ReadOnlyInputText(string label, string text, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, string errorMessage = "", TextInputStyle? style = null) {
        InputText(label, ref text, (uint)text.Length, flags | ImGuiInputTextFlags.ReadOnly, errorMessage, style);
    }

    private static string comboSearchString = string.Empty;

    public static bool Combo(string label, string preview, Func<bool> drawContents, ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightLargest, string? errorMessage = "", ComboStyle? style = null) {
        // No Search Combo
        return Combo(label, preview, _ => drawContents(), comboFlags, errorMessage, style, false);
    }

    public static bool Combo(string label, string preview, Func<string, bool> drawContents, ImGuiComboFlags comboFlags = ImGuiComboFlags.HeightLargest, string? errorMessage = "", ComboStyle? style = null, bool showSearchBar = true) {
        style ??= Style.Default.Combo;
        var ret = false;
        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 3))
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(32, 16))) 
        using (ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 3))
        using (ImRaii.PushId(label)) {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));    
            }

            var comboOpen = false;
            using (ImRaii.PushColor(ImGuiCol.Text, style.PreviewColour.U32)) {
                comboOpen = ImGui.BeginCombo("##customCombo", preview, comboFlags);
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
