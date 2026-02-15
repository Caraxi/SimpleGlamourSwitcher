using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace SimpleGlamourSwitcher.Utility;

public class WebPTexture : ISharedImmediateTexture, IDisposable {
    private readonly Image image;
    private readonly Dictionary<int, IDalamudTextureWrap?> frames = new();
    private readonly Stopwatch frameTimer = Stopwatch.StartNew();
    private int frameIndex;
    private uint currentFrameDuration = uint.MaxValue;
    private readonly IDalamudTextureWrap empty;
    
    public WebPTexture(string filePath) {
        image = Image.Load(filePath);
        if (image.Frames.Count > 1) {
            currentFrameDuration =  image.Frames[0].Metadata.GetWebpMetadata().FrameDelay;
        }
        
        empty = TextureProvider.CreateEmpty(RawImageSpecification.A8(1, 1), false, false);
    }

    private IDalamudTextureWrap? GetFrame(int index) {
        if (image.Frames.Count <= index) return null;
        if (frames.TryGetValue(index, out var frame)) {
            return frame;
        }
        
        frames[index] = null;
        using var frameImage = new Image<Rgba32>(image.Width, image.Height);
        frameImage.Frames.AddFrame(image.Frames[index]);
        frameImage.Frames.RemoveFrame(0);
        var buffer = new byte[frameImage.Width * frameImage.Height * 4];
        frameImage.CopyPixelDataTo(buffer);
        try {
            return frames[index] = TextureProvider.CreateFromRaw(RawImageSpecification.Rgba32(image.Width, image.Height), buffer);
        } catch {
            return null;
        }
    }
    
    public void Dispose() {
        image.Dispose();
        foreach (var (_, frame) in frames) frame?.Dispose();
        empty.Dispose();
    }

    public IDalamudTextureWrap GetWrapOrEmpty() => GetWrapOrDefault(empty) ?? empty;

    public IDalamudTextureWrap? GetWrapOrDefault(IDalamudTextureWrap? defaultWrap = null) {
        if (image.Frames.Count <= 1) { 
            var wrap = GetFrame(0);
            return wrap ?? defaultWrap;
        }

        if (frameTimer.ElapsedMilliseconds > currentFrameDuration) {
            frameIndex++;
            frameTimer.Restart();
            if (frameIndex >= image.Frames.Count) {
                frameIndex = 0;
            }

            currentFrameDuration = image.Frames[frameIndex].Metadata.GetWebpMetadata().FrameDelay;
        }

        return GetFrame(frameIndex % image.Frames.Count) ?? defaultWrap;
    }

    public bool TryGetWrap([NotNullWhen(true)] out IDalamudTextureWrap? texture, out Exception? exception) {
        try {
            texture = GetWrapOrDefault();
            exception = null;
            return texture != null;
        } catch (Exception ex) {
            texture = null;
            exception = ex;
            return false;
        }
    }

    public Task<IDalamudTextureWrap> RentAsync(CancellationToken cancellationToken) => throw new NotImplementedException();
}
