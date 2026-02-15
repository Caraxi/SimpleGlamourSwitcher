using SixLabors.ImageSharp.Formats.Webp;

namespace SimpleGlamourSwitcher.Configuration.Parts;

public class AnimatedImageConfiguration {
    public float MaxFrameRate = 10;
    public bool UseLosslessCompression = false;
    public int CompressionQuality = 50;
    public WebpEncodingMethod EncodingMethod = WebpEncodingMethod.Default;
}
