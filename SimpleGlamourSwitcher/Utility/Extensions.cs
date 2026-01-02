using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;

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
    
    public static string RemoveImGuiId(this string label) {
        return label.Split("##")[0];
    }

    public static BonusItemFlag ToBonusSlot(this HumanSlot slot) {
        return slot switch {
            HumanSlot.Face => BonusItemFlag.Glasses,
            _ => BonusItemFlag.Unknown,
        };
    }

    public static Vector4 ToVector4(this uint color) {
        return ImGui.ColorConvertU32ToFloat4(color);
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

    public static string PrettyName(this EquipSlot slot) {
        return slot switch {
            EquipSlot.LFinger => "Left Ring",
            EquipSlot.RFinger => "Right Ring",
            _ => slot.ToString()
        };
    }

    public static string PrettyName(this CustomizeIndex cIndex) {
        return cIndex switch {
            CustomizeIndex.BustSize => "Bust Size",
            CustomizeIndex.EyeShape => "Eye Shape",
            _ => cIndex.ToDefaultName()
        };
    }

    public static string PrettyName(this AppearanceParameterKind kind) {
        return kind switch {
            _ => kind.ToString()
        };
    }

    public static T Enroll<T>(this T window, WindowSystem windowSystem) where T : Window {
        windowSystem.AddWindow(window);
        return window;
    }
    
    public static bool IsPlayerWorld(this World world) {
        if (world.Name.Data.IsEmpty) return false;
        if (world.DataCenter.RowId == 0) return false;
        if (world.IsPublic) return true;
        return char.IsUpper((char)world.Name.Data.Span[0]);
    }

    public static uint Get(this ImGuiCol col) => ImGui.GetColorU32(col);
    
    public static bool TryWaitResult<T>(this Task<T> task, out T? result) {
        task.Wait();
        if (task.IsCompletedSuccessfully) {
            result = task.Result;
            return true;
        }
        
        result = default;
        return false;
        
    }
    
    internal static FullEquipType ToEquipType(this Item item)
    {
        var slot   = (EquipSlot)item.EquipSlotCategory.RowId;
        var weapon = (WeaponCategory)item.ItemUICategory.RowId;
        return slot.ToEquipType(weapon);
    }

    extension(ClassJobCategory category) {
        public bool Allows(ClassJob classJob) {
            return category.GetClassJobs().Any(cj => classJob.RowId == cj.RowId);
        }

        public IEnumerable<ClassJob> GetClassJobs() {
            if (category.GLA) yield return GetClassJob(1);
            if (category.PGL) yield return GetClassJob(2);
            if (category.MRD) yield return GetClassJob(3);
            if (category.LNC) yield return GetClassJob(4);
            if (category.ARC) yield return GetClassJob(5);
            if (category.CNJ) yield return GetClassJob(6);
            if (category.THM) yield return GetClassJob(7);
            if (category.CRP) yield return GetClassJob(8);
            if (category.BSM) yield return GetClassJob(9);
            if (category.ARM) yield return GetClassJob(10);
            if (category.GSM) yield return GetClassJob(11);
            if (category.LTW) yield return GetClassJob(12);
            if (category.WVR) yield return GetClassJob(13);
            if (category.ALC) yield return GetClassJob(14);
            if (category.CUL) yield return GetClassJob(15);
            if (category.MIN) yield return GetClassJob(16);
            if (category.BTN) yield return GetClassJob(17);
            if (category.FSH) yield return GetClassJob(18);
            if (category.PLD) yield return GetClassJob(19);
            if (category.MNK) yield return GetClassJob(20);
            if (category.ACN) yield return GetClassJob(21);
            if (category.WAR) yield return GetClassJob(22);
            if (category.DRG) yield return GetClassJob(23);
            if (category.BRD) yield return GetClassJob(24);
            if (category.WHM) yield return GetClassJob(25);
            if (category.BLM) yield return GetClassJob(26);
            if (category.SMN) yield return GetClassJob(27);
            if (category.SCH) yield return GetClassJob(28);
            if (category.ROG) yield return GetClassJob(29);
            if (category.NIN) yield return GetClassJob(30);
            if (category.MCH) yield return GetClassJob(31);
            if (category.DRK) yield return GetClassJob(32);
            if (category.AST) yield return GetClassJob(33);
            if (category.SAM) yield return GetClassJob(34);
            if (category.RDM) yield return GetClassJob(35);
            if (category.BLU) yield return GetClassJob(36);
            if (category.GNB) yield return GetClassJob(37);
            if (category.DNC) yield return GetClassJob(38);
            if (category.RPR) yield return GetClassJob(39);
            if (category.SGE) yield return GetClassJob(40);
            if (category.VPR) yield return GetClassJob(41);
            if (category.PCT) yield return GetClassJob(42);
            yield break;
            ClassJob GetClassJob(uint id) => DataManager.GetExcelSheet<ClassJob>().GetRow(id);
        }

        public IEnumerable<ClassJob> GetBaseClasses() {
            foreach (var cj in category.GetClassJobs()) {
                if (cj.ClassJobParent.RowId == cj.RowId) yield return cj;
            }
        }
    }
    
    public static bool IsEquipableWeaponOrToolForClassSlot(this Item item, ClassJob classJob, EquipSlot equipSlot) {
        var equipType = item.ToEquipType();
        
        if (classJob.RowId is 26 or 27 or 28) { // Arcanist, Summoner and Scholar can use all books.
            return equipType == FullEquipType.Book && equipSlot == EquipSlot.MainHand;
        }
        
        if (!(equipType.IsWeapon() || equipType.IsTool())) return false;
        switch (equipSlot) {
            default:
                return item.ClassJobCategory.Value.Allows(classJob);
        }
    }
}
