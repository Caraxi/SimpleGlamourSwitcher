using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;
using World = Lumina.Excel.Sheets.World;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditCharacterPage(CharacterConfigFile? character) : Page {

    public bool IsNewCharacter { get; } = character == null;

    public CharacterConfigFile Character { get; } = character ?? CharacterConfigFile.Create(PluginConfig);


    private bool dirty;
    private string characterName = character?.Name ?? string.Empty;

    private bool applyOnLogin = character?.ApplyOnLogin ?? true;
    private bool applyOnPluginReload = character?.ApplyOnPluginReload ?? false;
    
    private (string Name, uint World) honorificIdentity = character?.HonorificIdentity ?? (ClientState.LocalPlayer?.Name.TextValue ?? string.Empty, ClientState.LocalPlayer?.HomeWorld.RowId ?? 0);
    private (string Name, uint World) heelsIdentity = character?.HeelsIdentity ?? (ClientState.LocalPlayer?.Name.TextValue ?? string.Empty, ClientState.LocalPlayer?.HomeWorld.RowId ?? 0);

    private Guid? customizePlusProfile = character == null ? CustomizePlus.TryGetActiveProfileOnCharacter(0, out var activeProfile) ? activeProfile.UniqueId : null : character.CustomizePlusProfile;

    private Guid? penumbraCollection = character == null ? PenumbraIpc.GetCollectionForObject.Invoke(0).EffectiveCollection.Id : character.PenumbraCollection;
    
    private readonly HashSet<CustomizeIndex> defaultEnabledCustomizeIndexes = character?.DefaultEnabledCustomizeIndexes.Clone() ?? [];
    private readonly HashSet<HumanSlot> defaultDisableEquipSlots = character?.DefaultDisabledEquipmentSlots.Clone() ?? [];
    private readonly HashSet<AppearanceParameterKind> defaultEnabledParameterKinds = character?.DefaultEnabledParameterKinds.Clone() ?? [];
    private readonly HashSet<ToggleType> defaultEnabledToggles = character?.DefaultEnabledToggles.Clone() ?? [];
    
    private readonly Dictionary<Guid, string> penumbraCollections = PenumbraIpc.GetCollections.Invoke();
    
    private PolaroidStyle? outfitStyle = character?.OutfitPolaroidStyle.Clone();
    private PolaroidStyle? folderStyle = character?.FolderPolaroidStyle.Clone();


    private List<Guid> linkBefore = character?.DefaultLinkBefore ?? [];
    private List<Guid> linkAfter = character?.DefaultLinkAfter ?? [];

    private List<AutoCommandEntry> autoCommandBeforeOutfit = character?.AutoCommandBeforeOutfit.Clone() ?? [];
    private List<AutoCommandEntry> autoCommandAfterOutfit = character?.AutoCommandAfterOutfit.Clone() ?? [];

    private OutfitLinksEditor? OutfitLinksEditor;
    
    
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

                    if (ImGuiExt.SelectableWithNote("No Collection", "Collection will not be changed", penumbraCollection == null)) {
                        r = true;
                        penumbraCollection = null;
                    }
                    
                    foreach (var (guid, name) in penumbraCollections.OrderBy(kvp => kvp.Value)) {
                        if (!string.IsNullOrWhiteSpace(search) && !name.Contains(search, StringComparison.InvariantCultureIgnoreCase)) continue;
                        if (ImGuiExt.SelectableWithNote($"{name}##{guid}", $"{guid}", guid == penumbraCollection, PluginInterface.UiBuilder.MonoFontHandle)) {
                            r = true;
                            penumbraCollection = guid;
                        }
                    }

                    return r;
                });

                var honorificReady = HonorificIpc.IsReady();
                
                using (ImRaii.Disabled(!honorificReady))
                using (ImRaii.ItemWidth(ImGui.CalcItemWidth() / 2 - ImGui.GetStyle().ItemSpacing.X / 2 - (honorificReady ? 0 : ImGui.GetTextLineHeightWithSpacing()))) {
                    dirty |= CustomInput.InputText("Honorific Identity:", ref honorificIdentity.Name, 100);
                    ImGui.SameLine();
                    var selectedWorld = DataManager.GetExcelSheet<Lumina.Excel.Sheets.World>().GetRowOrDefault(honorificIdentity.World);
                    dirty |= CustomInput.Combo("##HonorificIdentityWorld", selectedWorld?.Name.ExtractText() ?? $"World#{honorificIdentity.World}", () => {
                        var m = false;
                        var appearing = ImGui.IsWindowAppearing();
                        var lastDc = uint.MaxValue;

                        void World(string name, uint worldId, WorldDCGroupType? dc = null) {
                            
                            if (dc != null) {
                                if (lastDc != dc.Value.RowId) {
                                    lastDc = dc.Value.RowId;
                                    ImGui.TextDisabled($"{dc.Value.Name.ExtractText()}");
                                }
                            }

                            if (ImGui.Selectable($"    {name}", honorificIdentity.World == worldId)) {
                                honorificIdentity.World = worldId;
                                m = true;
                                ImGui.CloseCurrentPopup();
                            }

                            if (appearing && honorificIdentity.World == worldId) {
                                ImGui.SetScrollHereY();
                            }
                        }

                        foreach (var w in DataManager.GetExcelSheet<World>()!.Where(w => w.IsPlayerWorld()).OrderBy(w => w.DataCenter.Value.Name.ExtractText()).ThenBy(w => w.Name.ExtractText())) {
                            World(w.Name.ExtractText(), w.RowId, w.DataCenter.Value);
                        }

                        return m;
                    });
                }

                if (!honorificReady) {
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("A supported version of Honorific is not detected.", FontAwesomeIcon.ExclamationTriangle, ImGuiColors.DalamudYellow);
                }
                
                var heelsReady = HeelsIpc.IsReady();
                using (ImRaii.Disabled(!heelsReady))
                using (ImRaii.ItemWidth(ImGui.CalcItemWidth() / 2 - ImGui.GetStyle().ItemSpacing.X / 2 - (heelsReady ? 0 : ImGui.GetTextLineHeightWithSpacing()))) {
                    dirty |= CustomInput.InputText("Simple Heels Identity:", ref heelsIdentity.Name, 100);
                    ImGui.SameLine();
                    var selectedWorld = DataManager.GetExcelSheet<Lumina.Excel.Sheets.World>().GetRowOrDefault(heelsIdentity.World);
                    dirty |= CustomInput.Combo("##heelsIdentityWorld", selectedWorld?.Name.ExtractText() ?? $"World#{heelsIdentity.World}", () => {
                        var m = false;
                        var appearing = ImGui.IsWindowAppearing();
                        var lastDc = uint.MaxValue;

                        void World(string name, uint worldId, WorldDCGroupType? dc = null) {
                            
                            if (dc != null) {
                                if (lastDc != dc.Value.RowId) {
                                    lastDc = dc.Value.RowId;
                                    ImGui.TextDisabled($"{dc.Value.Name.ExtractText()}");
                                }
                            }

                            if (ImGui.Selectable($"    {name}", heelsIdentity.World == worldId)) {
                                heelsIdentity.World = worldId;
                                m = true;
                                ImGui.CloseCurrentPopup();
                            }

                            if (appearing && heelsIdentity.World == worldId) {
                                ImGui.SetScrollHereY();
                            }
                        }

                        foreach (var w in DataManager.GetExcelSheet<World>()!.Where(w => w.IsPlayerWorld()).OrderBy(w => w.DataCenter.Value.Name.ExtractText()).ThenBy(w => w.Name.ExtractText())) {
                            World(w.Name.ExtractText(), w.RowId, w.DataCenter.Value);
                        }

                        return m;
                    });
                }
                if (!heelsReady) {
                    ImGui.SameLine();
                    ImGuiComponents.HelpMarker("A supported version of Simple Heels is not detected.", FontAwesomeIcon.ExclamationTriangle, ImGuiColors.DalamudYellow);
                }
                
                var customizeReady = CustomizePlus.IsReady();
                using (ImRaii.Disabled(!customizeReady)) {
                    var profileName = "Not Set";
                    if (customizeReady) {
                        if (customizePlusProfile != null) {
                            if (customizePlusProfile == Guid.Empty) {
                                profileName = "No Profile";
                            } else {
                                if (CustomizePlus.TryGetProfileDataByUniqueId(customizePlusProfile.Value, out var profile)) {
                                    profileName = profile.Name;
                                } else {
                                    profileName = $"{customizePlusProfile}";
                                }
                            }
                        }
                    } else {
                        profileName = "A supported version of Customize+ is not detected";
                    }
                    
                    dirty |= CustomInput.Combo("Customize Plus Profile", profileName, () => {

                        var r = false;
                        var list = CustomizePlus.GetProfileList();

                        if (ImGuiExt.SelectableWithNote("Not Set", "Leave profile as is", customizePlusProfile == null)) {
                            customizePlusProfile = null;
                            r = true;
                        }
                        
                        if (ImGuiExt.SelectableWithNote("No Profile", "Use empty profile", customizePlusProfile == Guid.Empty)) {
                            customizePlusProfile = Guid.Empty;
                            r = true;
                        }
                        
                        foreach (var p in list) {
                            if (ImGuiExt.SelectableWithNote(p.VirtualPath + $"##{p.UniqueId}", p.UniqueId.ToString(), customizePlusProfile == p.UniqueId, PluginInterface.UiBuilder.MonoFontHandle)) {
                                customizePlusProfile = p.UniqueId;
                                r = true;
                            }
                        }
                        
                        return r;
                    });
                }
            }

            if (ImGui.CollapsingHeader("Automatic Applications")) {
                dirty |= ImGui.Checkbox("Apply Default Outfit on Login", ref applyOnLogin);
                dirty |= ImGui.Checkbox("Apply Default Outfit when Simple Glamour Switcher reloads", ref applyOnPluginReload);
            }
            

            if (ImGui.CollapsingHeader("Default Appearance Toggles")) {
                using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled))) {
                    ImGui.TextWrapped("Set to have outfits created for this character apply their appearance attributes. Individual outfits can toggle these separately, this only changes the default values when making new outfits.");
                }
                
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
            
            if (ImGui.CollapsingHeader("Default Advanced Appearance Toggles")) {
                using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled))) {
                    ImGui.TextWrapped("Set to have outfits created for this character apply their appearance attributes. Individual outfits can toggle these separately, this only changes the default values when making new outfits.");
                }
                ImGui.Columns(3, "defaultAdvancedAppearanceToggles", false);
                foreach (var ci in Enum.GetValues<AppearanceParameterKind>()) {
                    var v = defaultEnabledParameterKinds.Contains(ci);
                    if (ImGui.Checkbox($"{ci}##defaultEnabledParameters", ref v)) {
                        dirty = true;
                        if (v) {
                            defaultEnabledParameterKinds.Add(ci);
                        } else {
                            defaultEnabledParameterKinds.Remove(ci);
                        }
                    }
                    ImGui.NextColumn();
                }
                
                ImGui.Columns(1);
            }
            
            if (ImGui.CollapsingHeader("Default Equipment Toggles")) {
                using (ImRaii.PushColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled))) {
                    ImGui.TextWrapped("Set to have outfits created for this character apply their equipment. Individual outfits can toggle these separately, this only changes the default values when making new outfits.");
                }
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

            if (ImGui.CollapsingHeader("Other Default Toggles")) {
                ImGui.Columns(2, "defaultToggles", false);
                foreach (var hs in Enum.GetValues<ToggleType>()) {
                    var v = defaultEnabledToggles.Contains(hs);
                    if (ImGui.Checkbox($"{hs}##defaultEnabledToggle", ref v)) {
                        dirty = true;
                        if (v) {
                            defaultEnabledToggles.Add(hs);
                        } else {
                            defaultEnabledToggles.Remove(hs);
                        }
                    }
                    ImGui.NextColumn();
                }
                
                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader("Default Outfit Links")) {
                OutfitLinksEditor ??= new OutfitLinksEditor(Character, linkBefore, linkAfter);
                if (OutfitLinksEditor.Draw($"New Outfits for {Character.Name.OrDefault("This Character")}")) {
                    dirty = true;
                }
            }
            
            var useCustomOutfitPolaroid = outfitStyle != null;
            var useCustomFolderPolaroid = folderStyle != null;
            
            if (ImGui.Checkbox(useCustomOutfitPolaroid ? "##useCustomOutfitPolaroid" : "Use custom outfit style", ref useCustomOutfitPolaroid)) {
                if (useCustomOutfitPolaroid) {
                    outfitStyle = (PluginConfig.CustomStyle?.OutfitList.Polaroid ?? Style.Default.OutfitList.Polaroid).Clone();
                } else {
                    outfitStyle = null;
                }
                dirty = true;
            }

            if (outfitStyle != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Outfit Image Style")) {
                    using (ImRaii.PushIndent()) {
                        dirty |= PolaroidStyle.DrawEditor("Outfit", outfitStyle);
                    }
                }
            }

            if (ImGui.Checkbox(useCustomFolderPolaroid ? "##useCustomFolderPolaroid" : "Use custom folder style", ref useCustomFolderPolaroid)) {
                if (useCustomFolderPolaroid) {
                    folderStyle = (PluginConfig.CustomStyle?.FolderPolaroid ?? Style.Default.FolderPolaroid).Clone();
                } else {
                    folderStyle = null;
                }

                dirty = true;
            }

            if (folderStyle != null) {
                ImGui.SameLine();
                if (ImGui.CollapsingHeader("Custom Folder Image Style")) {
                    using (ImRaii.PushIndent()) {
                        dirty |= PolaroidStyle.DrawEditor("Folder", folderStyle);
                    }
                }
            }
            
            if (PluginConfig.EnableOutfitCommands && ImGui.CollapsingHeader("Commands")) {
                ImGui.TextColoredWrapped(ImGui.GetColorU32(ImGuiCol.TextDisabled), "Execute commands automatically when changing outfits. Commands set here will be executed when any outfit is applied with this character.");
                ImGui.Spacing();
                ImGui.TextDisabled("Before Outfit Commands:");
                using (ImRaii.PushIndent()) {
                    using (ImRaii.PushId("autoCommandBeforeOutfit")) {
                        dirty |= CommandEditor.Show(autoCommandBeforeOutfit, down: autoCommandAfterOutfit);
                    }
                }
                
                ImGui.TextDisabled("After Outfit Commands:");
                using (ImRaii.PushIndent()) {
                    using (ImRaii.PushId("autoCommandAfterOutfit")) {
                        dirty |= CommandEditor.Show(autoCommandAfterOutfit, up: autoCommandBeforeOutfit);
                    }
                }
            }
            
            if (ImGui.CollapsingHeader("Image")) {
                var style = (PluginConfig.CustomStyle ?? Style.Default).CharacterPolaroid;
                ImageEditor.Draw(Character, style, characterName, ref controlFlags);
            }
            
            controlFlags |= a;
            
            using (ImRaii.Disabled(!(dirty || IsNewCharacter))) {
                if (ImGuiExt.ButtonWithIcon(IsNewCharacter ? "Create Character" : "Save Character", FontAwesomeIcon.Save, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                    controlFlags |= WindowControlFlags.PreventClose;
                    Character.Name = characterName;
                    Character.PenumbraCollection = penumbraCollection;
                    Character.HonorificIdentity = honorificIdentity;
                    Character.HeelsIdentity = heelsIdentity;
                    Character.DefaultEnabledCustomizeIndexes = defaultEnabledCustomizeIndexes;
                    Character.DefaultDisabledEquipmentSlots = defaultDisableEquipSlots;
                    Character.DefaultEnabledParameterKinds = defaultEnabledParameterKinds;
                    Character.DefaultEnabledToggles = defaultEnabledToggles;
                    Character.CustomizePlusProfile = customizePlusProfile;
                    Character.OutfitPolaroidStyle = outfitStyle;
                    Character.FolderPolaroidStyle = folderStyle;
                    Character.DefaultLinkBefore = linkBefore;
                    Character.DefaultLinkAfter = linkAfter;

                    autoCommandBeforeOutfit.Cleanup();
                    Character.AutoCommandBeforeOutfit = autoCommandBeforeOutfit;
                    autoCommandAfterOutfit.Cleanup();
                    Character.AutoCommandAfterOutfit = autoCommandAfterOutfit;

                    Character.ApplyOnLogin = applyOnLogin;
                    Character.ApplyOnPluginReload = applyOnPluginReload;
                    
                    Character.Dirty = true;
                    Character.Save(true);
                    
                    if (IsNewCharacter) {
                        

                        var defaultOutfit = OutfitConfigFile.CreateFromLocalPlayer(Character, Guid.Empty, Character.GetOptionsProvider(Guid.Empty));

                        defaultOutfit.Name = Character.Name;

                        Character.TryGetImage(out _, out var path);

                        if (path.Exists) {
                            defaultOutfit.SetImage(path);
                            defaultOutfit.SetImageDetail(Character.ImageDetail);
                        }
                        
                        defaultOutfit.Save(true);

                        Character.Automation.Login = defaultOutfit.Guid;
                        
                        Character.Dirty = true;
                        Character.Save(true);
                        
                        Config.SwitchCharacter(Character.Guid);

                        MainWindow.OpenPage(new GlamourListPage(), true);
                    } else {
                        
                        Character.Dirty = true;
                        Character.Save(true);
                        MainWindow.PopPage();
                    }
                    

                    
                }
            }
        }
        
        ImGui.EndChild();
    }

    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        using (ImRaii.Disabled(dirty && !ImGui.GetIO().KeyShift)) {
            if (ImGuiExt.ButtonWithIcon(IsNewCharacter ? "Cancel" : dirty ? "Revert Changes" : "Cancel", FontAwesomeIcon.Ban, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
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
}