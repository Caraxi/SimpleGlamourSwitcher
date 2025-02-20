using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.Service;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class ModListDisplay {
    public static void Show(IHasModConfigs modable, bool showLinks = false) {

        var p = ImGui.GetItemRectMax();
        var s = new Vector2(ImGui.CalcItemWidth(), ImGui.GetTextLineHeightWithSpacing());
        
        var configs = modable.ModConfigs;
        
        var modName = "Vanilla";
        if (configs.Count > 0) {
            modName = configs.Count == 1 ? configs.First().ModDirectory : $"{configs.Count} Mods";

            if (showLinks) {
                ImGui.SetNextItemWidth(p.X - ImGui.GetCursorScreenPos().X - ImGui.GetStyle().ItemSpacing.X - s.Y);
            } else {
                ImGui.SetNextItemWidth(p.X - ImGui.GetCursorScreenPos().X);
            }
            
            ImGui.InputText("##modInfo", ref modName, 64, ImGuiInputTextFlags.ReadOnly);

            if (ImGui.IsItemHovered())
                using (ImRaii.Tooltip()) {
                    foreach (var modConfig in configs) {
                        if (configs.Count > 1) {
                            ImGui.Text(modConfig.ModDirectory);
                            if (ImGui.GetIO().KeyShift) ImGui.Separator();
                        }

                        if (configs.Count > 1 && ImGui.GetIO().KeyShift == false) continue;
                        using (ImRaii.PushIndent(1, configs.Count > 1)) {
                            if (ImGui.BeginTable("modSettingsTable", 2)) {
                                ImGui.TableNextColumn();
                                ImGui.TextDisabled("Priority");
                                ImGui.TableNextColumn();
                                ImGui.Text($"{modConfig.Priority}");
                                foreach (var (g, l) in modConfig.Settings) {
                                    ImGui.TableNextColumn();
                                    ImGui.TextDisabled($"{g}");
                                    ImGui.TableNextColumn();
                                    foreach (var sl in l) ImGui.Text(sl);
                                }

                                ImGui.EndTable();
                            }

                            ImGui.Spacing();
                        }
                    }

                    if (configs.Count > 1 && ImGui.GetIO().KeyShift == false) ImGui.TextDisabled("Hold SHIFT to show mod settings.");
                }

            

            if (showLinks) {
                ImGui.SameLine();
                if (configs.Count == 1) {
                    using (ImRaii.PushFont(UiBuilder.IconFont)) {
                        if (ImGui.Button(FontAwesomeIcon.Link.ToIconString(), new Vector2(s.Y))) {
                        
                            /*
                           if (modManager.TryGetMod(configs.Keys.First(), configs.Keys.First(), out var mod)) {
                               commandManager.ProcessCommand("/penumbra window true");
                               communicatorService.SelectTab.Invoke(TabType.Mods, mod);
                           }
                           */
                        }
                    }
                } else {
                    bool comboOpen;
                    using (ImRaii.PushColor(ImGuiCol.Text, Vector4.Zero)) {
                        comboOpen = ImGui.BeginCombo("##multiModLink", "", ImGuiComboFlags.NoPreview);
                    }

                    if (comboOpen) {
                        foreach (var config in configs) {
                            if (!ImGui.Selectable(config.ModDirectory + $"##{config.ModDirectory}")) continue;
                            /*
                            if (modManager.TryGetMod(modDir, modDir, out var mod)) {
                                commandManager.ProcessCommand("/penumbra window true");
                                communicatorService.SelectTab.Invoke(TabType.Mods, mod);
                            }
                            */
                        }

                        ImGui.EndCombo();
                    }

                    ImGui.GetWindowDrawList().AddText(UiBuilder.IconFont, ImGui.GetFontSize(), ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Text), FontAwesomeIcon.Link.ToIconString());
                }
            }

            
        } else {
            ImGui.SetNextItemWidth(p.X - ImGui.GetCursorScreenPos().X);
            ImGui.InputText("##modInfo", ref modName, 64, ImGuiInputTextFlags.ReadOnly);
        }
    }
}
