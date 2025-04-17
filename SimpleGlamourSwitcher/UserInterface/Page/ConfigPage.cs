using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using ECommons.ImGuiMethods;
using ImGuiNET;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class ConfigPage : Page {
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText("Config", shadowed: true);
    }

    public override void DrawCenter(ref WindowControlFlags controlFlags) {


        var maxW = Plugin.ConfigWindow.SizeConstraints?.MaximumSize.X ?? 640;
        
        if (ImGui.GetContentRegionAvail().X > maxW * ImGuiHelpers.GlobalScale) {
            
            ImGui.Dummy(new Vector2((ImGui.GetContentRegionAvail().X - maxW * ImGuiHelpers.GlobalScale) / 2f));
            ImGui.SameLine();
        }

        if (ImGui.BeginChild("config", new Vector2(MathF.Min(maxW * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X), ImGui.GetContentRegionAvail().Y))) {
            Plugin.ConfigWindow.Draw();
        }
        
        ImGui.EndChild();
    }
    
    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        if (ImGuiExt.ButtonWithIcon("Back", FontAwesomeIcon.CaretLeft, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
            MainWindow.PopPage();
        }
    }
}
