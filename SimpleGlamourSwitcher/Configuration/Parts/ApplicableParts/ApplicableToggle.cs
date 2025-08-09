using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using OtterGuiInternal.Utility;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public record ApplicableToggle : Applicable<ToggleType> {
    public bool Toggle;
    
    public override void ApplyToCharacter(ToggleType toggleType, ref bool requestRedraw) {
        if (!Apply) return;
        var metaFlag = toggleType.ToMetaFlag();
        if (metaFlag == 0) return;
        GlamourerIpc.SetMetaState.Invoke(0, metaFlag, Toggle);


    }
    
    public static ApplicableToggle FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, ToggleType hatVisible, bool apply, bool toggle) {
        return new ApplicableToggle() {
            Apply = apply && defaultOptionsProvider.DefaultEnabledToggles.Contains(hatVisible),
            Toggle = toggle
        };
    }

    public bool ShowToggleEditor(string label) {

        using (ImRaii.Group()) {
            var size = ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2;
            
            ImGui.Dummy(new Vector2(size));
            

            var dl = ImGui.GetWindowDrawList();

            dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.FrameBg), ImGui.GetStyle().FrameRounding);
            dl.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGuiCol.Border), ImGui.GetStyle().FrameRounding, ImDrawFlags.None, ImGui.GetStyle().FrameBorderSize);
            
            if (Apply) {
                if (Toggle) {
                    SymbolHelpers.RenderCheckmark(dl, ImGui.GetItemRectMin() + new Vector2(ImGui.GetStyle().FramePadding.Y), 0xFF00FF00, ImGui.GetTextLineHeight());
                } else {
                    SymbolHelpers.RenderCross(dl, ImGui.GetItemRectMin() + new Vector2(ImGui.GetStyle().FramePadding.Y), 0xFF0000FF, ImGui.GetTextLineHeight());
                }
               
            } else {
                SymbolHelpers.RenderDot(dl, ImGui.GetItemRectMin() + new Vector2(ImGui.GetStyle().FramePadding.Y), 0xFFAAAAAA, ImGui.GetTextLineHeight());
            }

            ImGui.SameLine();
            ImGui.TextUnformatted(" " + label.Split("##")[0]);
        }

        if (ImGui.IsItemClicked()) {
            if (Apply) {
                if (Toggle) {
                    Toggle = false;
                    Apply = true;
                } else {
                    Toggle = false;
                    Apply = false;
                }
            } else {
                Toggle = true;
                Apply = true;
            }

            return true;
        }

        return false;
    }
}
