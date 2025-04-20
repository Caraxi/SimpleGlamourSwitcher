using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.UserInterface.Windows;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public abstract class Page {

    public MainWindow MainWindow => Plugin.MainWindow;

    protected List<ButtonInfo> BottomRightButtons { get; } = [];


    public virtual bool AllowStack => true;


    public float SidebarScaleRight { get; protected set; } = 1f;
    public float SidebarScaleLeft { get; protected set; } = 1f;

    protected Page() {
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.Cog, "Settings", () => {
            if (MainWindow?.ActivePage is ConfigPage) {
                MainWindow.PopPage();
            } else {
                MainWindow?.OpenPage(new ConfigPage());
            }
        }) { DisplayPriority = -100 });
        
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.Clipboard, "Changelog", () => {
            if (MainWindow.ActivePage is ChangeLogPage) {
                MainWindow.PopPage();
            } else {
                MainWindow.OpenPage(new ChangeLogPage());
            }
        }) { DisplayPriority = -99 });
        
    }
    
    public virtual void DrawLeft(ref WindowControlFlags controlFlags) {
        if (ImGuiExt.ButtonWithIcon("Back", FontAwesomeIcon.CaretLeft, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
            MainWindow.PopPage();
        }
    }
    
    public virtual void DrawRight(ref WindowControlFlags controlFlags) {
        DrawBottomRightButtons(ref controlFlags);
    }

    private void DrawBottomRightButtons(ref WindowControlFlags controlFlags) {
        if (BottomRightButtons.Count == 0) return;
        
        var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2);

        
        var totalSize = buttonSize.Y * BottomRightButtons.Count + (ImGui.GetStyle().ItemSpacing.Y * (BottomRightButtons.Count + 1));
        
        ImGui.Dummy(new Vector2(0, ImGui.GetContentRegionAvail().Y - totalSize));
        
        foreach (var btn in BottomRightButtons.OrderByDescending(btn => btn.DisplayPriority)) {
            using (ImRaii.Disabled(btn.IsDisabled()))
            using (ImRaii.PushId(btn.Id)) {
                
                if (btn.Icon != 0) {
                    if (ImGuiExt.ButtonWithIcon(btn.Text, btn.Icon, buttonSize)) {
                        btn.Action();
                    }
                } else {
                    if (ImGui.Button(btn.Text, buttonSize)) {
                        btn.Action();
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(btn.Tooltip)) {
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    ImGui.SetTooltip(btn.Tooltip);
                }
            }
            
        }
    }

    public virtual void Refresh() {
        
    }
    

    public virtual void DrawTop(ref WindowControlFlags controlFlags) {
        ImGuiExt.CenterText("Glamour Switcher", shadowed: true);
    }
    
    public virtual void DrawCenter(ref WindowControlFlags controlFlags) {
        
    }
}
