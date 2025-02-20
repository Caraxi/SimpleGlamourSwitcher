using Dalamud.Interface.Colors;
using ImGuiNET;

namespace SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

public record TextInputStyle : StyleProvider<TextInputStyle> {
    public ShadowTextStyle ErrorMessage = new() { TextColour = ImGuiColors.DalamudRed };
    public ShadowTextStyle Label = new();
}

public record ComboStyle : StyleProvider<ComboStyle> {
    public ShadowTextStyle ErrorMessage = new() { TextColour = ImGuiColors.DalamudRed };
    public ShadowTextStyle Label = new();
    public Colour PreviewColour = ImGui.GetColorU32(ImGuiCol.Text);
    public float PopoutHeight = 400;
}