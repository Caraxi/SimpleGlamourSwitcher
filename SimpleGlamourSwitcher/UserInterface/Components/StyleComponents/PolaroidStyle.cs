using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

public record PolaroidStyle : StyleProvider<PolaroidStyle> {
    public Colour BlankImageColour = ImGui.GetColorU32(ImGuiCol.TextDisabled);
    public Colour FrameColour = ImGuiColors.DalamudWhite;
    public Colour FrameHoveredColour = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
    public Colour FrameActiveColour = ImGui.GetColorU32(ImGuiCol.ButtonActive);

    
    public Colour LabelColour = ImGuiColors.DalamudWhite;
    public Colour LabelShadowColour = new Vector4(0, 0, 0, 0.75f);
    
    
    public float FrameRounding = 0f;
    public Vector2 ImageSize = new(240, 240);
    public Vector2 FramePadding = ImGui.GetStyle().FramePadding;
    public Vector2 LabelShadowOffset = new(1, 1);
    public int LabelShadowSize = 2;


    public PolaroidStyle FitTo(Vector2 fitSize) {
        var size = fitSize - (FramePadding * 2 + FramePadding * Vector2.UnitY + new Vector2(0, ImGui.GetTextLineHeightWithSpacing()));
        if (size.X < 0 || size.Y < 0) return this;
        return this with { ImageSize = ImageSize.FitTo(size) };
    }



    [Flags]
    public enum PolaroidStyleEditorFlags : uint {
        None = 0,
        ImageSize = 1U << 0,
        FramePadding = 1U << 1,
        FrameRounding = 1U << 2,
        TextColour = 1U << 3,
        FrameColour = 1U << 4,
        ActiveFrameColours = 1U << 5,
        ShowPreview  = 1U << 31,
        All = uint.MaxValue
    }
    
    
    public static bool DrawEditor(string header, PolaroidStyle style, PolaroidStyleEditorFlags flags = PolaroidStyleEditorFlags.All) {
        var edited = false;

        if (flags.HasFlag(PolaroidStyleEditorFlags.ImageSize)) {
            ImGui.TextDisabled("Note: Adjusting the image size will cause existing images to stretch to fit the new size.");
        }
        
        using (ImRaii.PushIndent()) {
            ImRaii.IEndObject? group = null;
            
            if (flags.HasFlag(PolaroidStyleEditorFlags.ShowPreview)) {
                if (flags.HasFlag(PolaroidStyleEditorFlags.ActiveFrameColours)) {
                    ImGui.Dummy(Polaroid.GetActualSize(style));
                    Polaroid.DrawPolaroid(null, ImageDetail.Default, $"{header} Preview", style with {
                        FrameColour = ImGui.IsItemHovered() ? ImGui.IsMouseDown(ImGuiMouseButton.Left) ? style.FrameActiveColour : style.FrameHoveredColour : style.FrameColour
                    });
                } else {
                    Polaroid.DrawPolaroid(null, ImageDetail.Default, $"{header} Preview", style);
                }
                
                ImGui.SameLine();

                if (ImGui.GetContentRegionAvail().X > 300 * ImGuiHelpers.GlobalScale) {
                    group = ImRaii.Group();
                } else {
                    ImGui.NewLine();
                }
            }
            
            if (flags.HasFlag(PolaroidStyleEditorFlags.ImageSize)) {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                edited |= ImGui.DragFloat2($"Image Size##{header}", ref style.ImageSize, 1, 0, float.MaxValue, "%.0f", ImGuiSliderFlags.AlwaysClamp);
            }
            
            if (flags.HasFlag(PolaroidStyleEditorFlags.FramePadding)) {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                edited |= ImGui.DragFloat2($"Frame Padding##{header}", ref style.FramePadding, 1, 0, float.MaxValue, "%.0f", ImGuiSliderFlags.AlwaysClamp);
            }
           
            if (flags.HasFlag(PolaroidStyleEditorFlags.FrameRounding)) {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                edited |= ImGui.DragFloat($"Frame Rounding##{header}", ref style.FrameRounding, 1, 0, float.MaxValue, "%.0f", ImGuiSliderFlags.AlwaysClamp);
            }

            if (flags.HasFlag(PolaroidStyleEditorFlags.TextColour)) {
                edited |= ColourEdit4($"Text Colour##{header}", ref style.LabelColour);
                edited |= ColourEdit4($"Text Shadow Colour##{header}", ref style.LabelShadowColour);
                using (ImRaii.PushIndent()) {
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                    edited |= ImGui.DragFloat2($"Shadow Offset##{header}", ref style.LabelShadowOffset, 1, -30, 30, "%.0f", ImGuiSliderFlags.AlwaysClamp);
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.7f);
                    edited |= ImGui.SliderInt($"Shadow Size##{header}", ref style.LabelShadowSize, 0, 5, "%.0f", ImGuiSliderFlags.AlwaysClamp);
                }
            }
            
            if (flags.HasFlag(PolaroidStyleEditorFlags.FrameColour)) {
                edited |= ColourEdit4($"Frame Colour##{header}", ref style.FrameColour);
                edited |= ColourEdit4($"Image Area Colour##{header}", ref style.BlankImageColour);
            }

            if (flags.HasFlag(PolaroidStyleEditorFlags.ActiveFrameColours)) {
                edited |= ColourEdit4($"Active Frame Colour##{header}", ref style.FrameActiveColour);
                edited |= ColourEdit4($"Hovered Frame Colour##{header}", ref style.FrameHoveredColour);
            }
            
            group?.Dispose();
        }

        return edited;
    }

    private static bool ColourEdit4(string name, ref Colour colour) {
        var c = colour.Float4;
        
        if (ImGui.ColorEdit4(name, ref c, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoDragDrop | ImGuiColorEditFlags.NoTooltip)) {
            colour.Float4 =  c;
            return true;
        }

        return false;
    }
    
    
    
}