using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class AdvancedMaterialsDisplay {


    public static bool ShowAdvancedMaterialsDisplay<T>(ApplicableItem<T> item, string slotName) {

        ImGui.SameLine();
        using (ImRaii.PushFont(UiBuilder.IconFont)) {
            if (ImGui.Button(FontAwesomeIcon.Palette.ToIconString(), new Vector2(ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2))) {
                
            }
        }

        if (ImGui.IsItemHovered()) {
            using (ImRaii.Tooltip()) {
                ImGui.Text($"{slotName} Advanced Dyes");
                ImGui.Separator();

                if (item.Materials.Count == 0) {
                    ImGui.TextDisabled("No advanced dyes are configured.");
                } else {
                    using (ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero))
                    using (ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(3, ImGui.GetStyle().CellPadding.Y))) {
                        if (ImGui.BeginTable("materialsTable", 4)) {
                            foreach (var material in item.Materials) {
                                ImGui.TableNextColumn();
                                var t = $"{material.MaterialValueIndex.MaterialString()} {material.MaterialValueIndex.RowString()}";
                                ImGui.SetNextItemWidth(ImGui.CalcTextSize(t).X + ImGui.GetStyle().FramePadding.X * 2);
                                ImGui.InputText("##material", ref t, 128, ImGuiInputTextFlags.ReadOnly);
                                ImGui.TableNextColumn();
                                ImGui.ColorButton("Diffuse", new Vector4(material.DiffuseR, material.DiffuseG, material.DiffuseB, 1));
                                ImGui.SameLine();
                                ImGui.ColorButton("Specular", new Vector4(material.SpecularR, material.SpecularG, material.SpecularB, 1));
                                ImGui.SameLine();
                                ImGui.ColorButton("Emissive", new Vector4(material.EmissiveR, material.EmissiveG, material.EmissiveB, 1));
                                ImGui.TableNextColumn();
                                ImGui.Text($"{material.Gloss}");
                                ImGui.TableNextColumn();
                                ImGui.TextUnformatted($"{material.SpecularA * 100}%");
                            }

                            ImGui.EndTable();
                        }
                    }
                }
            }
        }
        

                
        return false;
    }
    
}
