using System.Collections.Immutable;
using Lumina.Excel.Sheets;
using Lumina.Extensions;
using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Service;

public static class DataCache {
    private static readonly Dictionary<(byte clan, byte gender), ImmutableList<CharaMakeCustomize>> Hairstyles = new();
    public static ImmutableList<CharaMakeCustomize> GetHairstyles(byte clan, byte gender) {
        if (Hairstyles.TryGetValue((clan, gender), out var hairstyles)) return hairstyles;
        var list = new List<CharaMakeCustomize>();
        var hairMakeType = DataManager.GetExcelSheet<HairMakeType>().FirstOrDefault(c => c.Gender == gender && c.Tribe.RowId == clan);
        if (hairMakeType.Tribe.RowId != clan || hairMakeType.Gender != gender || !hairMakeType.CharaMakeStruct.TryGetFirst(c => c.Customize == (byte)CustomizeIndex.Hairstyle, out var customizeTypes))
            return [];
        foreach (var a in customizeTypes.SubMenuParam.Where(a => a != 0)) {
            if (!DataManager.GetExcelSheet<CharaMakeCustomize>().TryGetRow(a, out var cmc)) continue;
            list.Add(cmc);
        }

        hairstyles = list.OrderBy(c => c.FeatureID).ToImmutableList();
        Hairstyles.Add((clan, gender), hairstyles);
        return hairstyles;
    }
}
