using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;




namespace SimpleGlamourSwitcher.UserInterface.Page;

public class CharacterListPage : Page {
    private Dictionary<Guid, CharacterConfigFile>? characters;

    private bool showHidden;
    
    public CharacterListPage() {
        
        BottomRightButtons.Add(new ButtonInfo(FontAwesomeIcon.PersonCirclePlus, "New Character", () => {
            MainWindow?.OpenPage(new EditCharacterPage(null));
        }) { IsDisabled = () => Objects.LocalPlayer == null, Tooltip = "New Character"} );
        
        LoadCharacters();
    }

    private void LoadCharacters() {
        characters = null;
        CharacterConfigFile.GetCharacterConfigurations(fc => {
            if (PluginConfig.ShowActiveCharacterInCharacterList == false && ActiveCharacter?.Guid == fc.Guid) return false;
            return CharacterConfigFile.Filters.ShowHiddenCharacter(fc);
        }).ContinueWith((c) => {
            characters = c.Result;
        });
    }
    
    public override void Refresh() {
        LoadCharacters();
        base.Refresh();
    }
    
    
    public static void DrawCharacter(Guid guid, CharacterConfigFile characterConfigFile, PolaroidStyle? polaroidStyle = null, Action? contextMenuAdditions = null) {
        var _ = WindowControlFlags.None;
        DrawCharacter(guid, characterConfigFile, polaroidStyle ?? PluginConfig.CustomStyle?.CharacterPolaroid ?? Style.Default.CharacterPolaroid, contextMenuAdditions, ref _);    
    }
    
    public static void DrawCharacter(Guid guid, CharacterConfigFile characterConfig, PolaroidStyle polaroidStyle, Action? contextMenuAdditions, ref WindowControlFlags controlFlags) {
        characterConfig.TryGetImage(out var image);
        if (Polaroid.Button(image, characterConfig.ImageDetail, characterConfig.Name, guid, polaroidStyle with { FrameColour = ActiveCharacter?.Guid == guid ? ImGuiColors.HealerGreen : polaroidStyle.FrameColour})) {
            controlFlags |= WindowControlFlags.PreventClose;
            Config.SwitchCharacter(guid);
            GlamourSystem.ApplyCharacter().ConfigureAwait(false);
            Plugin.MainWindow?.OpenPage(new GlamourListPage(), true);
        }

        if (ImGui.BeginPopupContextItem($"character_{guid}_context")) {
            controlFlags |= WindowControlFlags.PreventClose;
            
            ImGui.Text(characterConfig.Name.OrDefault($"{guid}"));
            ImGui.Separator();


            if (ImGui.MenuItem("Edit Character")) {
                Plugin.MainWindow?.OpenPage(new EditCharacterPage(characterConfig));
            }
            
            if (ImGui.MenuItem(characterConfig.Hidden ? "Un-Hide" : "Hide", ImGui.GetIO().KeyShift)) {
                characterConfig.Hidden = !characterConfig.Hidden;
                characterConfig.Dirty = true;
                characterConfig.Save();
            }

            if (!ImGui.GetIO().KeyShift && ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Hold SHIFT");
            }
            
            if (ImGui.MenuItem("Open in Explorer")) {
                CharacterConfigFile.GetFile(guid).Directory?.OpenInExplorer();
            }
            
            contextMenuAdditions?.Invoke();
            
            ImGui.EndPopup();
        }
    

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right)) {
            controlFlags |= WindowControlFlags.PreventClose;
        }
    }
    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        if (characters == null) {
            ImGuiExt.CenterText("Loading Characters...", centerHorizontally: true, centerVertically: true, shadowed: true);
            return;
        }
        
        if (characters.Count == 0) {
            ImGuiExt.CenterText("No Characters Available", centerHorizontally: true, centerVertically: true, shadowed: true);
            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X / 2 - ImGui.GetItemRectSize().X / 2);
            if (ImGuiExt.ButtonWithIcon("Create Character", FontAwesomeIcon.PersonCirclePlus, ImGui.GetItemRectSize() * Vector2.UnitX + ImGui.GetTextLineHeightWithSpacing() * 2 * Vector2.UnitY)) {
                MainWindow.OpenPage(new EditCharacterPage(null));
            }
            
            return;
        }

        var first = true;
        var polaroidStyle = PluginConfig.CustomStyle?.CharacterPolaroid ?? Style.Default.CharacterPolaroid;
        var polaroidSize = Polaroid.GetActualSize(polaroidStyle);
        
        List<Action> actions = new();
        
        foreach (var (guid, characterConfig) in characters) {
            if (characterConfig.Hidden && !showHidden) continue;
            
            using (ImRaii.PushId($"character_{guid}")) {
                if (!PluginConfig.ShowActiveCharacterInCharacterList && ActiveCharacter?.Guid == guid) continue;
                if (!first) ImGui.SameLine();
                if (!first && ImGui.GetContentRegionAvail().X < polaroidSize.X) ImGui.NewLine();
                first = false;
                DrawCharacter(guid, characterConfig, polaroidStyle, () => {
                    if (ImGui.BeginMenu($"Delete")) {

                        ImGui.Text("Hold SHIFT and ALT to confirm.");

                        if (ImGui.MenuItem("> Confirm Delete <", false, ImGui.GetIO().KeyShift && ImGui.GetIO().KeyAlt)) {
                            if (characterConfig.Delete()) {
                                actions.Add(() => { characters.Remove(guid); });
                            }
                        }

                        ImGui.EndMenu();
                    }
                }, ref controlFlags);
            }
        }

        foreach (var a in actions) a();
    }

    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        if (ImGuiExt.ButtonWithIcon("Cancel", FontAwesomeIcon.CaretLeft, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
            MainWindow.PopPage();
        }
    }

    public override void DrawRight(ref WindowControlFlags controlFlags) {
        ImGui.Checkbox("Show Hidden Characters", ref showHidden);
        base.DrawRight(ref controlFlags);
    }

    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText("Character List", shadowed: true);
    }
}
