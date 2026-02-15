using Dalamud.Interface.Textures;
using ECommons;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Service;

public static class CustomTextureProvider {

    static CustomTextureProvider() {
        Framework.RunOnTick(DisposeStaleLoop, delay: TimeSpan.FromSeconds(5));
    }

    private static void DisposeStaleLoop() {
        DisposeStale();
        Framework.RunOnTick(DisposeStaleLoop, delay: TimeSpan.FromSeconds(5));
    }

    
    private interface ITexture : IDisposable {
        public bool IsDisposed { get; }
        public ISharedImmediateTexture Texture { get; }
        public DateTime LastAccessed { get; }
    }
    
    private class CachedCustomTexture(ISharedImmediateTexture texture) : ITexture {
        public bool IsDisposed { get; private set; }
        public ISharedImmediateTexture Texture {
            get {
                LastAccessed = DateTime.Now;
                return field;
            }
        } = texture;

        public DateTime LastAccessed { get; private set; } = DateTime.Now;

        public void Dispose() {
            IsDisposed = true;
            if (Texture is IDisposable disposable) disposable.Dispose();
        }
    }


    private class BlankTexture() : ITexture {
        public void Dispose() => IsDisposed = true;
        public bool IsDisposed { get; private set; }
        public ISharedImmediateTexture Texture => TextureProvider.GetFromGameIcon(new GameIconLookup(0));
        public DateTime LastAccessed => DateTime.Now;
    }
    
    
    private readonly static Dictionary<string, ITexture> CustomTextures = new();
    public static ISharedImmediateTexture GetFromFile(FileInfo file) => GetFromFileAbsolute(file.FullName);
    
    public static ISharedImmediateTexture GetFromFileAbsolute(string path) {
        if (CustomTextures.TryGetValue(path, out var cache) && !cache.IsDisposed) {
            return cache.Texture;
        }

        if (path.Length >=5 && string.Equals(path[^5..], ".webp", StringComparison.InvariantCultureIgnoreCase)) {
            CustomTextures[path] = new BlankTexture();
            Task.Run(() => {
                PluginLog.Debug($"Creating Texture for WEBP: {path}");
                var webp = new WebPTexture(path);
                cache = new CachedCustomTexture(webp);
                CustomTextures[path] = cache;
                return cache.Texture;
            });

            return CustomTextures[path].Texture;
        }

        return TextureProvider.GetFromFileAbsolute(path);
    }

    public static void DisposeStale() {
        var disposeAt = DateTime.Now - TimeSpan.FromSeconds(5);
        CustomTextures.RemoveAll(v => v.Value.IsDisposed);
        foreach (var (key, cache) in CustomTextures) {
            if (!cache.IsDisposed && cache.LastAccessed < disposeAt) {
                PluginLog.Debug($"Disposing {cache.Texture.GetType().Name} for {key}");
                cache.Dispose();
            }
        }
    }
    
    public static void DisposeAll() {
        foreach(var c in CustomTextures) c.Value.Dispose();
    }
}
