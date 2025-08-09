using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class ChangeLogPage : Page {
    public override bool AllowStack => false;
    private static int _configIndex;
    private static bool _isOldExpanded;
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText("Changelogs", shadowed: true);
    }
    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        _configIndex = 0;
        using (ImRaii.Child("changelogs", ImGui.GetContentRegionAvail())) {
            ChangeLogs.Draw();
        }
    }

    public static void ChangelogFor(string label, Action draw) {
        _configIndex++;
        if (_configIndex == 8) _isOldExpanded = ImGui.TreeNodeEx("Old Versions", ImGuiTreeNodeFlags.NoTreePushOnOpen);
        if (_configIndex >= 8 && _isOldExpanded == false) return;
        ImGui.Text($"{label}:");
        ImGui.Indent();
        draw();
        ImGui.Unindent();
    }
    
    public static void Change(string text, int indent = 0, Vector4? color = null) {
        for (var i = 0; i < indent; i++) ImGui.Indent();
        if (color != null)
            ImGui.TextColored(color.Value, $"- {text}");
        else
            ImGui.Text($"- {text}");

        for (var i = 0; i < indent; i++) ImGui.Unindent();
    }
}
