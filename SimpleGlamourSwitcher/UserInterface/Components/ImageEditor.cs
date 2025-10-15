using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.UserInterface.Page;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class ImageEditor {

    private static FileDialogManager _fileDialogManager = new();

    static ImageEditor() {
        PluginInterface.UiBuilder.Draw += _fileDialogManager.Draw;
    }


    private static readonly bool NoScreenshot = typeof(IDalamudPlugin).Assembly.GetName().Version <= new Version(13, 0, 0, 3);
    
    public static void Draw(IImageProvider imageProvider, PolaroidStyle style, string previewName, ref WindowControlFlags controlFlags) {
        
        imageProvider.TryGetImage(out var image);

        var actualSize = Polaroid.GetActualSize(style);
        
        if (ImGui.GetContentRegionAvail().X < actualSize.X) {
            style = style.FitTo(ImGui.GetContentRegionAvail());
            actualSize = Polaroid.GetActualSize(style);
        } else if (actualSize.X < ImGui.GetContentRegionAvail().X / 2) {
            ImGui.SetCursorPosX(ImGui.GetStyle().ItemSpacing.X);
        } else {
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2f - Polaroid.GetActualSize(style).X / 2f);
        }

        using (ImRaii.Group()) {

            var confirmReplace = image == null || ImGui.GetIO().KeyShift;
            
            Vector2 buttonSize;
            var sideBySide = false;
            if (actualSize.X < ImGui.GetContentRegionAvail().X / 2) {
                Polaroid.Draw(image, imageProvider.ImageDetail, previewName, style);
                ImGui.SameLine();
                sideBySide = true;
                ImGui.BeginGroup();
                buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2);
            } else {
                Polaroid.Draw(image, imageProvider.ImageDetail, previewName, style);
                buttonSize = new Vector2(ImGui.GetItemRectSize().X, ImGui.GetTextLineHeightWithSpacing() * 2);
            }
            
            using (ImRaii.Disabled(NoScreenshot && !ImGui.GetIO().KeyAlt)) {
                if (ImGuiExt.ButtonWithIcon("Take Screenshot", FontAwesomeIcon.Image, buttonSize)) {
                    Plugin.ScreenshotWindow.BeginScreenshot(style, imageProvider);
                }
            }

            if (NoScreenshot && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.SetTooltip("Not available in current dalamud version. Please wait for an update.");
            }

            if (ImGuiExt.ButtonWithIcon("Load Image", FontAwesomeIcon.FileUpload, buttonSize)) {
                controlFlags |= WindowControlFlags.PreventClose;
                Plugin.MainWindow.AllowAutoClose = false;
                _fileDialogManager.Reset();
                _fileDialogManager.OpenFileDialog("Select Image...", $"Image Files ({string.Join(' ', Common.SupportedImageFileTypes)}){{{string.Join(',', Common.SupportedImageFileTypes.Select(t => $".{t}"))}}}", imageProvider.LoadFile, 1, startPath: PluginConfig.ImageFilePickerLastPath.OrDefault(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));
            }
            
            using (ImRaii.Disabled(image == null || imageProvider.IsUsingDefaultImage())) {
                if (ImGuiExt.ButtonWithIcon("Crop Image", FontAwesomeIcon.Crop, buttonSize)) {
                    Plugin.MainWindow.OpenPage(new ImageEditorPage(imageProvider, style));
                }
            }

            if (sideBySide) {
                ImGui.EndGroup();
            }
            
        }
            
            

        
        
    }
}
