using System.Numerics;
using Dalamud.Bindings.ImGui;
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

    public abstract bool ShowEditor(string s, AppearanceParameterKind kind);
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

    public override bool ShowEditor(string s, AppearanceParameterKind kind) {
        var color = new Vector4(Red, Green, Blue, Alpha);

        if (ImGui.ColorEdit4(s, ref color)) {
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

    public override bool ShowEditor(string s, AppearanceParameterKind kind) {
        var color = new Vector3(Red, Green, Blue);


        if (ImGui.ColorEdit3(s, ref color)) {
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

    private float maxValue = 100;
    private float minValue = 0;
    
    public override bool ShowEditor(string s, AppearanceParameterKind kind) {
        var value = Percentage * 100;
        if (value > maxValue) maxValue = value;
        if (value < minValue) minValue = value;
        if (!ImGui.SliderFloat(s, ref value, minValue, maxValue, "%.2f%%")) return false;
        if (ImGui.IsItemHovered()) {
            ImGui.SetTooltip("Hold CTRL and click to set arbitrary values.");
        }
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
    
    public override bool ShowEditor(string s, AppearanceParameterKind kind) {
        return ImGui.DragFloat(s, ref Value, 0.01f);
    }
}