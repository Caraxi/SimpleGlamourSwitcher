using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Penumbra.Api.Enums;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class ModListDisplay {
    private static Vector2 _buttonSize = new(ImGui.GetTextLineHeightWithSpacing());
    private static readonly Cached<Dictionary<string, string>> CachedModList = new(TimeSpan.FromSeconds(5), () => PenumbraIpc.GetModList.Invoke());
    private static string _locatingMod = string.Empty;
    private static string modSearch = string.Empty;
    
    private static bool TryParseModName(string modDirectory, out string modName) {
        if (!CachedModList.Value.TryGetValue(modDirectory, out modName!)) {
            modName = modDirectory;
            return false;
        }
        return true;
    }

    public static bool IsValid(IHasModConfigs modable) {
        return modable.ModConfigs.Count == 0 || modable.ModConfigs.All(m => TryParseModName(m.ModDirectory, out _));
    }
    
    public static bool Show(IHasModConfigs modable, string slotName, float width = -1) {
        var edited = false;
        var p = ImGui.GetItemRectMax();
        if (width <= 0) width = p.X - ImGui.GetCursorScreenPos().X;
        var s = new Vector2(ImGui.CalcItemWidth(), ImGui.GetTextLineHeightWithSpacing());
        var configs = modable.ModConfigs;

        var extraButtons = 1;

        if (modable is IHasCustomizePlusTemplateConfigs) {
            extraButtons++;
        }
        
        var modName = "No Associated Mods";
        Vector2 popupPosition;
        if (configs.Count > 0) {
            extraButtons++;
            bool modExists;

            if (configs.Count == 1) {
                modExists = TryParseModName(configs.First().ModDirectory, out modName);
            } else {
                modName = $"{configs.Count} Mods";
                modExists = configs.All(m => TryParseModName(m.ModDirectory, out _));
            }

            ImGui.SetNextItemWidth(width - ImGui.GetStyle().ItemSpacing.X * extraButtons - _buttonSize.X * extraButtons);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow, !modExists)) {
                ImGui.InputText("##modInfo", ref modName, 64, ImGuiInputTextFlags.ReadOnly);
            }
            popupPosition = ImGui.GetItemRectMin();
            _buttonSize = new Vector2(ImGui.GetItemRectSize().Y);
            
            if (ImGui.IsItemHovered())
                using (ImRaii.Tooltip()) {
                    foreach (var modConfig in configs) {
                        var exists = TryParseModName(modConfig.ModDirectory, out var name);
                        if (configs.Count > 1) {
                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow, !modExists)) {
                                ImGui.Text(name);
                            }
                            
                            if (ImGui.GetIO().KeyShift) ImGui.Separator();
                        }
                        
                        if (!exists) {
                            using (ImRaii.PushIndent(1, configs.Count > 1)) {
                                ImGui.TextColored(ImGuiColors.DalamudRed, "This mod does not exist.");
                            }
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
                    if (ImGui.Button("##modLink", _buttonSize)) {
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
            ImGui.SetNextItemWidth(width - ImGui.GetStyle().ItemSpacing.X * extraButtons - _buttonSize.X * extraButtons);
            using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled))) {
                ImGui.InputText("##modInfo", ref modName, 64, ImGuiInputTextFlags.ReadOnly);
            }
            
            popupPosition = ImGui.GetItemRectMin();
        }
        
        ImGui.SameLine();

        var id = $"##editMods_{ImGui.GetID("editModsPopup")}_{slotName}";
        
        
        if (ImGui.Button($"##{id}_open", _buttonSize)) {
            ImGui.OpenPopup(id);
        }
        
        ImGui.GetWindowDrawList().AddText(UiBuilder.IconFont, ImGui.GetFontSize(), ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Text), FontAwesomeIcon.Edit.ToIconString());

        ImGui.SetNextWindowPos(popupPosition);
        if (ImGui.BeginPopup(id, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Modal)) {
            if (!string.IsNullOrEmpty(_locatingMod)) ImGui.Text($"Locating: {_locatingMod}");
            if (ImGui.IsWindowAppearing()) {
                _locatingMod = string.Empty;
            }
            ImGui.Dummy(new Vector2(s.X, 0));
            ImGui.Text($"Edit Mods for {slotName}");
            ImGui.Separator();
            var modList = PenumbraIpc.GetModList.Invoke();

            if (string.IsNullOrWhiteSpace(_locatingMod)) {
                foreach (var i in Enumerable.Range(0, modable.ModConfigs.Count)) {
                    var m = modable.ModConfigs[i];
                    using var editModId = ImRaii.PushId($"editMod_{m.ModDirectory}");
                    var exists = TryParseModName(m.ModDirectory, out var editModName);
                    
                    if (ImGuiExt.IconButton("##trash", FontAwesomeIcon.Trash, _buttonSize) && ImGui.GetIO().KeyShift) {
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
                    
                    if (exists) {
                        ImGui.SameLine();
                        if (ImGuiExt.IconButton("##update", FontAwesomeIcon.ArrowsSpin, _buttonSize) && ImGui.GetIO().KeyShift) {
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

                    } else {
                        ImGui.SameLine();
                        if (ImGuiExt.IconButton("##locateMod", FontAwesomeIcon.Search, _buttonSize)) {
                            _locatingMod = m.ModDirectory;
                        }

                        if (ImGui.IsItemHovered()) {
                            using (ImRaii.Tooltip()) {
                                ImGui.Text("Locate mod");
                            }
                        }
                    }

                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudYellow, !exists)) {
                        ImGui.InputText($"##modInfo_{m.ModDirectory}", ref editModName, 64, ImGuiInputTextFlags.ReadOnly);
                    }

                    if (ImGui.IsItemHovered()) {
                        using (ImRaii.Tooltip()) {
                            if (!exists) {
                                ImGui.TextColored(ImGuiColors.DalamudRed, "This mod does not exist.");
                            }

                            using (ImRaii.PushIndent()) {
                                ShowModSettingsTable(m);
                                ImGui.Spacing();
                            }
                        }
                    }
                }

            } else {
                ImGui.Text($"Locating mod: {_locatingMod}");
            }
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (ImGui.BeginCombo("##addMod", string.IsNullOrWhiteSpace(_locatingMod) ? "Add Mod..." : "Locate Mod...", ImGuiComboFlags.HeightLargest)) {
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
                            if (string.IsNullOrWhiteSpace(_locatingMod)) {
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
                            } else {
                                var existing = modable.ModConfigs.FirstOrDefault(m => m.ModDirectory == _locatingMod);
                                if (existing is not null) {
                                    var index = modable.ModConfigs.IndexOf(existing);
                                    modable.ModConfigs[index] = existing with { ModDirectory = mod.Key };
                                    edited = true;
                                    _locatingMod = string.Empty;
                                }
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
        
        if (modable is IHasCustomizePlusTemplateConfigs cPlusTemplateConfig && ActiveCharacter?.CustomizePlusProfile != null && ActiveCharacter.CustomizePlusProfile != Guid.Empty) {
            ImGui.SameLine();
            edited |= CustomizePlusTemplateEditor.ShowButton(cPlusTemplateConfig, slotName, _buttonSize, popupPosition, s.X);
        }
        
        return edited;
    }

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
