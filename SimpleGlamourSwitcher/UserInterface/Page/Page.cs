using System.Numerics;
using Dalamud.Interface;
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


    public float SidebarScaleRight { get; protected set; } = 1f;
    public float SidebarScaleLeft { get; protected set; } = 1f;

    protected Page() {
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.Cog, () => {
            if (MainWindow?.ActivePage is ConfigPage) {
                MainWindow?.PopPage();
            } else {
                MainWindow?.OpenPage(new ConfigPage());
            }
        }) { DisplayPriority = -100 });
    }
    
    public virtual void DrawLeft(ref WindowControlFlags controlFlags) {
        
    }
    
    public virtual void DrawRight(ref WindowControlFlags controlFlags) {
        DrawBottomRightButtons(ref controlFlags);
    }

    private void DrawBottomRightButtons(ref WindowControlFlags controlFlags) {
        if (BottomRightButtons.Count == 0) return;
        
        var buttonsPerRow = MathF.Floor(ImGui.GetContentRegionAvail().X / (56f * ImGuiHelpers.GlobalScale)) + 1;
        var buttonWidth = MathF.Floor((ImGui.GetContentRegionAvail().X - (buttonsPerRow - 1f) * ImGui.GetStyle().ItemSpacing.X) / buttonsPerRow);
        var buttonRows = MathF.Ceiling(BottomRightButtons.Count / buttonsPerRow);


        var totalSize = (buttonRows * buttonWidth) + (ImGui.GetStyle().ItemSpacing.Y * (buttonRows + 1));
        
        
        
        ImGui.Dummy(new Vector2(0, ImGui.GetContentRegionAvail().Y - totalSize));
        
        var b = 0;
        
        for (b = 0; b < buttonsPerRow - BottomRightButtons.Count % buttonsPerRow; b++) {
            ImGui.Dummy(new Vector2(buttonWidth));
            ImGui.SameLine();
        }


        foreach (var btn in BottomRightButtons.OrderByDescending(btn => btn.DisplayPriority)) {
            if (b++ % buttonsPerRow == 0) {
                ImGui.NewLine();
            }
            using (ImRaii.Disabled(btn.IsDisabled()))
            using (ImRaii.PushId(btn.Id))
            using (ImRaii.PushFont(btn.Font, btn.Font.IsLoaded())) {
                if (ImGui.Button(btn.Text, new Vector2(buttonWidth))) {
                    btn.Action();
                }
            }

            if (!string.IsNullOrWhiteSpace(btn.Tooltip)) {
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    ImGui.SetTooltip(btn.Tooltip);
                }
            }

            
            ImGui.SameLine();
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
