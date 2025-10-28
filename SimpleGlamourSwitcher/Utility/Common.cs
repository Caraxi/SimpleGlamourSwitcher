using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Dalamud.Interface.Textures;
using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Utility;

public static class Common {
    public static ISharedImmediateTexture GetEmbeddedTexture(string embeddedPath) {
        return TextureProvider.GetFromManifestResource(Assembly.GetExecutingAssembly(), $"{nameof(SimpleGlamourSwitcher)}.{embeddedPath.Replace('/', '.')}");
    }

    
    public static readonly string[] SupportedImageFileTypes = ["png", "jpg", "jpeg"];
    public static FileInfo? GetImageFile(string pathWithoutExtension) {
        foreach (var type in SupportedImageFileTypes) {
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

/*

    private enum ClipboardType {
        Other,
        Image,
        Files,
    }
    
    private static class ClipboardCache {
        private static Stopwatch stopwatch = Stopwatch.StartNew();

        private static ClipboardType Type = ClipboardType.Other;
        private static string[]? files = null;
        private static Image? image = null;

        private static object? GetClipboard(out ClipboardType type) {
            type = ClipboardType.Other;
            if (stopwatch.ElapsedMilliseconds < 1000) {
                type = Type;
                return Type switch {
                    ClipboardType.Image => image,
                    ClipboardType.Files => files,
                    _ => null,
                };
            }
            stopwatch.Restart();

            if (Clipboard.ContainsImage()) {
                Type = ClipboardType.Image;
                image = Clipboard.GetImage();
                files = null;
                type = Type;
                return image;

            }

            if (Clipboard.ContainsFileDropList()) {
                Type = ClipboardType.Files;
                var fileDrops = Clipboard.GetFileDropList();
                files = new string[fileDrops.Count];
                fileDrops.CopyTo(files, 0);
                type = Type;
                return files;
            }

            Type = ClipboardType.Other;
            files = null;
            image = null;

            return null;
        }

        public static bool TryGetImage([NotNullWhen(true)] out Image? image) {
            image = GetClipboard(out var t) as Image;
            return t == ClipboardType.Image && image != null;
        }

        public static bool TryGetFiles([NotNullWhen(true)] out string[]? files) {
            files = GetClipboard(out var t) as string[];
            return t == ClipboardType.Files && files != null;
        }


    }

    */
    public static bool TryGetClipboardImage([NotNullWhen(true)] out object? image)
    {
        image = null;
        return false;
        // return ClipboardCache.TryGetImage(out image);
    }

    public static bool TryGetClipboardFile([NotNullWhen(true)] out string? file) {
        file = null;
        return false;
        /*
        if (!ClipboardCache.TryGetFiles(out var files)) return false;
        if (files.Length != 1) return false;
        file = files[0];
        return true;
        */
    }

    public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] sources) {
        foreach (var source in sources) {
            foreach(var item in source) yield return item;
        }
    }
    public static IReadOnlyList<T> ConcatList<T>(params IEnumerable<T>[] sources) {
        return Concat(sources).ToList();
    }

}
