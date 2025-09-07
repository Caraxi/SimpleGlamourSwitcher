using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

namespace SimpleGlamourSwitcher.UserInterface.Windows;

public class ScreenshotWindow() : Window("Photo | Simple Glamour Switcher", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground) {
    
    private IImageProvider? imageProvider;
    private PolaroidStyle? polaroidStyle;
    private PolaroidStyle? ScaledStyle => polaroidStyle == null ? null : polaroidStyle with { ImageSize = polaroidStyle.ImageSize * scale };
    private float scale = 2f;
    private IDalamudTextureWrap? wrap;
    private Task<IDalamudTextureWrap>? textureWrapTask;
    
    public void BeginScreenshot(PolaroidStyle? style, IImageProvider? imageProvider) {
        this.imageProvider = imageProvider;
        this.polaroidStyle = style;
        IsOpen = true;
    }

    public override bool DrawConditions() {
        return imageProvider != null && polaroidStyle != null;;
    }

    public override void PreDraw() {
        ForceMainWindow = true;
        this.AllowPinning = false;
        this.AllowClickthrough = false;
        base.PreDraw();
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
    }

    public override void Draw() {
        ImGui.GetBackgroundDrawList().AddRectFilled(ImGui.GetMainViewport().Pos, ImGui.GetMainViewport().Pos + ImGui.GetMainViewport().Size, 0xB0000000);
        if (imageProvider == null || ScaledStyle == null) return;

        if (ImGui.GetIO().MouseWheel > 0) {
            scale *= 1.025f;
            ImGui.SetWindowPos(ImGui.GetWindowPos() - (ImGui.GetWindowSize() * 1.025f - ImGui.GetWindowSize()) / 2);
        } else if (ImGui.GetIO().MouseWheel < 0) {
            scale /= 1.025f;
            ImGui.SetWindowPos(ImGui.GetWindowPos() - (ImGui.GetWindowSize() / 1.025f - ImGui.GetWindowSize()) / 2);
        }

        if (wrap == null) {
            textureWrapTask ??= TextureProvider.CreateFromImGuiViewportAsync(new ImGuiViewportTextureArgs() { AutoUpdate = true, KeepTransparency = false, TakeBeforeImGuiRender = true, ViewportId = ImGui.GetMainViewport().ID});
            if (textureWrapTask.IsCompletedSuccessfully) {
                wrap = textureWrapTask.Result;
                textureWrapTask = null;
            }
        }

        Polaroid.DrawDummy(ScaledStyle);
        var imageDetail = new ImageDetail() { UvMin = (ImGui.GetItemRectMin() + ScaledStyle.FramePadding) / ImGui.GetMainViewport().WorkSize, UvMax = (ImGui.GetItemRectMin() + ScaledStyle.FramePadding + ScaledStyle.ImageSize) / ImGui.GetMainViewport().WorkSize };
        Polaroid.DrawPolaroid(wrap, imageDetail, "Screenshot", ScaledStyle);
        
        if (wrap != null && ImGui.Button("Take Screenshot", new Vector2(ImGui.GetItemRectSize().X, 24 * ImGuiHelpers.GlobalScale))) {
            unsafe {
                var originalUiVisibility = !RaptureAtkUnitManager.Instance()->Flags.HasFlag(AtkUnitManagerFlags.UiHidden);
                if (originalUiVisibility) {
                    RaptureAtkModule.Instance()->SetUiVisibility(false);
                }
                 
                Framework.RunOnTick(() => {
                    TextureProvider.CreateFromExistingTextureAsync(wrap, new TextureModificationArgs() { NewHeight = (int)(polaroidStyle!.ImageSize.Y * 2), NewWidth = (int)(polaroidStyle!.ImageSize.X * 2), Uv0 = imageDetail.UvMin, Uv1 = imageDetail.UvMax }, true).ContinueWith((t) => {
                        if (t.IsCompletedSuccessfully) {
                            imageProvider.LoadImage(t.Result); 
                            t.Result.Dispose();
                            IsOpen = false;
                        }
                    });

                    if (originalUiVisibility) {
                        RaptureAtkModule.Instance()->SetUiVisibility(true);
                    }
                }, delayTicks: 2);
            }
        }
    }

    public override void PostDraw() {
        base.PostDraw();
        ImGui.PopStyleVar();
    }

    public override void OnClose() {
        base.OnClose();
        wrap?.Dispose();
        wrap = null;
        textureWrapTask = null;
    }
}
