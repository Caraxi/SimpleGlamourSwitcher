using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Lumina.Excel.Sheets;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class StainPicker {
    private static byte _selectedShade = 2;

    private static readonly Dictionary<byte, Vector4> StainShadeHeaders = new() {
        { 2, new Vector4(1, 1, 1, 1) },
        { 4, new Vector4(1, 0, 0, 1) },
        { 5, new Vector4(0.75f, 0.5f, 0.3f, 1) },
        { 6, new Vector4(1f, 1f, 0.1f, 1) },
        { 7, new Vector4(0.5f, 1f, 0.25f, 1f) },
        { 8, new Vector4(0.3f, 0.5f, 1f, 1f) },
        { 9, new Vector4(0.7f, 0.45f, 0.9f, 1) },
        { 10, new Vector4(1f, 1f, 1f, 1f) }
    };

    public static bool Show(string label, ref byte stainId, Vector2 size, bool tooltip = true) {
        var edit = false;
        var texture = TextureProvider.GetFromGame("ui/uld/ListColorChooser_hr1.tex").GetWrapOrDefault();

        using (ImRaii.PushId(label)) {
            var stain = DataManager.GetExcelSheet<Stain>().GetRow(stainId);
            if (StainButton(stain, size, tooltip)) {
                ImGui.OpenPopup($"stainPicker_{label}");
            }

            if (texture == null) return false;

            ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() * Vector2.UnitY);
            using (ImRaii.PushColor(ImGuiCol.Border, 0xFFFFFFFF)) 
            using (ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2)) 
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(3, 3))) {
                if (ImGui.BeginPopup($"stainPicker_{label}", ImGuiWindowFlags.AlwaysAutoResize)) {

                    if (ImGui.IsWindowAppearing()) {
                        _selectedShade = stain.Shade;
                    }
                    
                    ImGui.Text(label.Split("##")[0]);
                    ImGui.Separator();

                    var stains = DataManager.GetExcelSheet<Stain>().Where(s => !string.IsNullOrWhiteSpace(s.Name.ExtractText())).OrderBy(s => s.Shade).ThenBy(s => s.SubOrder).ToList();
                    
                    foreach (var shade in StainShadeHeaders) {
                        using (ImRaii.Group()) {
                            var p = ImGui.GetCursorPos();
                            if (shade.Key == 10) {
                                ImGui.Image(texture.ImGuiHandle, size * 2, new Vector2(0, 0.647f), new Vector2(0.3333f, 1f));
                            } else {
                                ImGui.Image(texture.ImGuiHandle, size * 2, new Vector2(0, 0), new Vector2(0.3333f, 0.3529f), shade.Value);
                            }

                            ImGui.SetCursorPos(p);
                            if (_selectedShade == shade.Key || ImGui.IsItemHovered()) {
                                ImGui.Image(texture.ImGuiHandle, size * 2, new Vector2(0.6666f, 0), new Vector2(1, 0.3529f));
                            } else {
                                ImGui.Image(texture.ImGuiHandle, size * 2, new Vector2(0.3333f, 0), new Vector2(0.6666f, 0.3529f));
                            }
                        }

                        if (ImGui.IsItemHovered()) {
                            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                        }

                        if (ImGui.IsItemClicked()) {
                            _selectedShade = shade.Key;
                        }

                        ImGui.SameLine();
                    }

                    ImGui.NewLine();
                    ImGui.Separator();

                    foreach (var s in stains.Where(s => s.Shade == _selectedShade)) {
                        if (ImGui.GetContentRegionAvail().X < size.X * 1.5f) ImGui.NewLine();
                        if (StainButton(s, size * 1.5f, true, s.RowId == stainId)) {
                            stainId = (byte) s.RowId;
                            ImGui.CloseCurrentPopup();
                            edit = true;
                        }

                        ImGui.SameLine();
                    }

                    ImGui.EndPopup();
                }
            }
        }

        return edit;
    }

    public static bool StainButton(byte stainId, Vector2 size, bool tooltip = true, bool isSelected = false) {
        var stain = DataManager.GetExcelSheet<Stain>()?.GetRow(stainId);
        return StainButton(stain, size, tooltip, isSelected);
    }

    public static bool StainButton(Stain? stain, Vector2 size, bool tooltip = true, bool isSelected = false) {
        ImGui.Dummy(size);

        var drawOffset = size / 1.5f;
        var drawOffset2 = size / 2.25f;
        var pos = ImGui.GetItemRectMin();
        var center = ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2;
        var dl = ImGui.GetWindowDrawList();
        var texture = TextureProvider.GetFromGame("ui/uld/ListColorChooser_hr1.tex").GetWrapOrEmpty();

        if (stain == null || stain.Value.RowId == 0) {
            dl.AddImage(texture.ImGuiHandle, center - drawOffset2, center + drawOffset2, new Vector2(0.8333333f, 0.3529412f), new Vector2(0.9444444f, 0.47058824f), 0x80FFFFFF);
            dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.27777f, 0.3529f), new Vector2(0.55555f, 0.64705f));
            if (ImGui.IsItemHovered()) dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.55555f, 0.3529f), new Vector2(0.83333f, 0.64705f));

            if (tooltip && ImGui.IsItemHovered()) ImGui.SetTooltip("No Dye");

            return ImGui.IsItemClicked();
        }

        var b = stain.Value.Color & 255;
        var g = (stain.Value.Color >> 8) & 255;
        var r = (stain.Value.Color >> 16) & 255;
        var stainVec4 = new Vector4(r / 255f, g / 255f, b / 255f, 1f);
        var stainColor = ImGui.GetColorU32(stainVec4);

        dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0, 0.3529f), new Vector2(0.27777f, 0.6470f), stainColor);
        if (stain.Value.Unknown1) {
            dl.PushClipRect(center - drawOffset2, center + drawOffset2);
            ImGui.ColorConvertRGBtoHSV(stainVec4.X, stainVec4.Y, stainVec4.Z, out var h, out var s, out var v);
            ImGui.ColorConvertHSVtoRGB(h, s, v - 0.5f, out var dR, out var dG, out var dB);
            ImGui.ColorConvertHSVtoRGB(h, s, v + 0.8f, out var bR, out var bG, out var bB);
            var dColor = ImGui.GetColorU32(new Vector4(dR, dG, dB, 1));
            var bColor = ImGui.GetColorU32(new Vector4(bR, bG, bB, 1));
            var tr = pos + size with { Y = 0 };
            var bl = pos + size with { X = 0 };
            var opacity = 0U;
            for (var x = 3; x < size.X; x++) {
                if (opacity < 0xF0_00_00_00U) opacity += 0x08_00_00_00U;
                dl.AddLine(tr + new Vector2(0, x), bl + new Vector2(x, 0), opacity | (0x00A0A0A0 & dColor), 2);
                dl.AddLine(tr - new Vector2(0, x), bl - new Vector2(x, 0), opacity | (0x00FFFFFF & bColor), 2);
            }

            dl.PopClipRect();
        }

        dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.27777f, 0.3529f), new Vector2(0.55555f, 0.64705f));
        if (isSelected || ImGui.IsItemHovered()) dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.55555f, 0.3529f), new Vector2(0.83333f, 0.64705f));

        if (tooltip && ImGui.IsItemHovered()) ImGui.SetTooltip(stain.Value.Name.ExtractText());

        return ImGui.IsItemClicked();
    }
}
