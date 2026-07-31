using System.Numerics;

namespace SimpleGlamourSwitcher.API;

public class IpcImage(string path, Vector2 uvMin, Vector2 uvMax) {
    public string Path { get; } = path;
    public Vector2 UvMin { get; } = uvMin;
    public Vector2 UvMax { get; } = uvMax;
}
