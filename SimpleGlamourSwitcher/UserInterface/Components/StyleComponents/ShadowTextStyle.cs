using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

public record ShadowTextStyle : StyleProvider<ShadowTextStyle> {

    public Colour TextColour = ImGui.GetColorU32(ImGuiCol.Text);
    public Colour ShadowColour = 0x80000000;

    public float Size = 2;
    public Vector2 Offset = new(0);



}
