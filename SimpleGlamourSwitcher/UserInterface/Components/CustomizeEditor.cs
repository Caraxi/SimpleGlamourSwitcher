using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using Race = Lumina.Excel.Sheets.Race;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class CustomizeEditor {

    public static void ShowReadOnly(string label, CustomizeIndex index, ApplicableCustomize applicableCustomize) {
        ShowEditor(label, index, applicableCustomize, true);
    }


    private static bool ShowEditorEntry(string label, CustomizeIndex index, ApplicableCustomize customize, bool isReadOnly = false) {
        switch (index) {
            case CustomizeIndex.Race: {
                var selectedRace = DataManager.GetExcelSheet<Race>().GetRow(customize.Value);
                if (isReadOnly) {
                    var v = selectedRace.Feminine.ExtractText();
                    ImGui.InputText(label, ref v, 32, ImGuiInputTextFlags.ReadOnly);
                    return false;
                }

                goto default;
            }

            case CustomizeIndex.Gender:
                if (isReadOnly) {
                    var v = customize.Value == 0 ? "Male" : "Female";
                    ImGui.InputText(label, ref v, 32, ImGuiInputTextFlags.ReadOnly);
                    return false;
                }
                
                goto default;
                
            case CustomizeIndex.BodyType:
            case CustomizeIndex.Height:
            case CustomizeIndex.Clan:
            case CustomizeIndex.Face:
            case CustomizeIndex.Hairstyle:
            case CustomizeIndex.Highlights:
            case CustomizeIndex.SkinColor:
            case CustomizeIndex.EyeColorRight:
            case CustomizeIndex.HairColor:
            case CustomizeIndex.HighlightsColor:
            case CustomizeIndex.FacialFeature1:
            case CustomizeIndex.FacialFeature2:
            case CustomizeIndex.FacialFeature3:
            case CustomizeIndex.FacialFeature4:
            case CustomizeIndex.FacialFeature5:
            case CustomizeIndex.FacialFeature6:
            case CustomizeIndex.FacialFeature7:
            case CustomizeIndex.LegacyTattoo:
            case CustomizeIndex.TattooColor:
            case CustomizeIndex.Eyebrows:
            case CustomizeIndex.EyeColorLeft:
            case CustomizeIndex.EyeShape:
            case CustomizeIndex.SmallIris:
            case CustomizeIndex.Nose:
            case CustomizeIndex.Jaw:
            case CustomizeIndex.Mouth:
            case CustomizeIndex.Lipstick:
            case CustomizeIndex.LipColor:
            case CustomizeIndex.MuscleMass:
            case CustomizeIndex.TailShape:
            case CustomizeIndex.BustSize:
            case CustomizeIndex.FacePaint:
            case CustomizeIndex.FacePaintReversed:
            case CustomizeIndex.FacePaintColor:
            default:
                

                if (isReadOnly) {
                    var v = $"{customize.Value}";
                    ImGui.InputText(label, ref v, 4, ImGuiInputTextFlags.ReadOnly);
                } else {
                    var v = (int)customize.Value;
                    if (ImGui.InputInt(label, ref v)) {
                        customize.Value = (byte)v;
                        return true;
                    }
                }
                
                

                return false;
                
        }
    }

    public static bool ShowEditor(string label, CustomizeIndex index, ApplicableCustomize customize, bool isReadOnly = false) {

        using var group = ImRaii.Group();
        var r = ShowEditorEntry(label, index, customize, isReadOnly);

        if (customize is ApplicableCustomizeModable modable) {
            ModListDisplay.Show(modable);
        }
        

        return r;
    }
    
    
    
    
    
}