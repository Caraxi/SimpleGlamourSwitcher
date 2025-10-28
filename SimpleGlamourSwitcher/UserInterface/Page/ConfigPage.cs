using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class ConfigPage : Page {
    public override bool AllowStack => false;
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText("Config", shadowed: true);
    }

    public override void DrawCenter(ref WindowControlFlags controlFlags) {

        controlFlags |= WindowControlFlags.PreventClose;

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
}
