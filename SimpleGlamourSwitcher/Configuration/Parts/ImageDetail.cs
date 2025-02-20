using System.Drawing;
using System.Numerics;
using Dalamud.Interface.Textures.TextureWraps;
using ImGuiNET;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

namespace SimpleGlamourSwitcher.Configuration.Parts;


[Flags]
public enum RectangleHandle : byte
{
    None = 0,
    
    Left = 1,
    Right = 2,
    Top = 4,
    Bottom = 8,
    
    TopLeft =  Top | Left,
    TopRight = Top | Right,
    BottomLeft = Bottom | Left,
    BottomRight  = Bottom | Right,
    
    All = Left | Right | Top | Bottom,
}


public record ImageDetail {
    
    public static readonly ImageDetail Default = new();
    

    
    
    public Vector2 UvMin = Vector2.Zero;
    public Vector2 UvMax = Vector2.One;
    
    
    [JsonIgnore] public Vector2 UvSize => UvMax - UvMin;
    [JsonIgnore] public float UvRatio => UvSize.X == 0 ? float.NaN : UvSize.Y / UvSize.X;
    
    [JsonIgnore] public float UvHeightToWidthRatio => UvSize.Y == 0 ? float.NaN : UvSize.X / UvSize.Y;
    
    
    public bool MoveUv(Vector2 delta, bool clamp = true) {
        var oldUvMin = UvMin;
        var oldUvMax = UvMax;
        
        UvMin += delta;
        UvMax += delta;

        if (clamp) ClampUv();


        return Vector2.Distance(oldUvMin, UvMin) > 0 || Vector2.Distance(oldUvMax, UvMax) > 0;
    }

    public bool NormalizeUv() {
        var edited = false;
        
        if (UvMin.X > UvMax.X) {
            (UvMax.X, UvMin.X) = (UvMin.X, UvMax.X); // Swap Min and Max X
            edited = true;
        }
        
        if (UvMin.Y > UvMax.Y) {
            (UvMax.Y, UvMin.Y) = (UvMin.Y, UvMax.Y); // Swap Min and Max Y
            edited = true;
        }

        return edited;
    }
    
    public bool ClampUv() {
        var edited = NormalizeUv();
        
        if (UvMin.X < 0 || UvMin.Y < 0) {
            var offset = new Vector2(Math.Min(UvMin.X, 0), Math.Min(UvMin.Y, 0));
            UvMax -= offset;
            UvMin -= offset;
            edited = true;
        }

        if (UvMax.X > 1 || UvMax.Y > 1) {
            var offset = new Vector2(Math.Max(UvMax.X - 1, 0), Math.Max(UvMax.Y - 1, 0));
            UvMin -= offset;
            UvMax -= offset;
            edited = true;
        }
        
        return edited;
    }
    
    
    public bool MoveHandle(ref RectangleHandle handle, Vector2 delta, bool maintainAspectRatio = false) {
        var aspectRatio = UvRatio;
        if (float.IsNaN(aspectRatio) || aspectRatio == 0) maintainAspectRatio = false;
        
        var newUvMin = UvMin;
        var newUvMax = UvMax;


        var h = handle;
        
        
        if (h.HasFlag(RectangleHandle.Left)) {
            newUvMin.X += delta.X;

            if (newUvMin.X > newUvMax.X) {
                handle &= ~RectangleHandle.Left;
                handle |= RectangleHandle.Right;
            }
            
            
        }

        if (h.HasFlag(RectangleHandle.Right)) {
            newUvMax.X += delta.X;
            
            if (newUvMin.X > newUvMax.X) {
                handle &= ~RectangleHandle.Right;
                handle |= RectangleHandle.Left;
            }
            
        }

        if (h.HasFlag(RectangleHandle.Top)) {
            newUvMin.Y += delta.Y;
            
            if (newUvMin.Y > newUvMax.Y) {
                handle &= ~RectangleHandle.Top;
                handle |= RectangleHandle.Bottom;
            }
            
        }

        if (h.HasFlag(RectangleHandle.Bottom)) {
            newUvMax.Y += delta.Y;
            
            if (newUvMin.Y > newUvMax.Y) {
                handle &= ~RectangleHandle.Bottom;
                handle |= RectangleHandle.Top;
            }
            
        }
        
        
        
        newUvMin = Vector2.Clamp(newUvMin, Vector2.Zero, Vector2.One);
        newUvMax = Vector2.Clamp(newUvMax, Vector2.Zero, Vector2.One);

        if (Vector2.Distance(newUvMin, UvMin) > 0 || Vector2.Distance(newUvMax, UvMax) > 0) {
            UvMax = newUvMax;
            UvMin = newUvMin;
            return true;
        }

        return false;



    }

    public static ImageDetail CropFor(IDalamudTextureWrap? clipImage, PolaroidStyle outfitStyle) {
        if (clipImage == null) return Default;

        if (clipImage.Width == 0 || clipImage.Height == 0) return Default;
        if (outfitStyle.ImageSize.X <= 0 || outfitStyle.ImageSize.Y <= 0) return Default;
        

        if (outfitStyle.ImageSize.X > outfitStyle.ImageSize.Y) {
            // Wide
            
            var ratio = clipImage.Size.X / clipImage.Size.Y;
            
            return new ImageDetail() {
                UvMin = new Vector2(0, ratio / 2f),
                UvMax = new Vector2(1, 1 - ratio / 2f),
            };
            
            
        } else if (outfitStyle.ImageSize.Y > outfitStyle.ImageSize.X) {
            var ratio = clipImage.Size.X / clipImage.Size.Y;
            var ratio2 = outfitStyle.ImageSize.X / outfitStyle.ImageSize.Y;

            var ratioDiff = ratio2 - ratio;
            
            
            ImGui.Text($"{ratio} / {ratio2} / {ratioDiff}");

            if (ratioDiff < 0) {
                return new ImageDetail() {
                    UvMin = new Vector2(-ratioDiff / 2f, 0),
                    UvMax = new Vector2(1 + ratioDiff / 2f, 1),
                };
            } else {
                return Default;
            }
            
            // Tall
        }
        
        return Default;

    }
}
