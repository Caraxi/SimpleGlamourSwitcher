using Penumbra.GameData.Enums;
// ReSharper disable UnassignedField.Global

namespace SimpleGlamourSwitcher.IPC.Glamourer;

public class GlamourerCustomize {
    public uint ModelId;

    public GlamourerCustomizeOption Race = new();
    public GlamourerCustomizeOption Gender = new();
    public GlamourerCustomizeOption? BodyType;
    public GlamourerCustomizeOption? Height;
    public GlamourerCustomizeOption Clan = new();
    public GlamourerCustomizeOption? Face;
    public GlamourerCustomizeOption Hairstyle = new();
    public GlamourerCustomizeOption? Highlights;
    public GlamourerCustomizeOption? SkinColor;
    public GlamourerCustomizeOption? EyeColorRight;
    public GlamourerCustomizeOption? HairColor;
    public GlamourerCustomizeOption? HighlightsColor;
    public GlamourerCustomizeOption? FacialFeature1;
    public GlamourerCustomizeOption? FacialFeature2;
    public GlamourerCustomizeOption? FacialFeature3;
    public GlamourerCustomizeOption? FacialFeature4;
    public GlamourerCustomizeOption? FacialFeature5;
    public GlamourerCustomizeOption? FacialFeature6;
    public GlamourerCustomizeOption? FacialFeature7;
    public GlamourerCustomizeOption? LegacyTattoo;
    public GlamourerCustomizeOption? TattooColor;
    public GlamourerCustomizeOption? Eyebrows;
    public GlamourerCustomizeOption? EyeColorLeft;
    public GlamourerCustomizeOption? EyeShape;
    public GlamourerCustomizeOption? SmallIris;
    public GlamourerCustomizeOption? Nose;
    public GlamourerCustomizeOption? Jaw;
    public GlamourerCustomizeOption? Mouth;
    public GlamourerCustomizeOption? Lipstick;
    public GlamourerCustomizeOption? LipColor;
    public GlamourerCustomizeOption? MuscleMass;
    public GlamourerCustomizeOption? TailShape;
    public GlamourerCustomizeOption? BustSize;
    public GlamourerCustomizeOption? FacePaint;
    public GlamourerCustomizeOption? FacePaintReversed;
    public GlamourerCustomizeOption? FacePaintColor;
    
    public GlamourerCustomizeOption? this[CustomizeIndex index] =>
        index switch {
            CustomizeIndex.Race => Race,
            CustomizeIndex.Gender => Gender,
            CustomizeIndex.BodyType => BodyType,
            CustomizeIndex.Height => Height,
            CustomizeIndex.Clan => Clan,
            CustomizeIndex.Face => Face,
            CustomizeIndex.Hairstyle => Hairstyle,
            CustomizeIndex.Highlights => Highlights,
            CustomizeIndex.SkinColor => SkinColor,
            CustomizeIndex.EyeColorRight => EyeColorRight,
            CustomizeIndex.HairColor => HairColor,
            CustomizeIndex.HighlightsColor => HighlightsColor,
            CustomizeIndex.FacialFeature1 => FacialFeature1,
            CustomizeIndex.FacialFeature2 => FacialFeature2,
            CustomizeIndex.FacialFeature3 => FacialFeature3,
            CustomizeIndex.FacialFeature4 => FacialFeature4,
            CustomizeIndex.FacialFeature5 => FacialFeature5,
            CustomizeIndex.FacialFeature6 => FacialFeature6,
            CustomizeIndex.FacialFeature7 => FacialFeature7,
            CustomizeIndex.LegacyTattoo => LegacyTattoo,
            CustomizeIndex.TattooColor => TattooColor,
            CustomizeIndex.Eyebrows => Eyebrows,
            CustomizeIndex.EyeColorLeft => EyeColorLeft,
            CustomizeIndex.EyeShape => EyeShape,
            CustomizeIndex.SmallIris => SmallIris,
            CustomizeIndex.Nose => Nose,
            CustomizeIndex.Jaw => Jaw,
            CustomizeIndex.Mouth => Mouth,
            CustomizeIndex.Lipstick => Lipstick,
            CustomizeIndex.LipColor => LipColor,
            CustomizeIndex.MuscleMass => MuscleMass,
            CustomizeIndex.TailShape => TailShape,
            CustomizeIndex.BustSize => BustSize,
            CustomizeIndex.FacePaint => FacePaint,
            CustomizeIndex.FacePaintReversed => FacePaintReversed,
            CustomizeIndex.FacePaintColor => FacePaintColor,
            _ => null
        };
}
