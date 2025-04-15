using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Newtonsoft.Json.Linq;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC.Glamourer;

namespace SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

public abstract record ApplicableParameter : Applicable {
    public override void ApplyToCharacter(ref bool requestRedraw) {
        throw new Exception("Cannot be applied directly.");
    }

    public virtual JObject ToJObject() {
        return new JObject() {
            {"Apply", Apply },
        };
    }

    public abstract bool ShowEditor(string s, AppearanceParameterKind kind, bool readOnly);
}

public record ApplicableParameterColorAlpha : ApplicableParameterColor {
    public float Alpha;
    
    public override JObject ToJObject() {
        var obj = base.ToJObject();
        obj.Add("Alpha", Alpha);
        return obj;
    }
    
    public static ApplicableParameterColorAlpha FromExistingStateAlpha(IDefaultOutfitOptionsProvider defaultOptionsProvider, AppearanceParameterKind kind, GlamourerParameterColor? parameter) {
        if (parameter == null) return new ApplicableParameterColorAlpha();
        
        return new ApplicableParameterColorAlpha {
            Apply = parameter.Apply && defaultOptionsProvider.DefaultEnabledParameterKinds.Contains(kind),
            Red = parameter.Red ?? 0,
            Green = parameter.Green ?? 0,
            Blue = parameter.Blue ?? 0,
            Alpha = parameter.Alpha ?? 1,
        };
    }

    public override bool ShowEditor(string s, AppearanceParameterKind kind, bool readOnly) {
        var color = new Vector4(Red, Green, Blue, Alpha);

        if (ImGui.ColorEdit4(s, ref color) && !readOnly) {
            (Red, Green, Blue, Alpha) = (color.X, color.Y, color.Z, color.W);
            return true;
        }
        

        return false;
    }
}

public record ApplicableParameterColor : ApplicableParameter {
    public float Red;
    public float Green;
    public float Blue;
    
    public override JObject ToJObject() {
        var obj = base.ToJObject();
        obj.Add("Red", Red);
        obj.Add("Green", Green);
        obj.Add("Blue", Blue);
        return obj;
    }
    
    public static ApplicableParameterColor FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, AppearanceParameterKind kind, GlamourerParameterColor? parameter) {
        if (parameter == null) return new ApplicableParameterColor();
        
        return new ApplicableParameterColor {
            Apply = parameter.Apply && defaultOptionsProvider.DefaultEnabledParameterKinds.Contains(kind),
            Red = parameter.Red ?? 0,
            Green = parameter.Green ?? 0,
            Blue = parameter.Blue ?? 0,
        };
    }

    public override bool ShowEditor(string s, AppearanceParameterKind kind, bool readOnly) {
        var color = new Vector3(Red, Green, Blue);


        if (ImGui.ColorEdit3(s, ref color) && !readOnly) {
            (Red, Green, Blue) = (color.X, color.Y, color.Z);
            return true;
        }
        
        return false;
    }
    
};

public record ApplicableParameterPercent : ApplicableParameter {
    public float Percentage;
    
    public override JObject ToJObject() {
        var obj = base.ToJObject();
        obj.Add("Percentage", Percentage);
        return obj;
    }
    
    public static ApplicableParameterPercent FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, AppearanceParameterKind kind, GlamourerParameterPercentage? parameter) {
        if (parameter == null) return new ApplicableParameterPercent();
        
        return new ApplicableParameterPercent {
            Apply = parameter.Apply && defaultOptionsProvider.DefaultEnabledParameterKinds.Contains(kind),
            Percentage = parameter.Percentage ?? 0,
        };
    }
    
    public override bool ShowEditor(string s, AppearanceParameterKind kind, bool readOnly) {
        if (readOnly) {
            var valueStr = $"{Percentage * 100}%";
            ImGui.InputText(s, ref valueStr, 64, ImGuiInputTextFlags.ReadOnly);
            return false;
        }

        var value = Percentage * 100;
        if (!ImGui.SliderFloat(s, ref value, 0, 100)) return false;
        Percentage = value / 100f;
        return true;
    }
    
}

public record ApplicableParameterFloat : ApplicableParameter {
    public float Value;
    
    public override JObject ToJObject() {
        var obj = base.ToJObject();
        obj.Add("Value", Value);
        return obj;
    }

    public static ApplicableParameterFloat FromExistingState(IDefaultOutfitOptionsProvider defaultOptionsProvider, AppearanceParameterKind kind, GlamourerParameterFloat? parameter) {
        if (parameter == null) return new ApplicableParameterFloat();
        
        return new ApplicableParameterFloat {
            Apply = parameter.Apply && defaultOptionsProvider.DefaultEnabledParameterKinds.Contains(kind),
            Value = parameter.Value ?? 0,
        };
    }
    
    public override bool ShowEditor(string s, AppearanceParameterKind kind, bool readOnly) {
        if (readOnly) {
            var valueStr = $"{Value}";
            ImGui.InputText(s, ref valueStr, 64, ImGuiInputTextFlags.ReadOnly);
            return false;
        }

        return ImGui.DragFloat(s, ref Value, 0.01f);
    }
}