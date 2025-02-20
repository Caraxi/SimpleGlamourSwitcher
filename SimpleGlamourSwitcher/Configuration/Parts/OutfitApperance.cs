using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;

namespace SimpleGlamourSwitcher.Configuration.Parts;

public record OutfitAppearance : Applicable {
    
    public ApplicableCustomize Race = new();
    public ApplicableCustomize Gender = new();
    public ApplicableCustomize BodyType = new();
    public ApplicableCustomize Height = new();
    public ApplicableCustomize Clan = new();
    public ApplicableCustomize Face = new();
    public ApplicableCustomizeModable Hairstyle = new();
    public ApplicableCustomize Highlights = new();
    public ApplicableCustomize SkinColor = new();
    public ApplicableCustomize EyeColorRight = new();
    public ApplicableCustomize HairColor = new();
    public ApplicableCustomize HighlightsColor = new();
    public ApplicableCustomize FacialFeature1 = new();
    public ApplicableCustomize FacialFeature2 = new();
    public ApplicableCustomize FacialFeature3 = new();
    public ApplicableCustomize FacialFeature4 = new();
    public ApplicableCustomize FacialFeature5 = new();
    public ApplicableCustomize FacialFeature6 = new();
    public ApplicableCustomize FacialFeature7 = new();
    public ApplicableCustomize LegacyTattoo = new();
    public ApplicableCustomize TattooColor = new();
    public ApplicableCustomize Eyebrows = new();
    public ApplicableCustomize EyeColorLeft = new();
    public ApplicableCustomize EyeShape = new();
    public ApplicableCustomize SmallIris = new();
    public ApplicableCustomize Nose = new();
    public ApplicableCustomize Jaw = new();
    public ApplicableCustomize Mouth = new();
    public ApplicableCustomize Lipstick = new();
    public ApplicableCustomize LipColor = new();
    public ApplicableCustomize MuscleMass = new();
    public ApplicableCustomize TailShape = new();
    public ApplicableCustomize BustSize = new();
    public ApplicableCustomize FacePaint = new();
    public ApplicableCustomize FacePaintReversed = new();
    public ApplicableCustomize FacePaintColor = new();

    public ApplicableCustomize this[CustomizeIndex index] {
        get {
            return index switch {
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
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, @"Invalid index."),
            };
        }
    }
    
    
    public override void ApplyToCharacter(ref bool requestRedraw) {
        
        if (!Apply) return;
        Notice.Show("Applying Customization Values");
        
        foreach (var v in System.Enum.GetValues<CustomizeIndex>()) {
            var customization = this[v];
            if (customization is ApplicableCustomizeModable acm) {
                acm.ApplyToCharacter(v, ref requestRedraw);
            }
        }
        
        GlamourerIpc.ApplyCustomization.Invoke(this);
    }

    public static OutfitAppearance FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, GlamourerState glamourerState, Guid penumbraCollectionId) {
        var customize = glamourerState.Customize;

        return new OutfitAppearance {
            Apply = defaultOptionsProvider.DefaultEnabledCustomizeIndexes.Count > 0,
            Race = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Race, customize.Race),
            Gender = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Gender, customize.Gender),
            BodyType = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.BodyType, customize.BodyType),
            Height = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Height, customize.Height),
            Clan = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Clan, customize.Clan),
            Face = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Face, customize.Face),
            Hairstyle = ApplicableCustomizeModable.FromExistingState(CustomizeIndex.Hairstyle, customize, penumbraCollectionId, defaultOptionsProvider),
            Highlights = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Highlights, customize.Highlights),
            SkinColor = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.SkinColor, customize.SkinColor),
            EyeColorRight = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.EyeColorRight, customize.EyeColorRight),
            HairColor = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.HairColor, customize.HairColor),
            HighlightsColor = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.HighlightsColor, customize.HighlightsColor),
            FacialFeature1 = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacialFeature1, customize.FacialFeature1),
            FacialFeature2 = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacialFeature2, customize.FacialFeature2),
            FacialFeature3 = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacialFeature3, customize.FacialFeature3),
            FacialFeature4 = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacialFeature4, customize.FacialFeature4),
            FacialFeature5 = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacialFeature5, customize.FacialFeature5),
            FacialFeature6 = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacialFeature6, customize.FacialFeature6),
            FacialFeature7 = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacialFeature7, customize.FacialFeature7),
            LegacyTattoo = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.LegacyTattoo, customize.LegacyTattoo),
            TattooColor = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.TattooColor, customize.TattooColor),
            Eyebrows = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Eyebrows, customize.Eyebrows),
            EyeColorLeft = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.EyeColorLeft, customize.EyeColorLeft),
            EyeShape = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.EyeShape, customize.EyeShape),
            SmallIris = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.SmallIris, customize.SmallIris),
            Nose = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Nose, customize.Nose),
            Jaw = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Jaw, customize.Jaw),
            Mouth = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Mouth, customize.Mouth),
            Lipstick = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.Lipstick, customize.Lipstick),
            LipColor = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.LipColor, customize.LipColor),
            MuscleMass = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.MuscleMass, customize.MuscleMass),
            TailShape = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.TailShape, customize.TailShape),
            BustSize = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.BustSize, customize.BustSize),
            FacePaint = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacePaint, customize.FacePaint),
            FacePaintReversed = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacePaintReversed, customize.FacePaintReversed),
            FacePaintColor = ApplicableCustomize.FromExistingState(defaultOptionsProvider, CustomizeIndex.FacePaintColor, customize.FacePaintColor),
        };
    }
    
}
