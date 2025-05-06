using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
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
            ChangelogFor("1.0.0.11", () => {
                C("Will now detect missing mods assigned to an outfit.");
                C("Added ability to update a mod assigned on an item, maintaining the associated configuration .");
            });
            ChangelogFor("1.0.0.10", () => {
                C("Added option to set image sizes for the root outfit folder.");
                C("Added ability to adjust padding around images.");
            });
            ChangelogFor("1.0.0.9", "Added protections for invalid items in equipment slots.");
            ChangelogFor("1.0.0.8", () => {
                C("Added ability to change selected dyes on items.");
                C("Added ability to change selected items.");
                C("Added ability to edit selected mods on items.");
                C("Improved icon display for 'Nothing' items.");
            });
        }
    }

    private static void ChangelogFor(string label, Action draw) {
        _configIndex++;
        if (_configIndex == 4) _isOldExpanded = ImGui.TreeNodeEx("Old Versions", ImGuiTreeNodeFlags.NoTreePushOnOpen);
        if (_configIndex >= 4 && _isOldExpanded == false) return;
        ImGui.Text($"{label}:");
        ImGui.Indent();
        draw();
        ImGui.Unindent();
    }

    private static void ChangelogFor(string label, string singleLineChangelog) {
        ChangelogFor(label, () => { C(singleLineChangelog); });
    }

    private static void C(string text, int indent = 0, Vector4? color = null) {
        for (var i = 0; i < indent; i++) ImGui.Indent();
        if (color != null)
            ImGui.TextColored(color.Value, $"- {text}");
        else
            ImGui.Text($"- {text}");

        for (var i = 0; i < indent; i++) ImGui.Unindent();
    }
}
