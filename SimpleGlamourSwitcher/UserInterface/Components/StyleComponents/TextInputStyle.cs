using System.Numerics;
using Dalamud.Interface.Colors;
using ImGuiNET;

namespace SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

public record TextInputStyle : StyleProvider<TextInputStyle> {
    public ShadowTextStyle ErrorMessage = new() { TextColour = ImGuiColors.DalamudRed };
    public ShadowTextStyle Label = new();

    public Colour BorderColour = ImGui.GetColorU32(ImGuiCol.Border);
    public float BorderSize = 3;
    public Vector2 FramePadding = new Vector2(32, 16);


    public bool PadTop = true;
}

public record ComboStyle : StyleProvider<ComboStyle> {
    public ShadowTextStyle ErrorMessage = new() { TextColour = ImGuiColors.DalamudRed };
    public ShadowTextStyle Label = new();
    public Colour PreviewColour = ImGui.GetColorU32(ImGuiCol.Text);
    public float PopoutHeight = 400;
    public bool PadTop = true;
    public float BorderSize = 3;
    public Vector2 FramePadding = new Vector2(32, 16);
}