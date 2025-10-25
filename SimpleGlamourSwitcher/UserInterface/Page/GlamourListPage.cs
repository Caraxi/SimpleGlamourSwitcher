using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using ECommons;
using Dalamud.Bindings.ImGui;
using ECommons.ImGuiMethods;
using Lumina.Excel.Sheets;
using SimpleGlamourSwitcher.Configuration;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class GlamourListPage : Page {
    public readonly Guid ActiveFolder;

    private bool scrollTop;
    private bool showHiddenFolders;

    private bool hideBackButton;

    private FolderSortStrategy folderSortStrategy;
    
    public GlamourListPage(Guid folderGuid = default, bool hideBackButton = false) {
        this.hideBackButton = hideBackButton;
        ActiveFolder = ActiveCharacter == null || !ActiveCharacter.Folders.ContainsKey(folderGuid) ? Guid.Empty : folderGuid;
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.PersonCirclePlus, "Create Outfit", () => {
            if (ActiveCharacter != null) MainWindow?.OpenPage(new EditOutfitPage(ActiveCharacter,  ActiveFolder, null));
        }) { IsDisabled = () => ActiveCharacter == null, Tooltip = "Create New Outfit"} );
        
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.Cat, "Create Minion", () => {
            if (ActiveCharacter != null) MainWindow?.OpenPage(new EditMinionPage(ActiveCharacter,  ActiveFolder, null));
        }) { IsDisabled = () => ActiveCharacter == null, Tooltip = "Create New Minion"} );
        
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.KissWinkHeart, "Create Emote", () => {
            if (ActiveCharacter != null) MainWindow?.OpenPage(new EditEmotePage(ActiveCharacter,  ActiveFolder, null));
        }) { IsDisabled = () => ActiveCharacter == null, Tooltip = "Create New Emote"} );
        
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.FolderPlus, "Create Folder", () => {
            MainWindow?.OpenPage(new EditFolderPage(ActiveFolder, null));
        }) { IsDisabled = () => ActiveCharacter == null, Tooltip = "Create New Folder" } );
       
        LoadOutfits();
    }

    private void LoadOutfits() {
        var character = ActiveCharacter;

        scrollTop = true;
        if (character != null) {
            folderSortStrategy = PluginConfig.FolderSortStrategy;
            folderSortStrategy = character.Folders.TryGetValue(ActiveFolder, out var folder) ? folder.GetFolderSortStrategy() : character.GetFolderSortStrategy();

            Task.Run(() => character.GetEntries(ActiveFolder)).ContinueWith(t => {
                outfits = t.Result;
                scrollTop = true;
            });
        }
    }

    private bool allowContextMenu;
    
    
    public override void Refresh() {
        allowContextMenu = false;
        LoadOutfits();
        base.Refresh();
    }

    private (ItemType Type, Guid Guid)? dragItem;
    
    private OrderedDictionary<Guid, IListEntry>? outfits;

    public override void DrawLeft(ref WindowControlFlags controlFlags) {

        if (ImGuiExt.ButtonWithIcon(ActiveCharacter == null ? "Select Character" : "Switch Character", FontAwesomeIcon.PersonDressBurst, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
            MainWindow?.OpenPage(new CharacterListPage());
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Middle) && ImGui.GetIO().KeyAlt) {
            Config.SwitchCharacter(null, false);
        }

        if (ActiveCharacter == null) {
            ImGuiExt.CenterText("No Character Selected");
        } else {
            ActiveCharacter.TryGetImage(out var image);
            Polaroid.Draw(image, ActiveCharacter.ImageDetail, ActiveCharacter.Name, (PluginConfig.CustomStyle?.CharacterPolaroid ?? Style.Default.CharacterPolaroid).FitTo(ImGui.GetContentRegionAvail()));

            var buttonSize = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2);
            if (ImGuiExt.ButtonWithIcon("Edit Character", FontAwesomeIcon.PencilAlt, buttonSize)) {
                MainWindow?.OpenPage(new EditCharacterPage(ActiveCharacter));
            }
            
            if (ImGuiExt.ButtonWithIcon("Configure Automations", FontAwesomeIcon.Robot, buttonSize)) {
                MainWindow?.OpenPage(new AutomationPage(ActiveCharacter));
            }
            
            if (ImGuiExt.ButtonWithIcon("Open in Explorer", FontAwesomeIcon.FolderTree, buttonSize)) {
                CharacterConfigFile.GetFile(ActiveCharacter.Guid).Directory?.OpenInExplorer();
            }

            using (ImRaii.Disabled(!ImGui.GetIO().KeyShift)) {
                if (ImGuiExt.ButtonWithIcon(
                        "Reapply Automation", FontAwesomeIcon.Undo, buttonSize)) {
                    GlamourSystem.ApplyCharacter().ConfigureAwait(false);
                }
            }

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
                ImGui.BeginTooltip();
                ImGui.Text("Reapply Automation");
                var s = ImGui.GetItemRectSize() * Vector2.UnitX * 1.75f;
                ImGui.Dummy(s);
                ImGui.Separator();
                ImGui.Text("Resets character appearance and outfit to\ntheir game defaults and then applies the\ncharacter's automations as if logging in again.");
                ImGui.Spacing();
                ImGui.TextDisabled("Hold SHIFT to confirm");
                ImGui.EndTooltip();
            }
        }
    }
    
    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {

        var errors = ConfigFile.GetBadFiles<ConfigFile<OutfitConfigFile, CharacterConfigFile>>(ActiveCharacter?.Guid ?? Guid.Empty);

        if (errors.Count > 0) {

            var errListOpen = false;
            using (ImRaii.PushColor(ImGuiCol.Header, 0xAA3333AA)) 
            using (ImRaii.PushColor(ImGuiCol.HeaderActive, 0xBB3333BB)) 
            using (ImRaii.PushColor(ImGuiCol.HeaderHovered, 0xCC3333CC)) {
                errListOpen = ImGui.CollapsingHeader($"{errors.Count} Outfits failed to load###outfitLoadErrorHeader");
            }

            if (errListOpen) {
                using (ImRaii.PushIndent()) {
                    foreach (var (errGuid, ex) in errors.ToArray()) {

                        var file = OutfitConfigFile.GetConfigPath(ActiveCharacter, errGuid);
                        if (!File.Exists(file.FullName)) continue;
                        
                        if (ImGuiComponents.IconButton($"{errGuid}_trashButton", FontAwesomeIcon.Trash) && ImGui.GetIO().KeyShift) {
                            File.Delete(file.FullName);
                            errors.Remove(errGuid);
                        }

                        if (ImGui.IsItemHovered()) {
                            ImGui.BeginTooltip();
                            ImGui.TextUnformatted("Delete Errored File");
                            if (!ImGui.GetIO().KeyShift) {
                                ImGui.TextUnformatted("Hold SHIFT to confirm");
                            }
                            ImGui.EndTooltip();
                        }
                        ImGui.SameLine();
                        if (ImGui.CollapsingHeader($"{errGuid}###outfitLoadErrorHeader_{errGuid}")) {
                            
                            using (ImRaii.PushIndent()) {
                                var fullText = ex.ToStringFull();

                                var size = ImGui.CalcTextSize(fullText);

                                if (ImGui.BeginChild($"stackTrace_{errGuid}_child", new Vector2(ImGui.GetContentRegionAvail().X, size.Y + ImGui.GetStyle().FramePadding.Y * 4 + ImGui.GetStyle().WindowPadding.Y * 2 + ImGui.GetStyle().ScrollbarSize), true, ImGuiWindowFlags.HorizontalScrollbar)) {
                                    ImGui.InputTextMultiline($"##stackTrace_{errGuid}", ref fullText, fullText.Length, size + ImGui.GetStyle().FramePadding * 2, ImGuiInputTextFlags.ReadOnly);
                                }
                                ImGui.EndChild();
                                
                            }
                        }
                    }
                }
               
            }
            
        }
        
        
        
        
        
        
        
        using var child = ImRaii.Child("glamourListScroll", ImGui.GetContentRegionAvail());

        if (scrollTop) {
            scrollTop = false;
            ImGui.SetScrollHereY();
        }
        
        
        
        var character = ActiveCharacter;

        var drag = dragItem;
        if (drag != null) {
            controlFlags |= WindowControlFlags.PreventMove;
        }
        
        if (character == null) {
            ImGuiExt.CenterText("No Character Selected", centerHorizontally: true, centerVertically: true, shadowed: true);
            return;
        }

        if (ActiveFolder == Guid.Empty || !character.Folders.TryGetValue(ActiveFolder, out var folder)) {
            folder = null;
        }
        
        var folderStyle = folder?.FolderPolaroidStyle ?? character.FolderPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).FolderPolaroid;
        var outfitStyle = folder?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
        
        /*
        if (ActiveFolder != Guid.Empty && character.Folders.TryGetValue(ActiveFolder, out var folder)) {
            var parentGuid = Guid.Empty;
            var parentName = character.Name;
            if (folder.Parent != Guid.Empty && character.Folders.TryGetValue(folder.Parent, out var parentFolder)) {
                parentName = parentFolder.Name;
                parentGuid = folder.Parent;
            }
            
            // Parent Folder
            
            
            if (Polaroid.Button(Commom.GetEmbeddedTexture("resources/previousFolder.png").GetWrapOrDefault(), $"[Back] {parentName}", parentGuid == Guid.Empty ? character.Guid : parentGuid, folderStyle)) {
                if (MainWindow?.PreviousPage is GlamourListPage page && page.ActiveFolder == parentGuid) {
                    MainWindow?.PopPage();
                } else {
                    MainWindow?.PopPage();
                    MainWindow?.OpenPage(new GlamourListPage(folder.Parent));
                }
            }
            ImGui.SameLine();
        }
        */
        
        var folders = character.Folders.Where(f => (showHiddenFolders || !f.Value.Hidden) && (f.Value.Parent == ActiveFolder || (ActiveFolder == Guid.Empty && !character.Folders.ContainsKey(f.Value.Parent)))).ToList();

        if (folderSortStrategy == FolderSortStrategy.Alphabetical) {
            folders = folders.OrderBy(kvp => kvp.Value.Name, StringComparer.InvariantCultureIgnoreCase).ToList();
        }
        
        if (ActiveFolder != Guid.Empty && folder != null && hideBackButton == false) {
            var parentGuid = Guid.Empty;
            var parentName = character.Name;
            if (folder.Parent != Guid.Empty && character.Folders.TryGetValue(folder.Parent, out var parentFolder)) {
                parentName = parentFolder.Name;
                parentGuid = folder.Parent;
            }
            
            folders.Insert(0, new KeyValuePair<Guid, CharacterFolder>(parentGuid, new PreviousCharacterFolder() { Name = folderStyle.ImageSize.X < 100 ? "[Back]" : $"[Back] {parentName}", Parent = ActiveFolder}));
        }

        if (folders.Count > 0) {
            foreach (var (folderGuid, characterFolder) in folders) {
                if (characterFolder.Hidden && !showHiddenFolders) continue;
                
                using (ImRaii.PushId(folderGuid.ToString())) {
                    if (ImGui.GetContentRegionAvail().X < Polaroid.GetActualSize(folderStyle).X) ImGui.NewLine();

                    if (drag == null) {
                        
                        if (Polaroid.Button(characterFolder is PreviousCharacterFolder ? PreviousCharacterFolder.GetImage() : CharacterFolder.GetImage(character, folderGuid), characterFolder.ImageDetail, characterFolder.Name, folderGuid, folderStyle)) {

                            if (characterFolder is PreviousCharacterFolder) {
                                if (MainWindow?.PreviousPage is GlamourListPage page && page.ActiveFolder == folderGuid) {
                                    MainWindow?.PopPage();
                                } else {
                                    MainWindow?.PopPage();
                                    MainWindow?.OpenPage(new GlamourListPage(folderGuid));
                                }
                            } else {
                                MainWindow?.OpenPage(new GlamourListPage(folderGuid));
                            }
                        }

                        if (ImGui.IsItemHovered()) {
                            controlFlags |= WindowControlFlags.PreventMove;
                        }
                        
                        if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left) && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 10) && characterFolder is not PreviousCharacterFolder) {
                            dragItem = (ItemType.Folder, folderGuid);
                        }
                        
                        if (allowContextMenu && characterFolder is not PreviousCharacterFolder && ImGui.BeginPopupContextItem($"folder_{folderGuid}_context")) {
                            controlFlags |= WindowControlFlags.PreventClose;

                            using (ImRaii.PushFont(UiBuilder.IconFont)) {
                                ImGui.Text(FontAwesomeIcon.Folder.ToIconString());
                                ImGui.SameLine();
                            }
                            ImGuiExt.CenterText(characterFolder.Name, centerVertically: true, centerHorizontally: false, size: ImGui.GetItemRectSize() + ImGui.GetStyle().ItemSpacing);
                            ImGui.Separator();

                            if (characterFolder is not PreviousCharacterFolder && ImGui.MenuItem("Edit Folder")) {
                                MainWindow?.OpenPage(new EditFolderPage(ActiveFolder, characterFolder));
                            }

                            if (folderSortStrategy == FolderSortStrategy.Manual) {
                                var displayIndex = folders.FindIndex(f => f.Key ==  folderGuid);
                                var offset = folders[0].Value is PreviousCharacterFolder ? 1 : 0;
                                var folderCount = folders.Count(f => f.Value is not PreviousCharacterFolder);
                                displayIndex -= offset;
                                
                                if (ImGui.MenuItem($"Move Up", false, displayIndex > 0)) {
                                    var prevRealIndex = character.Folders.IndexOf(folders[displayIndex + offset - 1].Key);
                                    character.Folders.Remove(folderGuid);
                                    character.Folders.Insert(prevRealIndex,  folderGuid, characterFolder);
                                }
                                
                                if (ImGui.MenuItem($"Move Down", false, displayIndex < folderCount - 1)) {
                                    var nextRealIndex = character.Folders.IndexOf(folders[displayIndex + offset + 1].Key);
                                    character.Folders.Remove(folderGuid);
                                    character.Folders.Insert(nextRealIndex, folderGuid, characterFolder);
                                }
                            }

                            if (characterFolder is not PreviousCharacterFolder && ImGui.MenuItem("Copy Command")) {
                                ImGui.SetClipboardText($"/sgs open {folderGuid}");
                            }

                            if (ImGui.BeginMenu($"Delete")) {
                                
                                ImGui.Text("All contents of the folder will be moved out\nof the folder before it is deleted.\nHold SHIFT and ALT to confirm.");
                                
                                if (ImGui.MenuItem("> Confirm Delete <", false, ImGui.GetIO().KeyShift && ImGui.GetIO().KeyAlt)) {

                                    Task.Run(async () => {
                                        var moveOutfits = await character.GetEntries(folderGuid);
                                        foreach (var o in moveOutfits.Values.Where(o => o.Folder == folderGuid)) {
                                            o.Folder = characterFolder.Parent;
                                            o.Dirty = true;
                                            o.Save();
                                        }

                                        foreach (var f in character.Folders.Values.Where(f => f.Parent == folderGuid)) {
                                            f.Parent = characterFolder.Parent;
                                        }

                                        character.Folders.Remove(folderGuid);
                                        
                                        character.Dirty = true;
                                        character.Save();
                                        
                                        Refresh();



                                    });
                                }
                                
                                ImGui.EndMenu();
                            }

                            ImGui.EndPopup();
                        }
                        
                        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                            controlFlags |= WindowControlFlags.PreventClose;
                        }
                        
                    } else {
                        var dragging = drag.Value;
                        
                        Polaroid.Button(characterFolder is PreviousCharacterFolder ? PreviousCharacterFolder.GetImage() : CharacterFolder.GetImage(character, folderGuid), characterFolder.ImageDetail, characterFolder.Name, folderGuid, folderStyle with {
                            FrameColour = folderGuid == dragging.Guid && dragging.Type == ItemType.Folder ? (0x8040FFFF) : (0x40FFFFFF & folderStyle.FrameColour),
                            BlankImageColour = 0x40FFFFFF & folderStyle.BlankImageColour,
                            FrameHoveredColour = folderGuid == dragging.Guid && dragging.Type == ItemType.Folder ? (0x8040FFFF) : folderStyle.FrameColour,
                            FrameActiveColour = folderGuid == dragging.Guid && dragging.Type == ItemType.Folder ? (0x8040FFFF) : folderStyle.FrameColour
                        });

                        if (ImGui.IsItemHovered() && ImGui.IsMouseReleased(ImGuiMouseButton.Left) && dragging.Guid != folderGuid) {
                            
                            switch (dragging.Type) {
                                case ItemType.Folder:
                                    if (character.Folders.TryGetValue(dragging.Guid, out var dragFolder)) {
                                        dragFolder.Parent = folderGuid;
                                        character.Dirty = true;
                                    }
                                    break;
                                case ItemType.Outfit:
                                    if (outfits?.TryGetValue(dragging.Guid, out var dragFile) ?? false) {
                                        dragFile.Folder = folderGuid;
                                        dragFile.Dirty = true;
                                        dragFile.Save();
                                        Refresh();
                                    }
                                    break;
                                default:
                                    PluginLog.Warning($"Cannot move {dragging.Type} into folder.");
                                    break;
                            }
                            
                            dragItem = null;
                        }
                        
                    }
                    
                    ImGui.SameLine();
                }
            }
            ImGui.NewLine();
        }
        
        if (outfits == null) {
            ImGuiExt.CenterText("Loading Outfits...");
        } else {

           
            foreach (var (outfitGuid, entry) in outfits) {
                if (ImGui.GetContentRegionAvail().X < Polaroid.GetActualSize(outfitStyle).X) ImGui.NewLine();
                
                if (drag == null) {
                    if (Polaroid.Button((entry as IImageProvider).GetImage(), entry.ImageDetail, entry.Name, outfitGuid, outfitStyle with { FrameColour = GetOutfitFrameColour(outfitStyle, character, entry) })) {
                        entry.Apply().ConfigureAwait(false);
                        if (PluginConfig.AutoCloseAfterApplying) {
                            MainWindow!.IsOpen = false;
                        }
                    }

                    if (ImGui.IsItemHovered()) {
                        controlFlags |= WindowControlFlags.PreventMove;
                    }

                    if (!entry.IsValid) {
                        using (PluginService.PluginUi.IconFontHandle.Push()) {
                            ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() + outfitStyle.FramePadding * 2, FontAwesomeIcon.ExclamationTriangle.ToIconString(), new ShadowTextStyle() { ShadowColour = 0x80000000, TextColour = ImGuiColors.DalamudRed });
                        }

                        if (ImGui.IsItemHovered()) {
                            using (ImRaii.Tooltip()) {
                                ImGui.Text("Issues Detected");
                                ImGui.Separator();
                                foreach (var err in entry.ValidationErrors) {
                                    ImGui.Text($" - {err}");
                                }
                            }
                        }
                    }
                    else {
                        using (PluginService.PluginUi.IconFontHandle.Push()) {
                            ImGui.GetWindowDrawList().AddShadowedText(ImGui.GetItemRectMin() + outfitStyle.FramePadding * 2, entry.TypeIcon.ToIconString(), new ShadowTextStyle() { ShadowColour = 0x80000000, TextColour = 0xFFFFFFFF });
                        }
                    }
                    
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDown(ImGuiMouseButton.Left) && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 10)) {
                        dragItem = (ItemType.Outfit, outfitGuid);
                    }
                    
                    
                    if (allowContextMenu && ImGui.BeginPopupContextItem($"outfit_{outfitGuid}_context")) {
                        controlFlags |= WindowControlFlags.PreventClose;

                        using (ImRaii.PushFont(UiBuilder.IconFont)) {
                            
                            ImGui.Text(entry.TypeIcon.ToIconString());
                            ImGui.SameLine();
                        }
                        ImGuiExt.CenterText(entry.Name, centerVertically: true, centerHorizontally: false, size: ImGui.GetItemRectSize() + ImGui.GetStyle().ItemSpacing);
                        ImGui.Separator();


                        if (entry is OutfitConfigFile && ImGui.BeginMenu("Automation")) {

                            bool AutomationOption(string name, ref Guid? setValue) {
                                if (setValue == outfitGuid) {
                                    if (ImGui.MenuItem($"Remove as {name} outfit")) {
                                        setValue = null;
                                        return true;
                                    }
                                } else {
                                    if (ImGui.MenuItem($"Set as {name} outfit")) {
                                        setValue = outfitGuid;
                                        return true;
                                    }
                                }

                                return false;
                            }
                            
                            character.Dirty |= AutomationOption("Login", ref character.Automation.Login);
                            character.Dirty |= AutomationOption("Character Switch", ref character.Automation.CharacterSwitch);

                            var activeGearset = GameHelper.GetActiveGearset();
                            if (activeGearset != null) {
                                var gearsetAutomation = character.Automation.Gearsets.GetValueOrDefault(activeGearset.Value.Id, null);
                                if (AutomationOption($"'{activeGearset.Value.Name}' Gearset", ref gearsetAutomation)) {
                                    if (gearsetAutomation == null) {
                                        character.Automation.Gearsets.Remove(activeGearset.Value.Id);
                                    } else {
                                        character.Automation.Gearsets[activeGearset.Value.Id] = gearsetAutomation;
                                    }
                                    character.Dirty = true;
                                }
                            }
                            
                            character.Dirty |= AutomationOption("default Gearset", ref character.Automation.DefaultGearset);
                            
                            ImGui.EndMenu();
                        }
                        
                        if (entry is OutfitConfigFile outfit)
                        {
                            if (ImGui.MenuItem("Edit Outfit")) {
                            
                                MainWindow?.OpenPage(new EditOutfitPage(character, ActiveFolder, outfit));
                            }
                        
                            if (ImGui.MenuItem("Clone Outfit")) {
                                outfit.CreateClone().ContinueWith(task => {
                                    if (task.IsCompletedSuccessfully) {
                                        MainWindow?.OpenPage(new EditOutfitPage(character, ActiveFolder, task.Result));
                                    }
                                });
                            }
                        }
                        
                        if (entry is MinionConfigFile minion)
                        {
                            if (ImGui.MenuItem("Edit Minion")) {
                            
                                MainWindow?.OpenPage(new EditMinionPage(character, ActiveFolder, minion));
                            }
                        
                            if (ImGui.MenuItem("Clone Minion")) {
                                minion.CreateClone().ContinueWith(task => {
                                    if (task.IsCompletedSuccessfully) {
                                        MainWindow?.OpenPage(new EditMinionPage(character, ActiveFolder, task.Result));
                                    }
                                });
                            }
                        }
                        
                        if (entry is EmoteConfigFile emote)
                        {
                            if (ImGui.MenuItem("Edit Emote")) {
                                MainWindow?.OpenPage(new EditEmotePage(character, ActiveFolder, emote));
                            }
                        
                            if (ImGui.MenuItem("Clone Emote")) {
                                emote.CreateClone().ContinueWith(task => {
                                    if (task.IsCompletedSuccessfully) {
                                        MainWindow?.OpenPage(new EditEmotePage(character, ActiveFolder, task.Result));
                                    }
                                });
                            }
                        }
                        
                        if (ImGui.MenuItem("Copy Command")) {
                            ImGui.SetClipboardText($"/sgs apply {outfitGuid}");
                        }
                        
                        if (ImGui.MenuItem("Open File")) {
                            var file = OutfitConfigFile.GetConfigPath(character, outfitGuid);
                            file.OpenWithDefaultApplication();
                        }
                        
                        if (ImGui.BeginMenu($"Delete")) {
                            ImGui.Text("Hold SHIFT and ALT to confirm.");
                            if (ImGui.MenuItem("> Confirm Delete <", false, ImGui.GetIO().KeyShift && ImGui.GetIO().KeyAlt)) {
                                PluginLog.Debug("Try Delete?");
                                Task.Run(() => {
                                    try {
                                        entry.GetImageFile()?.Delete();
                                        entry.Delete();
                                    } catch (Exception ex) {
                                        PluginLog.Error(ex, "Error deleting file.");
                                    }
                                   
                                    Refresh();
                                });
                            }
                                
                            ImGui.EndMenu();
                        }
                        
                        ImGui.EndPopup();
                    }
                        
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
                        controlFlags |= WindowControlFlags.PreventClose;
                    }
                    
                    
                } else {
                    var dragging = drag.Value;
                    Polaroid.Draw((entry as IImageProvider).GetImage(), entry.ImageDetail, entry.Name, outfitStyle with {
                        FrameColour = outfitGuid == dragging.Guid && dragging.Type == ItemType.Outfit ? (0x8040FFFF) : (0x40FFFFFF & outfitStyle.FrameColour),
                        BlankImageColour = 0x40FFFFFF & outfitStyle.BlankImageColour
                    });
                }
                
                
                
                
                ImGui.SameLine();
                
                
                
            }
        }
        
        if (dragItem != null) {
            using (ImRaii.PushColor(ImGuiCol.PopupBg, 0)) {
          
                ImGui.BeginTooltip();

                switch (dragItem.Value.Type) {
                    case ItemType.Folder:
                        if (character.Folders.TryGetValue(dragItem.Value.Guid, out var dragFolder)) {
                            Polaroid.Draw(CharacterFolder.GetImage(character, dragItem.Value.Guid), dragFolder.ImageDetail, dragFolder.Name, folderStyle with {ImageSize = folderStyle.ImageSize.FitTo(72)});
                        }
                        break;
                    case ItemType.Outfit:
                        if (outfits?.TryGetValue(dragItem.Value.Guid, out var dragOutfit) ?? false) {
                            if (dragOutfit is IImageProvider imgProvider) {
                                Polaroid.Draw(imgProvider.GetImage(), dragOutfit.ImageDetail, dragOutfit.Name, outfitStyle with { ImageSize = outfitStyle.ImageSize.FitTo(72) });
                            }
                        }
                        break;
                    default:
                        ImGui.GetForegroundDrawList().AddRectFilled(ImGui.GetMousePos() - new Vector2(50, 100), ImGui.GetMousePos() + new Vector2(50, 0), 0x80FFFFFF, 8f, ImDrawFlags.RoundCornersAll);
                        ImGui.GetForegroundDrawList().AddRect(ImGui.GetMousePos() - new Vector2(50, 100), ImGui.GetMousePos() + new Vector2(50, 0), 0x80FFFFFF, 8f, ImDrawFlags.RoundCornersAll, 2);
                        break;
                }
                
                
            ImGui.EndTooltip();
            }
            
            if (!ImGui.IsMouseDown(ImGuiMouseButton.Left)) {
                dragItem = null;
            }
        }


        if (!allowContextMenu && !ImGui.IsMouseDown(ImGuiMouseButton.Right)) {
            allowContextMenu = true;
        }
        
        base.DrawCenter(ref controlFlags);
    }

    private Colour GetOutfitFrameColour(PolaroidStyle? polaroidStyle, CharacterConfigFile character, IListEntry outfit) {
        var style = PluginConfig.CustomStyle ?? Style.Default;
        var anyAutomation = character.Automation.Login == outfit.Guid || character.Automation.CharacterSwitch == outfit.Guid || character.Automation.DefaultGearset == outfit.Guid;

        if (!anyAutomation) {
            var activeGearset = GameHelper.GetActiveGearset();
            anyAutomation |= activeGearset != null && character.Automation.Gearsets.TryGetValue(activeGearset.Value.Id, out var gearsetAutomation) && gearsetAutomation == outfit.Guid;
        }
        
        
        return anyAutomation ? style.OutfitList.DefaultOutfitColour : polaroidStyle?.FrameColour ?? PolaroidStyle.Default.FrameColour;
    }

    public override void DrawRight(ref WindowControlFlags controlFlags) {

        ImGui.Checkbox("Show Hidden Folders", ref showHiddenFolders);
        
        
        base.DrawRight(ref controlFlags);
    }

    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText("Glamour List", shadowed: true);
        if (ActiveFolder != Guid.Empty && ActiveCharacter != null) {
            var path = ActiveCharacter.ParseFolderPath(ActiveFolder, false);
            ImGuiExt.CenterText(path, shadowed: true);
        }
        
    }
}
