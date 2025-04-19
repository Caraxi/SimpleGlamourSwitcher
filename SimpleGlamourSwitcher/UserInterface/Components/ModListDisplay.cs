using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Penumbra.Api.Enums;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class ModListDisplay {

    private static Vector2 buttonSize = new Vector2(ImGui.GetTextLineHeightWithSpacing());
    
    public static bool Show(IHasModConfigs modable, string slotName) {
        var edited = false;

        var p = ImGui.GetItemRectMax();
        var s = new Vector2(ImGui.CalcItemWidth(), ImGui.GetTextLineHeightWithSpacing());
        
        var configs = modable.ModConfigs;
        
        var modName = "Vanilla";
        Vector2 popupPosition;
        if (configs.Count > 0) {
            modName = configs.Count == 1 ? configs.First().ModDirectory : $"{configs.Count} Mods";


            ImGui.SetNextItemWidth(p.X - ImGui.GetCursorScreenPos().X - ImGui.GetStyle().ItemSpacing.X * 2 - buttonSize.X * 2);
            
            ImGui.InputText("##modInfo", ref modName, 64, ImGuiInputTextFlags.ReadOnly);
            popupPosition = ImGui.GetItemRectMin();
            buttonSize = new Vector2(ImGui.GetItemRectSize().Y);
            
            if (ImGui.IsItemHovered())
                using (ImRaii.Tooltip()) {
                    foreach (var modConfig in configs) {
                        if (configs.Count > 1) {
                            ImGui.Text(modConfig.ModDirectory);
                            if (ImGui.GetIO().KeyShift) ImGui.Separator();
                        }

                        if (configs.Count > 1 && ImGui.GetIO().KeyShift == false) continue;
                        using (ImRaii.PushIndent(1, configs.Count > 1)) {
                            ShowModSettingsTable(modConfig);
                            ImGui.Spacing();
                        }
                    }

                    if (configs.Count > 1 && ImGui.GetIO().KeyShift == false) ImGui.TextDisabled("Hold SHIFT to show mod settings.");
                }
            
            ImGui.SameLine();
            if (configs.Count == 1) {
                using (ImRaii.PushFont(UiBuilder.IconFont)) {
                    if (ImGui.Button("##modLink", buttonSize)) {
                        ShowPenumbraWindow(TabType.Mods, configs[0].ModDirectory);
                    }
                    ImGui.GetWindowDrawList().AddText(UiBuilder.IconFont, ImGui.GetFontSize(), ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Text), FontAwesomeIcon.Link.ToIconString());
                }
            } else {
                bool comboOpen;
                using (ImRaii.PushColor(ImGuiCol.Text, Vector4.Zero)) {
                    comboOpen = ImGui.BeginCombo("##multiModLink", "", ImGuiComboFlags.NoPreview);
                }

                if (comboOpen) {
                    foreach (var config in configs) {
                        if (!ImGui.Selectable(config.ModDirectory + $"##{config.ModDirectory}")) continue;
                        ShowPenumbraWindow(TabType.Mods, config.ModDirectory);
                    }

                    ImGui.EndCombo();
                }

                ImGui.GetWindowDrawList().AddText(UiBuilder.IconFont, ImGui.GetFontSize(), ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Text), FontAwesomeIcon.Link.ToIconString());
            }
            
        } else {
            ImGui.SetNextItemWidth(p.X - ImGui.GetCursorScreenPos().X - ImGui.GetStyle().ItemSpacing.X * 1 - buttonSize.X * 1);
            ImGui.InputText("##modInfo", ref modName, 64, ImGuiInputTextFlags.ReadOnly);
            popupPosition = ImGui.GetItemRectMin();
        }
        
        ImGui.SameLine();

        var id = $"##editMods_{ImGui.GetID("editModsPopup")}";
        
        if (ImGuiExt.IconButton($"{id}_open", FontAwesomeIcon.Edit, buttonSize)) {
            ImGui.OpenPopup(id);
        }
        
        ImGui.SetNextWindowPos(popupPosition);
        if (ImGui.BeginPopup(id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Modal)) {
            ImGui.Dummy(new Vector2(s.X, 0));
            ImGui.Text($"Edit Mods for {slotName}");
            ImGui.Separator();
            var modList = PenumbraIpc.GetModList.Invoke();
            
            foreach (var i in Enumerable.Range(0, modable.ModConfigs.Count)) {
                var m = modable.ModConfigs[i];
                using var editModId = ImRaii.PushId($"editMod_{m.ModDirectory}");
                var editModName = modList.GetValueOrDefault(m.ModDirectory, m.ModDirectory);
                
                if (ImGuiExt.IconButton("##trash", FontAwesomeIcon.Trash, buttonSize) && ImGui.GetIO().KeyShift) {
                    modable.ModConfigs.Remove(m);
                    edited = true;
                }

                if (ImGui.IsItemHovered()) {
                    using (ImRaii.Tooltip()) {
                        ImGui.Text("Remove mod from slot");
                        if (!ImGui.GetIO().KeyShift) {
                            ImGui.TextDisabled("Hold SHIFT to confirm");
                        }
                    }
                }
                
                ImGui.SameLine();
                if (ImGuiExt.IconButton("##update", FontAwesomeIcon.ArrowsSpin, buttonSize) && ImGui.GetIO().KeyShift) {
                    var getCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
                    var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(getCollection.EffectiveCollection.Id, m.ModDirectory);
                    if (getModSettings.Item1 != PenumbraApiEc.Success || getModSettings.Item2 == null) continue;
                    OutfitModConfig modConfig;
                    if (getModSettings is { Item1: PenumbraApiEc.Success, Item2: not null }) {
                        var modSettings = getModSettings.Item2.Value;
                        modConfig = new OutfitModConfig(m.ModDirectory, modSettings.Item1, modSettings.Item2, modSettings.Item3);
                    } else {
                        modConfig = new OutfitModConfig(m.ModDirectory, false, 0, []);
                    }

                    modable.ModConfigs[i] = modConfig;
                    edited = true;
                }

                if (ImGui.IsItemHovered()) {
                    using (ImRaii.Tooltip()) {
                        ImGui.Text("Update mod configs to current state");
                        
                        var getCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
                        var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(getCollection.EffectiveCollection.Id, m.ModDirectory);
                        if (getModSettings.Item1 != PenumbraApiEc.Success || getModSettings.Item2 == null) continue;
                        
                        if (getModSettings is { Item1: PenumbraApiEc.Success, Item2: not null }) {
                            var modSettings = getModSettings.Item2.Value;
                            var modConfig = new OutfitModConfig(m.ModDirectory, modSettings.Item1, modSettings.Item2, modSettings.Item3);
                            ShowModSettingsTable(modConfig);
                        } else {
                            var modConfig = new OutfitModConfig(m.ModDirectory, false, 0, []);
                            ShowModSettingsTable(modConfig, ImGuiTableFlags.BordersOuter);
                        }
                        
                        if (!ImGui.GetIO().KeyShift) {
                            ImGui.TextDisabled("Hold SHIFT to confirm");
                        }
                    }
                }
                
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputText($"##modInfo_{m.ModDirectory}", ref editModName, 64, ImGuiInputTextFlags.ReadOnly);

                if (ImGui.IsItemHovered()) {
                    using (ImRaii.Tooltip()) {
                        using (ImRaii.PushIndent()) {
                            ShowModSettingsTable(m);
                            ImGui.Spacing();
                        }
                    }
                }
            }
            
            
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.BeginCombo("##addMod", "Add Mod...", ImGuiComboFlags.HeightLargest)) {
                ImGui.Spacing();

                if (ImGui.IsWindowAppearing()) {
                    ImGui.SetKeyboardFocusHere();
                    modSearch = string.Empty;
                }
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputTextWithHint("##search", "Search...", ref modSearch, 256);
                ImGui.Separator();
                
                if (ImGui.BeginChild("modList", new Vector2(ImGui.GetContentRegionAvail().X, 400 * ImGuiHelpers.GlobalScale))) {
                    foreach (var mod in modList.OrderBy(k => k.Value)) {

                        if (!string.IsNullOrWhiteSpace(modSearch) && !(mod.Key.Contains(modSearch, StringComparison.InvariantCultureIgnoreCase) || mod.Value.Contains(modSearch, StringComparison.InvariantCultureIgnoreCase))) continue;
                        
                        if (ImGui.Selectable(mod.Value)) {
                            var getCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
                            var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(getCollection.EffectiveCollection.Id, mod.Key);
                            if (getModSettings is { Item1: PenumbraApiEc.Success, Item2: not null }) {
                                var modSettings = getModSettings.Item2.Value;
                                var modConfig = new OutfitModConfig(mod.Key, modSettings.Item1, modSettings.Item2, modSettings.Item3);
                                modable.ModConfigs.Add(modConfig);
                            } else {
                                var modConfig = new OutfitModConfig(mod.Key, false, 0, []);
                                modable.ModConfigs.Add(modConfig);
                            }

                            edited = true;
                            ImGui.CloseCurrentPopup();
                        }

                        if (ImGui.IsItemHovered()) {
                            using (ImRaii.Tooltip()) {
                                var getCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
                                var getModSettings = PenumbraIpc.GetCurrentModSettingsWithTemp.Invoke(getCollection.EffectiveCollection.Id, mod.Key);
                                if (getModSettings is { Item1: PenumbraApiEc.Success, Item2: not null }) {
                                    var modSettings = getModSettings.Item2.Value;
                                    var modConfig = new OutfitModConfig(mod.Key, modSettings.Item1, modSettings.Item2, modSettings.Item3);
                                    ShowModSettingsTable(modConfig);
                                } else {
                                    var modConfig = new OutfitModConfig(mod.Key, false, 0, []);
                                    ShowModSettingsTable(modConfig);
                                }
                            }
                        }
                    }
                }
                ImGui.EndChild();
                ImGui.EndCombo();
            }
            


            if (ImGui.Button("Done", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 1.5f))) {
                ImGui.CloseCurrentPopup();
            }
            
            ImGui.EndPopup();
        }

        return edited;
    }


    private static string modSearch = string.Empty;


    private static void ShowModSettingsTable(OutfitModConfig modConfig, ImGuiTableFlags flags = ImGuiTableFlags.None) {
        if (ImGui.BeginTable("modSettingsTable", 2, flags)) {
            ImGui.TableNextColumn();
            ImGui.TextDisabled("Enabled");
            ImGui.TableNextColumn();
            ImGui.Text($"{modConfig.Enabled}");
            if (modConfig.Enabled) {
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

            }
                               
            ImGui.EndTable();
        }
    }


    private static void ShowPenumbraWindow(TabType tab, string modDirectory) {
        Plugin.MainWindow.HoldAutoClose();
        Commands.ProcessCommand("/penumbra window off");
        Framework.RunOnTick(() => {
            PenumbraIpc.OpenMainWindow.Invoke(TabType.Mods, modDirectory);
        }, delayTicks: 1);
    }
}
