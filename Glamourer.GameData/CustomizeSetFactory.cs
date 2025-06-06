﻿using Dalamud.Game;
using Dalamud.Plugin.Services;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using OtterGui.Classes;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Structs;
using Race = Penumbra.GameData.Enums.Race;

namespace Glamourer.GameData;

internal class CustomizeSetFactory(
    IDataManager _gameData,
    IPluginLog _log,
    TextureCache _icons,
    NpcCustomizeSet _npcCustomizeSet,
    ColorParameters _colors)
{
    public CustomizeSetFactory(IDataManager gameData, IPluginLog log, TextureCache icons, NpcCustomizeSet npcCustomizeSet)
        : this(gameData, log, icons, npcCustomizeSet, new ColorParameters(gameData, log))
    { }

    /// <summary> Create the set of all available customization options for a given clan and gender. </summary>
    public CustomizeSet CreateSet(SubRace race, Gender gender)
    {
        var (skin, hair) = GetSkinHairColors(race, gender);
        var row      = _charaMakeSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender)!;
        var hrothgar = race.ToRace() == Race.Hrothgar;
        // Create the initial set with all the easily accessible parameters available for anyone.
        var set = new CustomizeSet(_npcCustomizeSet, race, gender)
        {
            Name                 = GetName(race, gender),
            Voices               = row.VoiceStruct,
            HairStyles           = GetHairStyles(race, gender),
            HairColors           = hair,
            SkinColors           = skin,
            EyeColors            = _eyeColorPicker,
            HighlightColors      = _highlightPicker,
            TattooColors         = _tattooColorPicker,
            LipColorsDark        = hrothgar ? HrothgarFurPattern(row) : _lipColorPickerDark,
            LipColorsLight       = hrothgar ? [] : _lipColorPickerLight,
            FacePaintColorsDark  = _facePaintColorPickerDark,
            FacePaintColorsLight = _facePaintColorPickerLight,
            Faces                = GetFaces(row),
            NumEyebrows          = GetListSize(row, CustomizeIndex.Eyebrows),
            NumEyeShapes         = GetListSize(row, CustomizeIndex.EyeShape),
            NumNoseShapes        = GetListSize(row, CustomizeIndex.Nose),
            NumJawShapes         = GetListSize(row, CustomizeIndex.Jaw),
            NumMouthShapes       = GetListSize(row, CustomizeIndex.Mouth),
            FacePaints           = GetFacePaints(race, gender),
            TailEarShapes        = GetTailEarShapes(row),
            OptionName           = GetOptionNames(row),
            Types                = GetMenuTypes(row),
        };
        SetPostProcessing(set, row);
        return set;
    }

    /// <summary> Some data can not be set independently of the rest, so we need a post-processing step to finalize. </summary>
    private void SetPostProcessing(CustomizeSet set, in CharaMakeType row)
    {
        SetAvailability(set, row);
        SetFacialFeatures(set, row);
        SetHairByFace(set);
        SetNpcData(set, set.Clan, set.Gender);
        SetOrder(set);
    }

    /// <summary> Given a customize set with filled data, find all customizations used by valid NPCs that are not regularly available. </summary>
    private void SetNpcData(CustomizeSet set, SubRace race, Gender gender)
    {
        var customizeIndices = new[]
        {
            CustomizeIndex.Face,
            CustomizeIndex.Hairstyle,
            CustomizeIndex.LipColor,
            CustomizeIndex.SkinColor,
            CustomizeIndex.TailShape,
        };

        var npcCustomizations = new HashSet<(CustomizeIndex, CustomizeValue)>()
        {
            (CustomizeIndex.Height, CustomizeValue.Max),
        };
        _npcCustomizeSet.Awaiter.Wait();
        foreach (var customize in _npcCustomizeSet.Select(s => s.Customize)
                     .Where(c => c.Clan == race && c.Gender == gender && c.BodyType.Value == 1))
        {
            foreach (var customizeIndex in customizeIndices)
            {
                var value = customize[customizeIndex];
                if (value == CustomizeValue.Zero)
                    continue;

                if (set.DataByValue(customizeIndex, value, out _, customize.Face) >= 0)
                    continue;

                npcCustomizations.Add((customizeIndex, value));
            }
        }

        set.NpcOptions = npcCustomizations.OrderBy(p => p.Item1).ThenBy(p => p.Item2.Value).ToArray();
    }

    private readonly ColorParameters                _colorParameters = new(_gameData, _log);
    private readonly ExcelSheet<CharaMakeCustomize> _customizeSheet  = _gameData.GetExcelSheet<CharaMakeCustomize>(ClientLanguage.English);
    private readonly ExcelSheet<Lobby>              _lobbySheet      = _gameData.GetExcelSheet<Lobby>(ClientLanguage.English);
    private readonly ExcelSheet<RawRow>             _hairSheet       = _gameData.GetExcelSheet<RawRow>(ClientLanguage.English, "HairMakeType");
    private readonly ExcelSheet<Tribe>              _tribeSheet      = _gameData.GetExcelSheet<Tribe>(ClientLanguage.English);

    // Those color pickers are shared between all races.
    private readonly CustomizeData[] _highlightPicker           = CreateColors(_colors, CustomizeIndex.HighlightsColor, 256,  192);
    private readonly CustomizeData[] _lipColorPickerDark        = CreateColors(_colors, CustomizeIndex.LipColor,        512,  96);
    private readonly CustomizeData[] _lipColorPickerLight       = CreateColors(_colors, CustomizeIndex.LipColor,        1024, 96, true);
    private readonly CustomizeData[] _eyeColorPicker            = CreateColors(_colors, CustomizeIndex.EyeColorLeft,    0,    192);
    private readonly CustomizeData[] _facePaintColorPickerDark  = CreateColors(_colors, CustomizeIndex.FacePaintColor,  640,  96);
    private readonly CustomizeData[] _facePaintColorPickerLight = CreateColors(_colors, CustomizeIndex.FacePaintColor,  1152, 96, true);
    private readonly CustomizeData[] _tattooColorPicker         = CreateColors(_colors, CustomizeIndex.TattooColor,     0,    192);

    private readonly ExcelSheet<CharaMakeType> _charaMakeSheet = _gameData.Excel.GetSheet<CharaMakeType>();

    /// <summary> Obtain available skin and hair colors for the given clan and gender. </summary>
    private (CustomizeData[] Skin, CustomizeData[] Hair) GetSkinHairColors(SubRace race, Gender gender)
    {
        if (race is > SubRace.Veena or SubRace.Unknown)
            throw new ArgumentOutOfRangeException(nameof(race), race, null);

        var gv  = gender == Gender.Male ? 0 : 1;
        var idx = ((int)race * 2 + gv) * 5 + 3;

        return (CreateColors(_colorParameters, CustomizeIndex.SkinColor, idx << 8,       192),
            CreateColors(_colorParameters,     CustomizeIndex.HairColor, (idx + 1) << 8, 192));
    }

    /// <summary> Obtain the gender-specific clan name. </summary>
    private string GetName(SubRace race, Gender gender)
        => gender switch
        {
            Gender.Male   => _tribeSheet.TryGetRow((uint)race, out var row) ? row.Masculine.ExtractText() : race.ToName(),
            Gender.Female => _tribeSheet.TryGetRow((uint)race, out var row) ? row.Feminine.ExtractText() : race.ToName(),
            _             => "Unknown",
        };

    /// <summary> Obtain available hairstyles via reflection from the Hair sheet for the given subrace and gender. </summary>
    private CustomizeData[] GetHairStyles(SubRace race, Gender gender)
    {
        var row = _hairSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender);
        // Unknown30 is the number of available hairstyles.
        var numHairs = row.ReadUInt8Column(30);
        var hairList = new List<CustomizeData>(numHairs);
        // Hairstyles can be found starting at Unknown66.
        for (var i = 0; i < numHairs; ++i)
        {
            // Hairs start at Unknown66.
            var customizeIdx = row.ReadUInt32Column(66 + i * 9);
            if (customizeIdx == uint.MaxValue)
                continue;

            // Hair Row from CustomizeSheet might not be set in case of unlockable hair.
            if (!_customizeSheet.TryGetRow(customizeIdx, out var hairRow))
                hairList.Add(new CustomizeData(CustomizeIndex.Hairstyle, (CustomizeValue)i, customizeIdx));
            else if (_icons.IconExists(hairRow.Icon))
                hairList.Add(new CustomizeData(CustomizeIndex.Hairstyle, (CustomizeValue)hairRow.FeatureID, hairRow.Icon,
                    (ushort)hairRow.RowId));
        }

        return [.. hairList.OrderBy(h => h.Value.Value)];
    }

    /// <summary> Specific icons for tails or ears. </summary>
    private CustomizeData[] GetTailEarShapes(CharaMakeType row)
        => ExtractValues(row, CustomizeIndex.TailShape);

    /// <summary> Specific icons for faces. </summary>
    private CustomizeData[] GetFaces(CharaMakeType row)
        => ExtractValues(row, CustomizeIndex.Face);

    /// <summary> Specific icons for Hrothgar patterns. </summary>
    private CustomizeData[] HrothgarFurPattern(CharaMakeType row)
        => ExtractValues(row, CustomizeIndex.LipColor);

    private CustomizeData[] ExtractValues(CharaMakeType row, CustomizeIndex type)
    {
        var data = row.CharaMakeStruct.FirstOrNull(m => m.Customize == type.ToByteAndMask().ByteIdx);
        return data?.SubMenuParam.Take(data.Value.SubMenuNum).Select((v, i) => FromValueAndIndex(type, v, i)).ToArray() ?? [];
    }

    /// <summary> Get face paints from the hair sheet via reflection since there are also unlockable face paints. </summary>
    private CustomizeData[] GetFacePaints(SubRace race, Gender gender)
    {
        var row       = _hairSheet.GetRow(((uint)race - 1) * 2 - 1 + (uint)gender);
        // Number of available face paints is at Unknown37.
        var numPaints  = row.ReadUInt8Column(37);
        var paintList = new List<CustomizeData>(numPaints);
        
        for (var i = 0; i < numPaints; ++i)
        {
            // Face paints start at Unknown73.
            var customizeIdx = row.ReadUInt32Column(73 + i * 9);
            if (customizeIdx == uint.MaxValue)
                continue;

            // Face paint Row from CustomizeSheet might not be set in case of unlockable face paints.
            if (_customizeSheet.TryGetRow(customizeIdx, out var paintRow))
                paintList.Add(new CustomizeData(CustomizeIndex.FacePaint, (CustomizeValue)paintRow.FeatureID, paintRow.Icon,
                    (ushort)paintRow.RowId));
            else
                paintList.Add(new CustomizeData(CustomizeIndex.FacePaint, (CustomizeValue)i, customizeIdx));
        }

        return [.. paintList.OrderBy(p => p.Value.Value)];
    }

    /// <summary> Get List sizes. </summary>
    private static int GetListSize(CharaMakeType row, CustomizeIndex index)
    {
        var gameId = index.ToByteAndMask().ByteIdx;
        var menu   = row.CharaMakeStruct.FirstOrNull(m => m.Customize == gameId);
        return menu?.SubMenuNum ?? 0;
    }

    /// <summary> Get generic Features. </summary>
    private CustomizeData FromValueAndIndex(CustomizeIndex id, uint value, int index)
        => _customizeSheet.TryGetRow(value, out var row)
            ? new CustomizeData(id, (CustomizeValue)row.FeatureID, row.Icon, (ushort)row.RowId)
            : new CustomizeData(id, (CustomizeValue)(index + 1),   value);

    /// <summary> Create generic color sets from the parameters. </summary>
    private static CustomizeData[] CreateColors(ColorParameters colorParameters, CustomizeIndex index, int offset, int num,
        bool light = false)
    {
        var ret = new CustomizeData[num];
        var idx = 0;
        foreach (var value in colorParameters.GetSlice(offset, num))
        {
            ret[idx] = new CustomizeData(index, (CustomizeValue)(light ? 128 + idx : idx), value, (ushort)(offset + idx));
            ++idx;
        }

        return ret;
    }

    /// <summary> Set the specific option names for the given set of parameters. </summary>
    private string[] GetOptionNames(CharaMakeType row)
    {
        var nameArray = Enum.GetValues<CustomizeIndex>().Select(c =>
        {
            // Find the first menu that corresponds to the Id.
            var byteId = c.ToByteAndMask().ByteIdx;
            var menu   = row.CharaMakeStruct.FirstOrNull(m => m.Customize == byteId);
            if (menu == null)
            {
                // If none exists and the id corresponds to highlights, set the Highlights name.
                if (c == CustomizeIndex.Highlights)
                    return string.Intern(_lobbySheet.TryGetRow(237, out var text) ? text.Text.ExtractText() : "Highlights");

                // Otherwise there is an error and we use the default name.
                return c.ToDefaultName();
            }

            // Otherwise all is normal, get the menu name or if it does not work the default name.
            return string.Intern(_lobbySheet.TryGetRow(menu.Value.Menu.RowId, out var textRow)
                ? textRow.Text.ExtractText()
                : c.ToDefaultName());
        }).ToArray();

        // Add names for both eye colors.
        nameArray[(int)CustomizeIndex.EyeColorLeft]      = CustomizeIndex.EyeColorLeft.ToDefaultName();
        nameArray[(int)CustomizeIndex.EyeColorRight]     = CustomizeIndex.EyeColorRight.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacialFeature1]    = CustomizeIndex.FacialFeature1.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacialFeature2]    = CustomizeIndex.FacialFeature2.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacialFeature3]    = CustomizeIndex.FacialFeature3.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacialFeature4]    = CustomizeIndex.FacialFeature4.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacialFeature5]    = CustomizeIndex.FacialFeature5.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacialFeature6]    = CustomizeIndex.FacialFeature6.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacialFeature7]    = CustomizeIndex.FacialFeature7.ToDefaultName();
        nameArray[(int)CustomizeIndex.LegacyTattoo]      = CustomizeIndex.LegacyTattoo.ToDefaultName();
        nameArray[(int)CustomizeIndex.SmallIris]         = CustomizeIndex.SmallIris.ToDefaultName();
        nameArray[(int)CustomizeIndex.Lipstick]          = CustomizeIndex.Lipstick.ToDefaultName();
        nameArray[(int)CustomizeIndex.FacePaintReversed] = CustomizeIndex.FacePaintReversed.ToDefaultName();
        return nameArray;
    }

    /// <summary> Get the manu types for all available options. </summary>
    private static MenuType[] GetMenuTypes(CharaMakeType row)
    {
        // Set up the menu types for all customizations.
        return Enum.GetValues<CustomizeIndex>().Select(c =>
        {
            // Those types are not correctly given in the menu, so special case them to color pickers.
            switch (c)
            {
                case CustomizeIndex.HighlightsColor:
                case CustomizeIndex.EyeColorLeft:
                case CustomizeIndex.EyeColorRight:
                case CustomizeIndex.FacePaintColor:
                    return MenuType.ColorPicker;
                case CustomizeIndex.BodyType: return MenuType.Nothing;
                case CustomizeIndex.FacePaintReversed:
                case CustomizeIndex.Highlights:
                case CustomizeIndex.SmallIris:
                case CustomizeIndex.Lipstick:
                    return MenuType.Checkmark;
                case CustomizeIndex.FacialFeature1:
                case CustomizeIndex.FacialFeature2:
                case CustomizeIndex.FacialFeature3:
                case CustomizeIndex.FacialFeature4:
                case CustomizeIndex.FacialFeature5:
                case CustomizeIndex.FacialFeature6:
                case CustomizeIndex.FacialFeature7:
                case CustomizeIndex.LegacyTattoo:
                    return MenuType.IconCheckmark;
            }

            var gameId = c.ToByteAndMask().ByteIdx;
            // Otherwise find the first menu corresponding to the id.
            // If there is none, assume a list.
            var menu = row.CharaMakeStruct.FirstOrNull(m => m.Customize == gameId);
            var ret  = (MenuType)(menu?.SubMenuType ?? (byte)MenuType.ListSelector);
            if (c is CustomizeIndex.TailShape && ret is MenuType.ListSelector)
                ret = MenuType.List1Selector;
            return ret;
        }).ToArray();
    }

    /// <summary> Set the availability of options according to actual availability. </summary>
    private static void SetAvailability(CustomizeSet set, CharaMakeType row)
    {
        Set(true,                                            CustomizeIndex.Height);
        Set(set.Faces.Count > 0,                             CustomizeIndex.Face);
        Set(true,                                            CustomizeIndex.Hairstyle);
        Set(true,                                            CustomizeIndex.Highlights);
        Set(true,                                            CustomizeIndex.SkinColor);
        Set(true,                                            CustomizeIndex.EyeColorRight);
        Set(true,                                            CustomizeIndex.HairColor);
        Set(true,                                            CustomizeIndex.HighlightsColor);
        Set(true,                                            CustomizeIndex.TattooColor);
        Set(set.NumEyebrows > 0,                             CustomizeIndex.Eyebrows);
        Set(true,                                            CustomizeIndex.EyeColorLeft);
        Set(set.NumEyeShapes > 0,                            CustomizeIndex.EyeShape);
        Set(set.NumNoseShapes > 0,                           CustomizeIndex.Nose);
        Set(set.NumJawShapes > 0,                            CustomizeIndex.Jaw);
        Set(set.NumMouthShapes > 0,                          CustomizeIndex.Mouth);
        Set(set.LipColorsDark.Count > 0,                     CustomizeIndex.LipColor);
        Set(GetListSize(row, CustomizeIndex.MuscleMass) > 0, CustomizeIndex.MuscleMass);
        Set(set.TailEarShapes.Count > 0,                     CustomizeIndex.TailShape);
        Set(GetListSize(row, CustomizeIndex.BustSize) > 0,   CustomizeIndex.BustSize);
        Set(set.FacePaints.Count > 0,                        CustomizeIndex.FacePaint);
        Set(set.FacePaints.Count > 0,                        CustomizeIndex.FacePaintColor);
        Set(true,                                            CustomizeIndex.FacialFeature1);
        Set(true,                                            CustomizeIndex.FacialFeature2);
        Set(true,                                            CustomizeIndex.FacialFeature3);
        Set(true,                                            CustomizeIndex.FacialFeature4);
        Set(true,                                            CustomizeIndex.FacialFeature5);
        Set(true,                                            CustomizeIndex.FacialFeature6);
        Set(true,                                            CustomizeIndex.FacialFeature7);
        Set(true,                                            CustomizeIndex.LegacyTattoo);
        Set(true,                                            CustomizeIndex.SmallIris);
        Set(set.Race != Race.Hrothgar,                       CustomizeIndex.Lipstick);
        Set(set.FacePaints.Count > 0,                        CustomizeIndex.FacePaintReversed);
        return;

        void Set(bool available, CustomizeIndex flag)
        {
            if (available)
                set.SetAvailable(flag);
        }
    }

    internal static void SetOrder(CustomizeSet set)
    {
        var ret = Enum.GetValues<CustomizeIndex>().ToArray();
        ret[(int)CustomizeIndex.TattooColor]   = CustomizeIndex.EyeColorLeft;
        ret[(int)CustomizeIndex.EyeColorLeft]  = CustomizeIndex.EyeColorRight;
        ret[(int)CustomizeIndex.EyeColorRight] = CustomizeIndex.TattooColor;

        var dict = ret.Skip(2).Where(set.IsAvailable).GroupBy(set.Type).ToDictionary(k => k.Key, k => k.ToArray());
        foreach (var type in Enum.GetValues<MenuType>())
            dict.TryAdd(type, []);
        set.Order = dict;
    }

    /// <summary> Set hairstyles per face for Hrothgar and make it simple for non-Hrothgar. </summary>
    private void SetHairByFace(CustomizeSet set)
    {
        if (set.Race != Race.Hrothgar)
        {
            set.HairByFace = Enumerable.Repeat(set.HairStyles, set.Faces.Count + 1).ToArray();
            return;
        }

        var tmp = new IReadOnlyList<CustomizeData>[set.Faces.Count + 1];
        tmp[0] = set.HairStyles;

        for (var i = 1; i <= set.Faces.Count; ++i)
        {
            tmp[i] = set.HairStyles.Where(Valid).ToArray();
            continue;

            bool Valid(CustomizeData c)
            {
                var data = _customizeSheet.TryGetRow(c.CustomizeId, out var customize) ? customize.Unknown0 : 0;
                return data == 0 || data == i + set.Faces.Count;
            }
        }

        set.HairByFace = tmp;
    }

    /// <summary>
    /// Create a list of lists of facial features and the legacy tattoo.
    /// Facial Features are bools in a bitfield, so we supply an "off" and an "on" value for simplicity of use.
    /// </summary>
    private static void SetFacialFeatures(CustomizeSet set, in CharaMakeType row)
    {
        var count = set.Faces.Count;
        set.FacialFeature1 = new List<(CustomizeData, CustomizeData)>(count);
        set.LegacyTattoo   = Create(CustomizeIndex.LegacyTattoo, 137905);

        var tmp = Enumerable.Repeat(0, 7).Select(_ => new (CustomizeData, CustomizeData)[count + 1]).ToArray();
        for (var i = 0; i < count; ++i)
        {
            var data = row.FacialFeatureOption[i];
            tmp[0][i + 1] = Create(CustomizeIndex.FacialFeature1, (uint)data.Option1);
            tmp[1][i + 1] = Create(CustomizeIndex.FacialFeature2, (uint)data.Option2);
            tmp[2][i + 1] = Create(CustomizeIndex.FacialFeature3, (uint)data.Option3);
            tmp[3][i + 1] = Create(CustomizeIndex.FacialFeature4, (uint)data.Option4);
            tmp[4][i + 1] = Create(CustomizeIndex.FacialFeature5, (uint)data.Option5);
            tmp[5][i + 1] = Create(CustomizeIndex.FacialFeature6, (uint)data.Option6);
            tmp[6][i + 1] = Create(CustomizeIndex.FacialFeature7, (uint)data.Option7);
        }

        set.FacialFeature1 = tmp[0];
        set.FacialFeature2 = tmp[1];
        set.FacialFeature3 = tmp[2];
        set.FacialFeature4 = tmp[3];
        set.FacialFeature5 = tmp[4];
        set.FacialFeature6 = tmp[5];
        set.FacialFeature7 = tmp[6];
        return;

        static (CustomizeData, CustomizeData) Create(CustomizeIndex i, uint data)
            => (new CustomizeData(i, CustomizeValue.Zero, data), new CustomizeData(i, CustomizeValue.Max, data, 1));
    }
}
