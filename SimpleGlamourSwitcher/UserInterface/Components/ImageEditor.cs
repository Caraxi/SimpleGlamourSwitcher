using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
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
            


            if (ImGuiExt.ButtonWithIcon("Load Image", FontAwesomeIcon.FileUpload, buttonSize)) {
                controlFlags |= WindowControlFlags.PreventClose;
                Plugin.MainWindow.AllowAutoClose = false;
                _fileDialogManager.Reset();
                _fileDialogManager.OpenFileDialog("Select Image...", $"Image Files ({string.Join(' ', Common.SupportedImageFileTypes)}){{{string.Join(',', Common.SupportedImageFileTypes.Select(t => $".{t}"))}}}", imageProvider.LoadFile, 1, startPath: PluginConfig.ImageFilePickerLastPath.OrDefault(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)));
            }

            var clipText = ImGui.GetClipboardText();
            
            if (IImageProvider.SupportedImageFileTypes.Any(t => clipText.EndsWith($".{t}") && File.Exists(clipText))) {
                using (ImRaii.Disabled(!confirmReplace)) {
                    if (ImGuiExt.ButtonWithIcon("Load Image from Clipboard##filePath", FontAwesomeIcon.Clipboard, buttonSize)) {
                        controlFlags |= WindowControlFlags.PreventClose;
                        imageProvider.LoadFile(true, [clipText]);
                    }
                }

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    
                    ImGui.BeginTooltip();
                    ImGui.Text("Load Image from Clipboard");
                    ImGui.Separator();
                    var clipImg = TextureProvider.GetFromFileAbsolute(clipText).GetWrapOrDefault();
                    Polaroid.Draw(clipImg, imageProvider.ImageDetail, previewName, style);
                    if (!confirmReplace) {
                        ImGui.Text("Hold SHIFT to confirm");
                    }
                    ImGui.EndTooltip();
                }
            } else if (Common.TryGetClipboardImage(out var clipImage)) {
                using (ImRaii.Disabled(!confirmReplace)) {
                    if (ImGuiExt.ButtonWithIcon("Load Image from Clipboard##clipboardImage", FontAwesomeIcon.Clipboard, buttonSize)) {
                        controlFlags |= WindowControlFlags.PreventClose;
                        imageProvider.LoadImage(clipImage);
                    }
                }

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    
                    ImGui.BeginTooltip();
                    ImGui.Text("Load Image from Clipboard");
                    
                    if (!confirmReplace) {
                        ImGui.Text("Hold SHIFT to confirm");
                    }
                    
                    ImGui.EndTooltip();
                }
            } else if (Common.TryGetClipboardFile(out var clipFile) && IImageProvider.SupportedImageFileTypes.Any(f => clipFile.EndsWith($".{f}"))) {
                using (ImRaii.Disabled(!confirmReplace)) {
                    if (ImGuiExt.ButtonWithIcon("Load Image from Clipboard##clipboardFile", FontAwesomeIcon.Clipboard, buttonSize)) {
                        controlFlags |= WindowControlFlags.PreventClose;
                        
                        var clipImg = TextureProvider.GetFromFileAbsolute(clipFile).GetWrapOrDefault();
                        if (clipImg != null) {
                            var detail = ImageDetail.CropFor(clipImg, style);
                            imageProvider.SetImageDetail(detail);
                        }
                        imageProvider.LoadFile(true, [clipFile]);
                    }
                }

                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    
                    ImGui.BeginTooltip();
                    ImGui.Text("Load Image from Clipboard");
                    ImGui.Separator();
                    var clipImg = TextureProvider.GetFromFileAbsolute(clipFile).GetWrapOrDefault();
                    var detail = ImageDetail.CropFor(clipImg, style);
                    ImGui.Text($"{detail}");
                    Polaroid.Draw(clipImg, detail, previewName, style);
                    if (!confirmReplace) {
                        ImGui.Text("Hold SHIFT to confirm");
                    }
                    
                    ImGui.EndTooltip();
                }
            } else {
                using (ImRaii.Disabled()) {
                    ImGuiExt.ButtonWithIcon("Load Image from Clipboard##clipboardNone", FontAwesomeIcon.Clipboard, buttonSize);
                }
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
