﻿using System.Numerics;
using ImGuiNET;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public record Style {
    public static Style Default { get; } = new();
    
    public Colour Text = ImGui.GetColorU32(ImGuiCol.Text);
    public Colour TextShadow = 0x80000000;

    public PolaroidStyle CharacterPolaroid = new() { ImageSize = PolaroidStyle.Default.ImageSize * new Vector2(0.5f, 1f) };
    public PolaroidStyle FolderPolaroid = new() { ImageSize = GetDefaultFolderSize() };
    
    public OutfitListStyle OutfitList = new();
    
    public ShadowTextStyle ShadowTextText = new();
    public TextInputStyle TextInput = new();
    public ComboStyle Combo = new();




    private static Vector2 GetDefaultFolderSize() {
        var s = PolaroidStyle.Default.ImageSize;
        var imageWidth = (s.X - ImGui.GetStyle().ItemSpacing.X * 2) / 2f;
        var imageHeight = s.Y / s.X * imageWidth;
        return new Vector2(imageWidth, imageHeight);
    }
}
