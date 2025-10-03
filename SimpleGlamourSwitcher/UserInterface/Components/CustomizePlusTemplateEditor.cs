using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using OtterGui.Extensions;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class CustomizePlusTemplateEditor {
    public static bool ShowButton(IHasCustomizePlusTemplateConfigs equipment, string slotName, Vector2 buttonSize, Vector2 popupPosition, float popupMinWidth) {
        if (ActiveCharacter == null) return false;
        if (ActiveCharacter.CustomizePlusProfile == null) return false;
        var profile = ActiveCharacter.CustomizePlusProfile.Value;

        using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled), equipment.CustomizePlusTemplateConfigs.Count == 0)) {
            if (ImGui.Button("C+", buttonSize)) {
                ImGui.OpenPopup($"cplus_template_editor_{slotName}");
            }
        }

        if (ImGui.IsItemHovered()) {
            using (ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2))
            using (ImRaii.Tooltip()) {
                ImGui.Text($"{slotName} Customize+ Templates");
                ImGui.Separator();
                if (equipment.CustomizePlusTemplateConfigs.Count == 0) {
                    ImGui.TextDisabled("No templates have been configured.");
                }

                if (CustomizePlus.TryGetTemplatesFromProfile(profile, out var templates)) {
                                    
                    foreach (var config in equipment.CustomizePlusTemplateConfigs) {
                        if (!templates.FindFirst(t => t.UniqueId == config.TemplateId, out var template)) continue;
                        ImGui.TextColored(config.Enable ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed, $"{(config.Enable?"Enable":"Disable")}");
                        ImGuiExt.SameLineNoSpace();
                        ImGui.Text($": {template.Name}");
                    }
                }
            }
        }

        ImGui.SetNextWindowPos(popupPosition);
        using (var popup = ImRaii.Popup($"cplus_template_editor_{slotName}", ImGuiWindowFlags.Modal | ImGuiWindowFlags.AlwaysAutoResize)) {
            if (popup.Success) {
                ImGui.Dummy(new Vector2(popupMinWidth, 0));
                ImGui.Text($"Customize+ Templates for {slotName}");
                ImGui.Separator();

                using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled))) {
                    ImGui.TextWrapped($"Toggle customize+ templates when this item is equipped. Simple Glamour Switcher will attempt to revert changes when switching to another item in the same slot.");
                }
                
                ImGui.Separator();
                var inputStyle = new TextInputStyle() { PadTop = false, FramePadding = new Vector2(8, 4), BorderSize = 1};
                
                if (CustomizePlus.TryGetTemplatesFromProfile(profile, out var templates)) {
                    using var _ = templates.Count > 10 ? ImRaii.Child("scrolling_templates", new Vector2(ImGui.GetContentRegionAvail().X, 260 * ImGuiHelpers.GlobalScale)) : ImRaii.Group();
                    
                    foreach (var template in templates) {
                        var configured = equipment.CustomizePlusTemplateConfigs.Find(tc => tc.TemplateId == template.UniqueId);
                        ImGui.Spacing();
                        var enable = false;
                        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeightWithSpacing() * 2 - ImGui.GetStyle().ItemSpacing.X);
                        if (configured != null) {
                            enable = configured.Enable;
                            CustomInput.ReadOnlyInputText($"##template_{template.UniqueId}", template.Name, style: inputStyle with { TextColour = enable ? ImGuiColors.HealerGreen : ImGuiColors.DPSRed});
                            
                        } else {
                            CustomInput.ReadOnlyInputText($"##template_{template.UniqueId}", template.Name, style: inputStyle with { TextColour = ImGui.GetColorU32(ImGuiCol.TextDisabled)});
                        }
                        
                        ImGui.SameLine();
                        if (ImGui.Checkbox($"##enable_{template.UniqueId}", ref enable)) {
                            if (configured == null) {
                                equipment.CustomizePlusTemplateConfigs.Add(new CustomizeTemplateConfig { TemplateId = template.UniqueId, Enable = enable });
                            } else {
                                configured.Enable = enable;
                            }
                        }

                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            equipment.CustomizePlusTemplateConfigs.RemoveAll(p => p.TemplateId == template.UniqueId);
                        }
                        
                        if (ImGui.IsItemHovered()) {
                            using (ImRaii.Tooltip()) {
                                if (configured != null) {
                                    ImGui.Text($"Template '{template.Name}' will be {(configured.Enable?"enabled":"disabled")}\nwhen this outfit equips {slotName}.");
                                    ImGui.TextDisabled("Right Click to Clear");
                                } else {
                                    ImGui.Text($"Template '{template.Name}' will be unchanged\nwhen this outfit equips {slotName}.");
                                }
                            }
                        }
                    }
                }

                if (ImGui.Button("Done")) {
                    ImGui.CloseCurrentPopup();
                }
            }
        }
        
        return false;
    }
}
