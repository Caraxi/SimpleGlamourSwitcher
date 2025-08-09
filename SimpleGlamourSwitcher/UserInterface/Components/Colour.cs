using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public class Colour {
    public uint U32 { get; set; }
    public Vector4 Float4 {
        get => ImGui.ColorConvertU32ToFloat4(U32);
        set => U32 = ImGui.ColorConvertFloat4ToU32(value);
    }
    
    public Colour(uint u32) => U32 = u32;
    public Colour(Vector4 float4) => Float4 = float4;
    
    public static implicit operator Vector4(Colour c) => c.Float4;
    public static implicit operator uint(Colour c) => c.U32;
    public static implicit operator Colour(uint c) => new(c);
    public static implicit operator Colour(Vector4 c) => new(c);
}
