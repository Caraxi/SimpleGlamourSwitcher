using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

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

        using (ImRaii.Group()) {
            Polaroid.DrawDummy(ScaledStyle);
            var imageDetail = new ImageDetail() { UvMin = (ImGui.GetItemRectMin() + ScaledStyle.FramePadding) / ImGui.GetMainViewport().Size, UvMax = (ImGui.GetItemRectMin() + ScaledStyle.FramePadding + ScaledStyle.ImageSize) / ImGui.GetMainViewport().Size };
            Polaroid.DrawPolaroid(wrap, imageDetail, "Screenshot", ScaledStyle);
            if (PluginConfig.ScreenshotGridlineStyle != GridlineStyle.None) {
                var tl = ImGui.GetItemRectMin() + ScaledStyle.FramePadding;
                var br =  ImGui.GetItemRectMin() + ScaledStyle.ImageSize + ScaledStyle.FramePadding;
                var sz = br - tl;
                var tr = tl + sz * Vector2.UnitX;
                var bl = tl + sz * Vector2.UnitY;
                var third = sz / 3f;
                var half = sz / 2f;
                var dl = ImGui.GetWindowDrawList();
                var color = ImGui.ColorConvertFloat4ToU32(ScaledStyle.FrameColour.Float4 with { W = ScaledStyle.FrameColour.Float4.W / 4f });
                var lineThickness = 3;

                switch (PluginConfig.ScreenshotGridlineStyle) {
                    case GridlineStyle.Thirds:
                        dl.AddLine(tl + third * Vector2.UnitX, bl + third * Vector2.UnitX, color, lineThickness);
                        dl.AddLine(tr - third * Vector2.UnitX, br - third * Vector2.UnitX , color, lineThickness);
                        dl.AddLine(tl + third * Vector2.UnitY, tr + third * Vector2.UnitY, color, lineThickness);
                        dl.AddLine(bl - third * Vector2.UnitY, br - third * Vector2.UnitY , color, lineThickness);
                        break;
                    case GridlineStyle.Diagonals:
                        dl.AddLine(tl, br, color, lineThickness);
                        dl.AddLine(bl, tr, color, lineThickness);
                        break;
                    case GridlineStyle.DiagonalSymmetry:
                        dl.AddLine(tl, br, color, lineThickness);
                        dl.AddLine(bl, tr, color, lineThickness);
                        dl.AddLine(tl + half * Vector2.UnitX, tl + half * Vector2.UnitY, color, lineThickness);
                        dl.AddLine(tl + half * Vector2.UnitX, br - half * Vector2.UnitY, color, lineThickness);
                        dl.AddLine(br - half * Vector2.UnitX, tl + half * Vector2.UnitY, color, lineThickness);
                        dl.AddLine(br - half * Vector2.UnitX, br - half * Vector2.UnitY, color, lineThickness);
                        if (ScaledStyle.ImageSize.Y > ScaledStyle.ImageSize.X * 1.5f) {
                            dl.AddLine(tl, tr + third * Vector2.UnitY, color, lineThickness);
                            dl.AddLine(tr, tl + third * Vector2.UnitY, color, lineThickness);
                            dl.AddLine(bl, br - third *  Vector2.UnitY, color, lineThickness);
                            dl.AddLine(br, bl - third * Vector2.UnitY, color, lineThickness);
                        } else if (ScaledStyle.ImageSize.X > ScaledStyle.ImageSize.Y * 1.5f) {
                            dl.AddLine(tl, bl + third * Vector2.UnitX, color, lineThickness);
                            dl.AddLine(tr, br - third * Vector2.UnitX, color, lineThickness);
                            dl.AddLine(bl, tl + third *  Vector2.UnitX, color, lineThickness);
                            dl.AddLine(br, tr - third * Vector2.UnitX, color, lineThickness);
                        }
                        
                        color = ImGui.ColorConvertFloat4ToU32(ScaledStyle.FrameColour.Float4 with { W = ScaledStyle.FrameColour.Float4.W / 8f });
                        dl.AddLine(tl + third * Vector2.UnitX, bl + third * Vector2.UnitX, color, lineThickness);
                        dl.AddLine(tr - third * Vector2.UnitX, br - third * Vector2.UnitX , color, lineThickness);
                        dl.AddLine(tl + third * Vector2.UnitY, tr + third * Vector2.UnitY, color, lineThickness);
                        dl.AddLine(bl - third * Vector2.UnitY, br - third * Vector2.UnitY , color, lineThickness);
                        break;
                    case GridlineStyle.None:
                    default:
                        dl.AddText(tl, color, $"Unsupported Grid Line Type:\n\t{PluginConfig.ScreenshotGridlineStyle}");
                        break;
                }
            }
            
            if (wrap != null && ImGui.Button("Take Screenshot", new Vector2(ImGui.GetItemRectSize().X, 24 * ImGuiHelpers.GlobalScale))) {
                unsafe {
                    var originalUiVisibility = !RaptureAtkUnitManager.Instance()->Flags.HasFlag(AtkUnitManagerFlags.UiHidden);
                    if (originalUiVisibility) {
                        RaptureAtkModule.Instance()->SetUiVisibility(false);
                    }

                    Framework.RunOnTick(() => {
                        TextureProvider.CreateFromExistingTextureAsync(wrap, new TextureModificationArgs() { NewHeight = (int)(polaroidStyle!.ImageSize.Y * 2), NewWidth = (int)(polaroidStyle!.ImageSize.X * 2), Uv0 = imageDetail.UvMin, Uv1 = imageDetail.UvMax }, true).ContinueWith((t) => {
                            if (t.IsCompletedSuccessfully) {
                                imageProvider.ImageDetail.UvMin = Vector2.Zero;
                                imageProvider.ImageDetail.UvMax = Vector2.One;
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

        ImGuiExt.SameLineNoSpace();
        using (ImRaii.Group()) {
            if (ImGui.Button($"＃###toggleGridline", new Vector2(24 * ImGuiHelpers.GlobalScale))) {
                var g = Enum.GetValues<GridlineStyle>();
                var i = g.IndexOf(PluginConfig.ScreenshotGridlineStyle);
                i++;
                if (g.Length <= i) {
                    PluginConfig.ScreenshotGridlineStyle = g[0];
                } else {
                    PluginConfig.ScreenshotGridlineStyle = g[i];
                }

                PluginConfig.Dirty = true;
            }

            if (ImGui.IsItemHovered()) {
                using (ImRaii.Tooltip()) {
                    ImGui.Text("Toggle Gridlines");
                    ImGui.TextDisabled($"{PluginConfig.ScreenshotGridlineStyle} [{(int)PluginConfig.ScreenshotGridlineStyle}]");
                }
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
