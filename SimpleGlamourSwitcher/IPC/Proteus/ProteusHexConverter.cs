using System.Globalization;
using System.Numerics;
using Newtonsoft.Json;

namespace SimpleGlamourSwitcher.IPC.Proteus;

public class ProteusHexConverter : JsonConverter<Vector3> {
    public override void WriteJson(JsonWriter writer, Vector3 c, JsonSerializer serializer) {
        var r = Math.Clamp((int)(c.X * 255), 0, 255);
        var g = Math.Clamp((int)(c.Y * 255), 0, 255);
        var b = Math.Clamp((int)(c.Z * 255), 0, 255);
        writer.WriteValue($"#{r:X2}{g:X2}{b:X2}");
    }

    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer) {
        if (reader.TokenType != JsonToken.String) return Vector3.One;
        var hex = (reader.Value as string)?.TrimStart('#');
        if (string.IsNullOrEmpty(hex)) return Vector3.One;
        if (hex.Length == 3) hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        if (hex.Length != 6) return Vector3.One;
        if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb)) return Vector3.One;
        var r = ((rgb >> 16) & 0xFF) / 255f;
        var g = ((rgb >> 8) & 0xFF) / 255f;
        var b = (rgb & 0xFF) / 255f;
        return new Vector3(r, g, b);
    }
}
