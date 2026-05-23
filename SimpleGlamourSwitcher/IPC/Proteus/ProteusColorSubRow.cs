using System.Numerics;
using Newtonsoft.Json;

namespace SimpleGlamourSwitcher.IPC.Proteus;

public record ProteusColorSubRow (
    [property: JsonConverter(typeof(ProteusHexConverter))] Vector3 Diffuse,
    float Emissive,
    int Opacity
);
