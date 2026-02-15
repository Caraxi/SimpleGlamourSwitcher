using System.Reflection;
using Dalamud.Interface.Textures;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;

namespace SimpleGlamourSwitcher.Utility;

public static class Common {
    public static ISharedImmediateTexture GetEmbeddedTexture(string embeddedPath) {
        return TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), $"{nameof(SimpleGlamourSwitcher)}.{embeddedPath.Replace('/', '.')}");
    }
    
    public static FileInfo? GetImageFile(string pathWithoutExtension) {
        foreach (var type in IImageProvider.SupportedImageFileTypes) {
            var fileInfo = new FileInfo($"{pathWithoutExtension}.{type}");
            if (fileInfo.Exists) return fileInfo;
        }
        return null;
    }
    
    public static IEnumerable<HumanSlot> GetGearSlots() {
        yield return HumanSlot.Head;
        yield return HumanSlot.Body;
        yield return HumanSlot.Hands;
        yield return HumanSlot.Legs;
        yield return HumanSlot.Feet;
        yield return HumanSlot.Ears;
        yield return HumanSlot.Neck;
        yield return HumanSlot.Wrists;
        yield return HumanSlot.RFinger;
        yield return HumanSlot.LFinger;
        yield return HumanSlot.Face;
    }

    public static IEnumerable<T> Set<T>(params T[] entries) => entries;

    public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] sources) {
        foreach (var source in sources) {
            foreach(var item in source) yield return item;
        }
    }
    public static IReadOnlyList<T> ConcatList<T>(params IEnumerable<T>[] sources) {
        return Concat(sources).ToList();
    }

    public static IEnumerable<IGrouping<TEnum, string>> GetEnumValueNames<TEnum>() where TEnum : struct, Enum {
        var names = Enum.GetNames<TEnum>();
        return names.GroupBy(Enum.Parse<TEnum>);
    }
    
    public static IEnumerable<string> GetEnumValueNames<TEnum>(TEnum value) where TEnum : struct, Enum {
        var names = Enum.GetNames<TEnum>();
        return names.Where(e => value.Equals(Enum.Parse<TEnum>(e)));
    }
}
