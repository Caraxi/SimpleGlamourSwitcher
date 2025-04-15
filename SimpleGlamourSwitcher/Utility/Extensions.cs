using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Utility;

public static class Extensions {
    public static string OrDefault(this string? str, string defaultValue) {
        return string.IsNullOrEmpty(str) ? defaultValue : str;
    }


    public static void OpenInExplorer(this FileInfo fileInfo) {
        Process.Start("explorer.exe", $"/select,\"" + fileInfo.FullName + "\"");
    }
    
    public static void OpenInExplorer(this DirectoryInfo dirInfo) {
        Process.Start("explorer.exe", dirInfo.FullName);
    }

    public static void OpenWithDefaultApplication(this FileInfo fileInfo) {
        Process.Start("explorer.exe", "\"" + fileInfo.FullName + "\"");
    }



    public static BonusItemFlag ToBonusSlot(this HumanSlot slot) {
        return slot switch {
            HumanSlot.Face => BonusItemFlag.Glasses,
            _ => BonusItemFlag.Unknown,
        };
    }
    
    
    public static Vector2 FitTo(this Vector2 vector, float x, float? y = null) {
        return vector * MathF.Min(x / vector.X, y ?? x / vector.Y);
    }
    
    public static Vector2 FitTo(this Vector2 vector, Vector2 other) => FitTo(vector, other.X, other.Y);


    public static T? GetAttribute<TEnum, T>(this TEnum enumValue) where T : Attribute where TEnum : Enum {
        var type = enumValue.GetType();
        var memInfo = type.GetMember(enumValue.ToString());
        var attributes = memInfo[0].GetCustomAttributes(typeof(T), false);
        return (attributes.Length > 0) ? (T)attributes[0] : null;
    }
    
    public static string GetDescriptionOrName<TEnum>(this TEnum e) where TEnum : Enum {
        return e.GetAttribute<TEnum, DescriptionAttribute>()?.Description ?? e.ToString();
    }

    public static T Clone<T>(this T obj) {
        return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj))!;
    }

    public static string PrettyName(this HumanSlot slot) {
        return slot switch {
            HumanSlot.LFinger => "Left Ring",
            HumanSlot.RFinger => "Right Ring",
            HumanSlot.Face => "Glasses",
            _ => slot.ToString()
        };
    }

    public static T Enroll<T>(this T window, WindowSystem windowSystem) where T : Window {
        windowSystem.AddWindow(window);
        return window;
    }
    
    
}

