using System.Collections;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.Service;

namespace SimpleGlamourSwitcher.Configuration.Parts;

[JsonObject]
public record OutfitAppearance : Applicable, IEnumerable<(string, Applicable)> {
    
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

    public ApplicableParameterFloat FacePaintUvMultiplier = new();
    public ApplicableParameterFloat FacePaintUvOffset = new();
    public ApplicableParameterPercent MuscleTone = new();
    public ApplicableParameterPercent LeftLimbalIntensity = new();
    public ApplicableParameterPercent RightLimbalIntensity = new();
    public ApplicableParameterColor SkinDiffuse = new();
    public ApplicableParameterColor HairDiffuse = new();
    public ApplicableParameterColor HairHighlight = new();
    public ApplicableParameterColor LeftEye = new();
    public ApplicableParameterColor RightEye = new();
    public ApplicableParameterColor FeatureColor = new();
    public ApplicableParameterColorAlpha LipDiffuse = new();
    public ApplicableParameterColorAlpha DecalColor = new();

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

    public ApplicableParameter this[AppearanceParameterKind kind] {
        get {
            return kind switch {
                AppearanceParameterKind.FacePaintUvMultiplier => FacePaintUvMultiplier,
                AppearanceParameterKind.FacePaintUvOffset => FacePaintUvOffset,
                AppearanceParameterKind.MuscleTone => MuscleTone,
                AppearanceParameterKind.LeftLimbalIntensity => LeftLimbalIntensity,
                AppearanceParameterKind.RightLimbalIntensity => RightLimbalIntensity,
                AppearanceParameterKind.SkinDiffuse => SkinDiffuse,
                AppearanceParameterKind.HairDiffuse => HairDiffuse,
                AppearanceParameterKind.HairHighlight => HairHighlight,
                AppearanceParameterKind.LeftEye => LeftEye,
                AppearanceParameterKind.RightEye => RightEye,
                AppearanceParameterKind.FeatureColor => FeatureColor,
                AppearanceParameterKind.LipDiffuse => LipDiffuse,
                AppearanceParameterKind.DecalColor => DecalColor,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Invalid parameter")
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
    }

    public static OutfitAppearance FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, GlamourerState glamourerState, Guid penumbraCollectionId) {
        var customize = glamourerState.Customize;
        var parameter = glamourerState.Parameters;

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
            
            FacePaintUvMultiplier = ApplicableParameterFloat.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.FacePaintUvMultiplier, parameter.FacePaintUvMultiplier),
            FacePaintUvOffset = ApplicableParameterFloat.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.FacePaintUvOffset, parameter.FacePaintUvOffset),
            MuscleTone = ApplicableParameterPercent.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.MuscleTone, parameter.MuscleTone),
            LeftLimbalIntensity = ApplicableParameterPercent.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.LeftLimbalIntensity, parameter.LeftLimbalIntensity),
            RightLimbalIntensity = ApplicableParameterPercent.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.RightLimbalIntensity, parameter.RightLimbalIntensity),
            SkinDiffuse = ApplicableParameterColor.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.SkinDiffuse, parameter.SkinDiffuse),
            HairDiffuse = ApplicableParameterColor.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.HairDiffuse, parameter.HairDiffuse),
            HairHighlight = ApplicableParameterColor.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.HairHighlight, parameter.HairHighlight),
            LeftEye = ApplicableParameterColor.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.LeftEye, parameter.LeftEye),
            RightEye = ApplicableParameterColor.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.RightEye, parameter.RightEye),
            FeatureColor = ApplicableParameterColor.FromExistingState(defaultOptionsProvider, AppearanceParameterKind.FeatureColor, parameter.FeatureColor),
            LipDiffuse = ApplicableParameterColorAlpha.FromExistingStateAlpha(defaultOptionsProvider, AppearanceParameterKind.LipDiffuse, parameter.LipDiffuse),
            DecalColor = ApplicableParameterColorAlpha.FromExistingStateAlpha(defaultOptionsProvider, AppearanceParameterKind.DecalColor, parameter.DecalColor),
        };
    }

    public IEnumerator<(string, Applicable)> GetEnumerator() {
        yield return ("Race", Race);
        yield return ("Gender", Gender);
        yield return ("BodyType", BodyType);
        yield return ("Height", Height);
        yield return ("Clan", Clan);
        yield return ("Face", Face);
        yield return ("Hairstyle", Hairstyle);
        yield return ("Highlights", Highlights);
        yield return ("SkinColor", SkinColor);
        yield return ("EyeColorRight", EyeColorRight);
        yield return ("HairColor", HairColor);
        yield return ("HighlightsColor", HighlightsColor);
        yield return ("FacialFeature1", FacialFeature1);
        yield return ("FacialFeature2", FacialFeature2);
        yield return ("FacialFeature3", FacialFeature3);
        yield return ("FacialFeature4", FacialFeature4);
        yield return ("FacialFeature5", FacialFeature5);
        yield return ("FacialFeature6", FacialFeature6);
        yield return ("FacialFeature7", FacialFeature7);
        yield return ("LegacyTattoo", LegacyTattoo);
        yield return ("TattooColor", TattooColor);
        yield return ("Eyebrows", Eyebrows);
        yield return ("EyeColorLeft", EyeColorLeft);
        yield return ("EyeShape", EyeShape);
        yield return ("SmallIris", SmallIris);
        yield return ("Nose", Nose);
        yield return ("Jaw", Jaw);
        yield return ("Mouth", Mouth);
        yield return ("Lipstick", Lipstick);
        yield return ("LipColor", LipColor);
        yield return ("MuscleMass", MuscleMass);
        yield return ("TailShape", TailShape);
        yield return ("BustSize", BustSize);
        yield return ("FacePaint", FacePaint);
        yield return ("FacePaintReversed", FacePaintReversed);
        yield return ("FacePaintColor", FacePaintColor);
        yield return ("FacePaintUvMultiplier", FacePaintUvMultiplier);
        yield return ("FacePaintUvOffset", FacePaintUvOffset);
        yield return ("MuscleTone", MuscleTone);
        yield return ("LeftLimbalIntensity", LeftLimbalIntensity);
        yield return ("RightLimbalIntensity", RightLimbalIntensity);
        yield return ("SkinDiffuse", SkinDiffuse);
        yield return ("HairDiffuse", HairDiffuse);
        yield return ("HairHighlight", HairHighlight);
        yield return ("LeftEye", LeftEye);
        yield return ("RightEye", RightEye);
        yield return ("FeatureColor", FeatureColor);
        yield return ("LipDiffuse", LipDiffuse);
        yield return ("DecalColor", DecalColor);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
