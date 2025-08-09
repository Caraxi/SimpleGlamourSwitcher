using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public unsafe class AutomationPage(CharacterConfigFile character) : Page {
    private bool dirty;
    private readonly AutomationConfig automationConfig = character.Automation.Clone();
    private readonly LazyAsync<OrderedDictionary<Guid, OutfitConfigFile>> outfits = new(character.GetOutfits);
    private const float Width = 600;
    private int selectedGearset = RaptureGearsetModule.Instance()->CurrentGearsetIndex;

    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        var offset = ImGui.GetContentRegionAvail().X > Width * ImGuiHelpers.GlobalScale;
        if (offset) {
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X / 2f - Width / 2f);
        }

        if (ImGui.BeginChild("automation", new Vector2(offset ? Width * ImGuiHelpers.GlobalScale : ImGui.GetContentRegionAvail().X, ImGui.GetContentRegionAvail().Y), true)) {
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get())) {
                ImGui.TextWrapped("Automation allows Simple Glamour Switcher to change your outfit based on the changing game state.");
            }

            ImGui.Separator();

            if (ImGui.CollapsingHeader("Gearsets", ImGuiTreeNodeFlags.DefaultOpen)) {
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get())) {
                    ImGui.TextWrapped("Gearset automations apply when you change gearsets.");
                }

                ImGui.Separator();

                using (ImRaii.PushIndent()) {
                    var currentGearset = GameHelper.GetGearsetByIndex(selectedGearset);

                    foreach (var (gsId, gsAutomation) in automationConfig.Gearsets.ToArray().OrderBy(kvp => kvp.Key)) {
                        var gs = GameHelper.GetGearsetById(gsId);
                        using (ImRaii.PushId($"gearsetAutomation_{gs}")) {
                            var automation = gsAutomation;
                            if (ShowAutomation(ref automation, $"Gearset #{gsId + 1}: {gs?.Name ?? "Unknown"}", (ImGui.GetTextLineHeight() + 16 * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X))) {
                                automationConfig.Gearsets[gsId] = automation;
                                dirty = true;
                            }

                            ImGui.SameLine();

                            using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
                                if (ImGuiExt.IconButton("removeGearset", FontAwesomeIcon.Trash, new Vector2(ImGui.GetItemRectSize().Y)) && ImGui.GetIO().KeyShift) {
                                    automationConfig.Gearsets.Remove(gsId);
                                    dirty = true;
                                }
                            }

                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                                ImGui.SetTooltip($"Remove automation for Gearset '[{gsId + 1}] {gs?.Name ?? "Unknown"}'\nHold SHIFT to confirm.");
                            }
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Spacing();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - (ImGui.GetTextLineHeight() + 16 * ImGuiHelpers.GlobalScale + ImGui.GetStyle().ItemSpacing.X));
                    CustomInput.Combo("Select Gearset", currentGearset?.Name ?? "Not Selected", (search) => {
                        var gsModule = RaptureGearsetModule.Instance();
                        for (var i = 0; i < gsModule->NumGearsets; i++) {
                            var gearset = gsModule->GetGearset(i);
                            if (gearset == null) continue;
                            if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) continue;

                            if (!string.IsNullOrWhiteSpace(search)) {
                                if (!gearset->NameString.Contains(search, StringComparison.InvariantCultureIgnoreCase)) {
                                    continue;
                                }
                            }

                            if (ImGui.IsWindowAppearing() && i == selectedGearset) {
                                ImGui.SetScrollHereY(0.5f);
                            }

                            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get(), automationConfig.Gearsets.ContainsKey(gearset->Id))) {
                                if (ImGui.Selectable($"[{gearset->Id + 1:00}] {gearset->NameString}##Gearset#{gearset->Id}", i == selectedGearset)) {
                                    selectedGearset = i;
                                    return true;
                                }
                            }
                        }

                        return false;
                    }, errorMessage: currentGearset == null ? "No Gearset Selected" : automationConfig.Gearsets.ContainsKey(currentGearset.Value.Id) ? "Gearset Already Added" : string.Empty, style: new ComboStyle() { FramePadding = new Vector2(16, 8), PadTop = false });

                    ImGui.SameLine();

                    using (ImRaii.Disabled(currentGearset == null || automationConfig.Gearsets.ContainsKey(currentGearset.Value.Id))) {
                        if (ImGuiExt.IconButton("addCurrentGearset", FontAwesomeIcon.PlusCircle, new Vector2(ImGui.GetItemRectSize().Y)) && currentGearset != null) {
                            automationConfig.Gearsets[currentGearset.Value.Id] = null;
                            dirty = true;
                        }

                        if (currentGearset == null) {
                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                                ImGui.SetTooltip("Add automation for selected gearset.");
                            }
                        } else if (automationConfig.Gearsets.ContainsKey(currentGearset.Value.Id)) {
                            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                                ImGui.SetTooltip($"Add an automation for Gearset '[{currentGearset.Value.Id + 1}] {currentGearset.Value.Name}'\n(Already Exists)");
                            }
                        } else {
                            if (ImGui.IsItemHovered()) {
                                ImGui.SetTooltip($"Add an automation for Gearset '[{currentGearset.Value.Id + 1}] {currentGearset.Value.Name}'");
                            }
                        }
                    }
                }
            }

            if (ImGui.CollapsingHeader("Other", ImGuiTreeNodeFlags.DefaultOpen)) {
                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get())) {
                    ImGui.TextWrapped("These automations will apply in certain scenarios when no other automation is triggered.");
                }
                ImGui.Separator();

                using (ImRaii.PushIndent()) {
                    ShowAutomation(ref automationConfig.DefaultGearset, "Gearset Changed");
                    ImGuiComponents.HelpMarker("This automation is only triggered when your active gearset is changed, but the specific gearset is not configured above.");
                    ShowAutomation(ref automationConfig.Login, "On Login");
                    ImGuiComponents.HelpMarker("This automation is triggered when logging in to the game, if the active gearset is not configured.");
                    ShowAutomation(ref automationConfig.CharacterSwitch, "On Character Switch");
                    ImGuiComponents.HelpMarker("This automation is triggered when using Simple Glamour Switcher to switch to another character appearance, if the active gearset is not configured.");
                }
            }

            using (ImRaii.Disabled(!dirty)) {
                if (ImGuiExt.ButtonWithIcon("Save Automations", FontAwesomeIcon.Save, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                    character.Automation = automationConfig;
                    character.Dirty = true;
                    character.Save();
                    dirty = false;
                    MainWindow.PopPage();
                }
            }
        }

        ImGui.EndChild();
    }

    private bool ShowAutomation(ref Guid? editGuid, string label, float leaveSpace = 0) {
        var guid = editGuid;

        bool Draw(string search) {
            var modified = false;
            using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get())) {
                if (ImGui.Selectable(GetDisplayName(null), guid == null)) {
                    guid = null;
                    modified = true;
                }
            }

            foreach (var (outfitGuid, outfit) in outfits.Value.OrderBy(kvp => character.ParseFolderPath(kvp.Value.Folder)).ThenBy(kvp => kvp.Value.SortName)) {
                var fullName = character.ParseFolderPath(outfit.Folder, false) + " / " + outfit.Name;
                var fullNameCollapse = string.Join('/', character.ParseFolderPath(outfit.Folder, false).Split('/', StringSplitOptions.TrimEntries)) + "/" + outfit.Name;
                if (!(fullName.Contains(search, StringComparison.InvariantCultureIgnoreCase) || fullNameCollapse.Contains(search, StringComparison.InvariantCultureIgnoreCase))) continue;

                if (ImGui.IsWindowAppearing() && guid == outfit.Guid) {
                    ImGui.SetScrollHereY(0.5f);
                }

                using (ImRaii.PushId(outfit.Guid.ToString())) {
                    using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get(), outfit.Folder != Guid.Empty)) {
                        if (ImGui.Selectable(outfit.Folder == Guid.Empty ? outfit.Name : character.ParseFolderPath(outfit.Folder, false) + " /", guid == outfitGuid)) {
                            guid = outfitGuid;
                            modified = true;
                        }
                    }

                    if (outfit.Folder != Guid.Empty) {
                        ImGui.SameLine();
                        ImGui.Text(outfit.Name);
                    }
                }
            }

            return modified;
        }

        ImGui.Spacing();
        ImGui.Spacing();
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - leaveSpace);
        if (CustomInput.Combo(label, GetDisplayName(guid), Draw, style: new ComboStyle() { FramePadding = new Vector2(16, 8), PadTop = false })) {
            editGuid = guid;
            dirty = true;
            return true;
        }

        return false;
    }

    private string GetDisplayName(Guid? guid) {
        if (guid == null) return "Do Not Apply";
        return outfits.Value.TryGetValue(guid.Value, out var outfit) ? outfit.Name : $"{guid}";
    }

    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        if (dirty) controlFlags |= WindowControlFlags.PreventClose;

        using (ImRaii.Disabled(dirty && !ImGui.GetIO().KeyShift)) {
            if (ImGuiExt.ButtonWithIcon(dirty ? "Revert Changes" : "Cancel", FontAwesomeIcon.Ban, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                controlFlags |= WindowControlFlags.PreventClose;
                MainWindow?.PopPage();
            }
        }

        if (!dirty || ImGui.GetIO().KeyShift) return;
        
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Hold SHIFT to confirm");
        }
    }
}
