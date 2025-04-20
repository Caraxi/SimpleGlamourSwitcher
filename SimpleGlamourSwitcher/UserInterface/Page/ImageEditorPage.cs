using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using ECommons.ImGuiMethods;
using ImGuiNET;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class ImageEditorPage(IImageProvider imageProvider, PolaroidStyle style) : Page {
    ImageDetail newDetail = new() { UvMin = imageProvider.ImageDetail.UvMin, UvMax = imageProvider.ImageDetail.UvMax };

    private Vector2? mouseClickStart;

    private RectangleHandle UvEditHandle = RectangleHandle.None;

    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        DrawEditor(ref controlFlags);
    }

    public void DrawEditor(ref WindowControlFlags controlFlags) {
        var largeWidth = ImGui.GetContentRegionAvail().X / 8f;
        var largeHeight = style.ImageSize.Y / style.ImageSize.X * largeWidth;

        var largeStyle = style with { ImageSize = new Vector2(largeWidth, largeHeight) };

        var actualSize = Polaroid.GetActualSize(largeStyle);

        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2 - actualSize.X - ImGui.GetStyle().ItemSpacing.X / 2f);

        Polaroid.Draw(imageProvider.GetImage(), imageProvider.ImageDetail, "Original", largeStyle);
        ImGui.SameLine();
        Polaroid.Draw(imageProvider.GetImage(), newDetail, "New", largeStyle);

        var editorSize = actualSize with { X = actualSize.X * 2 + ImGui.GetStyle().ItemSpacing.X };
        editorSize.Y = style.ImageSize.Y / style.ImageSize.X * editorSize.X;

        var image = imageProvider.GetImage();

        if (image != null && imageProvider.IsUsingDefaultImage() == false) {
            var actualEditorSize = image.Size.FitTo(editorSize);

            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2 - editorSize.X / 2);
            ImGui.Dummy(actualEditorSize);
            var dl = ImGui.GetWindowDrawList();

            dl.AddRectFilled(ImGui.GetItemRectMin() - new Vector2(2), ImGui.GetItemRectMax() + new Vector2(2), style.BlankImageColour);

            dl.AddImage(image.ImGuiHandle, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

/*
            dl.AddImage(image.ImGuiHandle, ImGui.GetItemRectMin(), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, newDetail.UvMin.Y), Vector2.Zero, new Vector2(1, newDetail.UvMin.Y));
            dl.AddImage(image.ImGuiHandle, ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, newDetail.UvMax.Y), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, 1), new Vector2(0, newDetail.UvMax.Y), new Vector2(1, 1));
          */

            var inactiveColour = 0xC0000000;

            dl.AddRectFilled(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, 0), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, newDetail.UvMin.Y), inactiveColour);
            dl.AddRectFilled(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, newDetail.UvMin.Y), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMin.X, newDetail.UvMax.Y), inactiveColour);
            dl.AddRectFilled(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMax.X, newDetail.UvMin.Y), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, newDetail.UvMax.Y), inactiveColour);
            dl.AddRectFilled(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, newDetail.UvMax.Y), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, 1), inactiveColour);

            // dl.AddImage(image.ImGuiHandle, ImGui.GetItemRectMin() + actualEditorSize * newDetail.UvMin, ImGui.GetItemRectMin() + actualEditorSize  * newDetail.UvMax, newDetail.UvMin, newDetail.UvMax);

            var grabSize = 5;

            if (mouseClickStart == null) {
                UvEditHandle = RectangleHandle.None;

                if (ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, newDetail.UvMin.Y) - new Vector2(0, grabSize), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, newDetail.UvMin.Y) + new Vector2(0, grabSize))) {
                    UvEditHandle |= RectangleHandle.Top;
                }

                if (ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMin.X, 0) - new Vector2(grabSize, 0), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMin.X, 1) + new Vector2(grabSize, 0))) {
                    UvEditHandle |= RectangleHandle.Left;
                }

                if (!UvEditHandle.HasFlag(RectangleHandle.Top) && ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, newDetail.UvMax.Y) - new Vector2(0, grabSize), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, newDetail.UvMax.Y) + new Vector2(0, grabSize))) {
                    UvEditHandle |= RectangleHandle.Bottom;
                }

                if (!UvEditHandle.HasFlag(RectangleHandle.Left) && ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMax.X, 0) - new Vector2(grabSize, 0), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMax.X, 1) + new Vector2(grabSize, 0))) {
                    UvEditHandle |= RectangleHandle.Right;
                }

                if (UvEditHandle == RectangleHandle.None && ImGui.IsMouseHoveringRect(ImGui.GetItemRectMin() + actualEditorSize * newDetail.UvMin, ImGui.GetItemRectMin() + actualEditorSize * newDetail.UvMax)) {
                    UvEditHandle = RectangleHandle.All;
                }
            }

            if (UvEditHandle != RectangleHandle.None) {
                controlFlags |= WindowControlFlags.PreventClose | WindowControlFlags.PreventMove;
            }

            var selectionColor = mouseClickStart == null ? 0xFFFF00FF : 0xFFFFFF00;
            var sideColour = mouseClickStart == null ? 0xFF00FFFF : 0xFFFFFF00;

            dl.AddLine(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, newDetail.UvMin.Y), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, newDetail.UvMin.Y), UvEditHandle == RectangleHandle.All ? selectionColor : UvEditHandle.HasFlag(RectangleHandle.Top) ? sideColour : 0xFF0000FF, 2);
            dl.AddLine(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMin.X, 0), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMin.X, 1), UvEditHandle == RectangleHandle.All ? selectionColor : UvEditHandle.HasFlag(RectangleHandle.Left) ? sideColour : 0xFF0000FF, 2);
            dl.AddLine(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMax.X, 0), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(newDetail.UvMax.X, 1), UvEditHandle == RectangleHandle.All ? selectionColor : UvEditHandle.HasFlag(RectangleHandle.Right) ? sideColour : 0xFF0000FF, 2);
            dl.AddLine(ImGui.GetItemRectMin() + actualEditorSize * new Vector2(0, newDetail.UvMax.Y), ImGui.GetItemRectMin() + actualEditorSize * new Vector2(1, newDetail.UvMax.Y), UvEditHandle == RectangleHandle.All ? selectionColor : UvEditHandle.HasFlag(RectangleHandle.Bottom) ? sideColour : 0xFF0000FF, 2);

            if (ImGui.IsMouseDown(ImGuiMouseButton.Left) && UvEditHandle != RectangleHandle.None) {
                if (mouseClickStart == null) {
                    mouseClickStart = ImGui.GetMousePos();
                } else {
                    var newPos = ImGui.GetMousePos();

                    if (Vector2.Distance(mouseClickStart.Value, newPos) > 1) {
                        var uvOld = (mouseClickStart.Value - ImGui.GetItemRectMin()) / actualEditorSize;
                        var uvNew = (newPos - ImGui.GetItemRectMin()) / actualEditorSize;

                        newDetail.NormalizeUv();

                        var maintainAspectRatio = !ImGui.GetIO().KeyShift;

                        if (UvEditHandle == RectangleHandle.All ? newDetail.MoveUv(uvNew - uvOld) : newDetail.MoveHandle(ref UvEditHandle, uvNew - uvOld, maintainAspectRatio)) {
                            mouseClickStart = newPos;
                        }
                    }
                }
            } else {
                mouseClickStart = null;
            }

            ImGuiExt.CenterText("Click and crag on edges to crop image.");
            ImGuiExt.CenterText("Drag from middle to move selection.");

            // dl.AddRect(ImGui.GetItemRectMin() + actualEditorSize * newDetail.UvMin, ImGui.GetItemRectMin() + actualEditorSize * newDetail.UvMax, 0xFF0000FF);

            ImGui.Text($"Editor Size: {actualEditorSize}");
            ImGui.Text($"UV Size: {actualEditorSize * newDetail.UvSize}");
            ImGui.Text($"UV Size: {actualEditorSize * newDetail.UvSize}");
            ImGui.Text($"UV Ratio: {newDetail.UvRatio}");
            ImGui.Text($"UV Ratio: {newDetail.UvHeightToWidthRatio}");
            ImGui.Text($"Display Size: {style.ImageSize}");
        }

        ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2 - editorSize.X / 2);
        if (ImGuiExt.ButtonWithIcon("Confirm", FontAwesomeIcon.CheckCircle, editorSize with { Y = ImGui.GetTextLineHeightWithSpacing() * 2 })) {
            controlFlags |= WindowControlFlags.PreventClose;
            imageProvider.SetImageDetail(newDetail);
            MainWindow.PopPage();
        }
    }

    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText("Image Editor", shadowed: true);
    }
}
