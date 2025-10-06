using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using OtterGuiInternal;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.UserInterface.Page;

namespace SimpleGlamourSwitcher.UserInterface.Windows;

public class MainWindow : Window {

    public bool IsFullscreen { get; private set; }

    public override bool DrawConditions() {
        return !Plugin.ScreenshotWindow.IsOpen ||  !Plugin.ScreenshotWindow.DrawConditions();
    }

    private bool allowAutoClose = true;
    private bool allowAutoCloseWaitForFocus;
    public bool AllowAutoClose {
        get => allowAutoClose;
        set {
            if (value == false) {
                allowAutoCloseWaitForFocus = false;
                allowAutoClose = false;
                return;
            }

            if (allowAutoClose) return;
            allowAutoCloseWaitForFocus = true;
        }
    }
    
    public override void OnOpen() {
        IsFullscreen = PluginConfig.FullScreenMode;
        
        AllowClickthrough = false;
        
        DisableWindowSounds = IsFullscreen;
        ForceMainWindow = IsFullscreen;
        
        AllowPinning = !IsFullscreen;
        RespectCloseHotkey = !IsFullscreen;

        if (IsFullscreen) {
            Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings;
        } else {
            Flags = ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoSavedSettings;
        }
        
        
        
        base.OnOpen();
    }
    
    public MainWindow() : base(nameof(SimpleGlamourSwitcher), forceMainWindow: true) {
        
    }
    
    public override void PreDraw() {
        if (IsFullscreen) {
            Position = (ImGuiHelpers.MainViewport.Pos / ImGuiHelpers.GlobalScale) + PluginConfig.FullscreenOffset + PluginConfig.FullscreenPadding;
            PositionCondition = ImGuiCond.Always;
            
            Size = (ImGuiHelpers.MainViewport.Size / ImGuiHelpers.GlobalScale) - PluginConfig.FullscreenPadding * 2;
            SizeCondition = ImGuiCond.Always;

            SizeConstraints = null;
            
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
            
        } else {
            Position = PluginConfig.WindowPosition;
            PositionCondition = ImGuiCond.Once;

            Size = PluginConfig.WindowSize;
            SizeCondition = ImGuiCond.Once;
            
            SizeConstraints = new WindowSizeConstraints() {
                MinimumSize = new Vector2(1024, 600),
                MaximumSize = new Vector2(4096, 2000)
            };
            
            
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, 0);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, 0);
            ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, 0);
            
        }
        
        
        
        

        
        
        
        
        base.PreDraw();
    }

    public override void PostDraw() {
        if (IsFullscreen) {
            ImGui.PopStyleVar(2);
        } else {
            ImGui.PopStyleColor(3);
        }

        base.PostDraw();
    }

    
    public Page.Page? ActivePage { get; private set; }

    public Page.Page? PreviousPage => previousPageStack.Count == 0 ? null : previousPageStack.Peek();
    
    private readonly Stack<Page.Page> previousPageStack = new();

    public void OpenPage(Page.Page page, bool cleanStack = false) {
        if (ActivePage == page) return;
        if (cleanStack) {
            previousPageStack.Clear();
        } else {
            if (ActivePage is { AllowStack: true }) previousPageStack.Push(ActivePage);
        }
        
        ActivePage = page;
    }

    public bool PopPage(bool allowClose = true) {
        if (previousPageStack.Count == 0) {
            if (allowClose) {
                ActivePage = null;
                IsOpen = false;
            }
            return false;
        }

        if (!previousPageStack.TryPop(out var p)) return false;
        ActivePage = p;
        p.Refresh();
        return true;
    }

    public override void OnClose() {
        ActivePage = null;
        previousPageStack.Clear();
    }

    public override void Draw() {
        try {
            DrawContent();
        } catch (Exception ex) {
            PluginLog.Error(ex, "Error drawing main window.");
        }
    }

    private void DrawContent() {
        ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize(), ImGui.ColorConvertFloat4ToU32(PluginConfig.BackgroundColour));

        ActivePage ??= new GlamourListPage();


        var controlFlags = WindowControlFlags.None;
        
        
        var page = ActivePage;
        
        using(ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, ImGui.GetStyle().ItemSpacing)) {
            
            if (ImGui.BeginChild("top", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 3 + ImGui.GetStyle().WindowPadding.Y * 2), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                page.DrawTop(ref controlFlags);
            }
            ImGui.EndChild();
            
            if (page.SidebarScaleLeft > 0) {
                if (ImGui.BeginChild("left", ImGui.GetContentRegionAvail() with {X = PluginConfig.SidebarSize * page.SidebarScaleLeft}, true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                    page.DrawLeft(ref controlFlags);
                }
                ImGui.EndChild();
                ImGui.SameLine();
            }
            
            
            if (ImGui.BeginChild("center", new Vector2(ImGui.GetContentRegionAvail().X - PluginConfig.SidebarSize * page.SidebarScaleRight - (ImGui.GetStyle().ItemSpacing.X * (page.SidebarScaleRight > 0 ? 1 : 0)), ImGui.GetContentRegionAvail().Y), true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                page.DrawCenter(ref controlFlags);
            }
            ImGui.EndChild();

            if (page.SidebarScaleRight > 0) {
                ImGui.SameLine();
                if (ImGui.BeginChild("right", ImGui.GetContentRegionAvail() with {X = PluginConfig.SidebarSize * page.SidebarScaleRight }, true, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                    page.DrawRight(ref controlFlags);
                }
                ImGui.EndChild();
            }
        }
        
        if (!controlFlags.HasFlag(WindowControlFlags.PreventClose) && IsFocused) {
            if (KeyState[VirtualKey.ESCAPE]) {
                PopPage();
                KeyState[VirtualKey.ESCAPE] = false;
            }
                
            if (ImGui.GetIO().MouseClicked[1]) {
                PopPage(IsFullscreen);
            }
        }
        
        if (!ImGui.IsWindowAppearing() && !IsFocused && AllowAutoClose && IsFullscreen && !controlFlags.HasFlag(WindowControlFlags.PreventClose)) {
            IsOpen = false;
            ActivePage = null;
            previousPageStack.Clear();
        }
        
        if (allowAutoCloseWaitForFocus && IsFocused) {
            allowAutoClose = true;
            allowAutoCloseWaitForFocus = false;
        }
        
        if (!IsFullscreen) {
            if (controlFlags.HasFlag(WindowControlFlags.PreventMove)) {
                ImGui.SetWindowPos(PluginConfig.WindowPosition);
                Flags |= ImGuiWindowFlags.NoMove;
            } else {
                Flags &= ~ImGuiWindowFlags.NoMove;
                PluginConfig.WindowPosition = ImGui.GetWindowPos();
            }
            
            PluginConfig.WindowSize = ImGui.GetWindowSize();
        }
    }

    public void HoldAutoClose() {
        AllowAutoClose = false;
        Framework.RunOnTick(() => {
            if (IsFocused) {
                HoldAutoClose();
            } else {
                AllowAutoClose = true;
            }
        }, delayTicks: 20);
    }
}
