using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

public record ShadowTextStyle : StyleProvider<ShadowTextStyle>
{
    public Colour TextColour = 0xFFFFFFFF;
    public Colour ShadowColour = 0x80000000;
    public float Size = 1;
    public Vector2 Offset = new(0);
}
