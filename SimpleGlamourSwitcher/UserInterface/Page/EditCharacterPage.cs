﻿using System.Numerics;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ImGuiNET;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditCharacterPage(CharacterConfigFile? character) : Page {

    public bool IsNewCharacter { get; } = character == null;

    public CharacterConfigFile Character { get; } = character ?? CharacterConfigFile.Create(PluginConfig);


    private bool dirty;
    private string characterName = character?.Name ?? string.Empty;
    private Guid? penumbraCollection = character?.PenumbraCollection;
    
    private HashSet<CustomizeIndex> defaultEnabledCustomizeIndexes = character?.DefaultEnabledCustomizeIndexes.Clone() ?? [];
    private HashSet<HumanSlot> defaultDisableEquipSlots = character?.DefaultDisabledEquipmentSlots.Clone() ?? [];
    
    private Dictionary<Guid, string> penumbraCollections = PenumbraIpc.GetCollections.Invoke();
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNewCharacter ? "Creating" : "Editing Character", shadowed: true);
        ImGuiExt.CenterText(IsNewCharacter ? "New Character" : Character.Name, shadowed: true);
    }

    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        if (dirty) {
            controlFlags |= WindowControlFlags.PreventClose;
        }
        
        var maxW = 640;
        
        if (ImGui.GetContentRegionAvail().X > maxW * ImGuiHelpers.GlobalScale) {
            
            ImGui.Dummy(new Vector2((ImGui.GetContentRegionAvail().X - maxW * ImGuiHelpers.GlobalScale) / 2f));
            ImGui.SameLine();
        }

        if (ImGui.BeginChild("characterEdit", new Vector2(MathF.Min(maxW * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().X), ImGui.GetContentRegionAvail().Y))) {
            var a = WindowControlFlags.None;
            using (ImRaii.ItemWidth(ImGui.GetContentRegionAvail().X)) {
                dirty |= CustomInput.InputText("Character Name", ref characterName, 100);
                dirty |= CustomInput.Combo("Penumbra Collection", penumbraCollection == null ? "No Collection Selected" : penumbraCollections.TryGetValue(penumbraCollection.Value, out var selectedName) ? selectedName : $"{penumbraCollection}", (search) => {
                    a |= WindowControlFlags.PreventClose;
                    var r = false;
                    foreach (var (guid, name) in penumbraCollections.OrderBy(kvp => kvp.Value)) {
                        if (!string.IsNullOrWhiteSpace(search) && !name.Contains(search, StringComparison.InvariantCultureIgnoreCase)) continue;
                        if (ImGui.Selectable(name, guid == penumbraCollection)) {
                            r = true;
                            penumbraCollection = guid;
                        }
                        ImGui.SameLine();
                        using (ImRaii.PushFont(UiBuilder.MonoFont)) {
                            var idSize = ImGui.CalcTextSize($"{guid}");
                            var dummySize = ImGui.GetContentRegionAvail().X - idSize.X - 10;
                            if (dummySize > 0) {
                                ImGui.Dummy(new Vector2(dummySize, 1));
                                ImGui.SameLine();
                            }
                            
                            ImGui.TextDisabled($"{guid}");
                        }
                    }

                    return r;
                });
                
            }

            if (ImGui.CollapsingHeader("Default Appearance Toggles")) {
                ImGui.Columns(3, "defaultAppearanceToggles", false);
                foreach (var ci in Enum.GetValues<CustomizeIndex>()) {
                    var v = defaultEnabledCustomizeIndexes.Contains(ci);
                    if (ImGui.Checkbox($"{ci}##defaultEnabledCustomize", ref v)) {
                        dirty = true;
                        if (v) {
                            defaultEnabledCustomizeIndexes.Add(ci);
                        } else {
                            defaultEnabledCustomizeIndexes.Remove(ci);
                        }
                    }
                    ImGui.NextColumn();
                }
                
                ImGui.Columns(1);
            }
            
            if (ImGui.CollapsingHeader("Default Equipment Toggles")) {
                ImGui.Columns(2, "defaultEquipmentToggles", false);
                foreach (var hs in Common.GetGearSlots()) {
                    var v = !defaultDisableEquipSlots.Contains(hs);
                    if (ImGui.Checkbox($"{hs.PrettyName()}##defaultEnabledEquip", ref v)) {
                        dirty = true;
                        if (v) {
                            defaultDisableEquipSlots.Remove(hs);
                        } else {
                            defaultDisableEquipSlots.Add(hs);
                        }
                    }
                    ImGui.NextColumn();
                }
                
                ImGui.Columns(1);
            }
            
            if (ImGui.CollapsingHeader("Image")) {
                var style = (PluginConfig.CustomStyle ?? Style.Default).CharacterPolaroid;
                ImageEditor.Draw(Character, style, characterName, ref controlFlags);
            }
            
            controlFlags |= a;
            
            using (ImRaii.Disabled(!(dirty || IsNewCharacter))) {
                if (ImGui.Button(IsNewCharacter ? "Create Character" : "Save Character", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                    controlFlags |= WindowControlFlags.PreventClose;
                    Character.Name = characterName;
                    Character.PenumbraCollection = penumbraCollection;
                    Character.DefaultEnabledCustomizeIndexes = defaultEnabledCustomizeIndexes;
                    Character.DefaultDisabledEquipmentSlots = defaultDisableEquipSlots;
                    Character.Dirty = true;
                    Character.Save(true);
                    MainWindow?.PopPage();
                }
            }

            using (ImRaii.Disabled(dirty && !ImGui.GetIO().KeyShift)) {
                if (ImGui.Button(IsNewCharacter ? "Cancel" : dirty ? "Revert Changes" : "Cancel", new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                    controlFlags |= WindowControlFlags.PreventClose;
                    MainWindow?.PopPage();
                }
            }

            if (dirty && !ImGui.GetIO().KeyShift) {
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                    ImGui.SetTooltip("Hold SHIFT to confirm");
                }
            }


            
            
            
            
        }
        
        ImGui.EndChild();
        
        
       
    }
}