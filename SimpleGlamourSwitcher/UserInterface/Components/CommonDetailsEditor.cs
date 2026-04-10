using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Enums;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public class CommonDetailsEditor(CharacterConfigFile character, IListEntry entry) {

    private string name = entry?.Name ?? string.Empty;
    public string Name => name;
    private string description =  entry?.Description ?? string.Empty;
    private string sortName = entry?.SortName ?? string.Empty;
    public Guid FolderGuid { get; private set; } = entry?.Folder ?? Guid.Empty;

    public string FolderPath { get; private set; } = character.ParseFolderPath(entry?.Folder ?? Guid.Empty);

    private List<AutoCommandEntry> autoCommands = entry?.AutoCommands ?? [];
    

    public bool ShowNameAndFolderEditors(float width = 0) {
        var dirty = false;
        
        ImGui.Spacing();
        
        var pad = (ImGui.GetContentRegionAvail().X - width * ImGuiHelpers.GlobalScale) / 2f;
        using (ImRaii.Group()) {
            ImGui.Dummy(new Vector2(pad, 1f));
        }

        ImGui.SameLine();
        
        
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(width * ImGuiHelpers.GlobalScale)) {
            dirty |= CustomInput.InputText("Outfit Name", ref name, 100, errorMessage: name.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.Combo("Path", FolderPath, s => {
                var folders = character.Folders.Select(f => {
                    var path = character.ParseFolderPath(f.Key);
                    return (Guid: f.Key, Path: path, CompactPath: character.ParseFolderPath(f.Key, compact: true));
                });

                if (character.Guid != CharacterConfigFile.SharedDataGuid) {
                    folders = folders.Prepend(new ValueTuple<Guid, string, string>(Guid.Empty, character.Name, character.Name));
                }
                
                foreach (var (guid, path, compactPath) in folders.OrderBy(f => f.CompactPath, StringComparer.InvariantCultureIgnoreCase)) {
                    if (!string.IsNullOrWhiteSpace(s)) {
                        if (!(path.Contains(s, StringComparison.CurrentCultureIgnoreCase) || compactPath.Contains(s, StringComparison.CurrentCultureIgnoreCase))) continue;
                    }

                    if (ImGui.Selectable($"{path}", guid == FolderGuid)) {
                        dirty |= FolderGuid != guid;
                        FolderGuid = guid;
                        FolderPath = character.ParseFolderPath(guid);
                    }
                }

                return false;
            }, icon: FontAwesomeIcon.Folder);
        }
                
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        ImGui.Spacing();
        return dirty;
    }

    public bool ShowCommonDetails(ref WindowControlFlags controlFlags) {
        var dirty = false;
        if (entry is IImageProvider imageProvider && ImGui.CollapsingHeader("Image")) {
            var folder = character.Folders.GetValueOrDefault(FolderGuid);
            var outfitStyle = folder?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
            ImageEditor.Draw(imageProvider, outfitStyle, name, ref controlFlags);
        }
            
        if (PluginConfig.EnableOutfitCommands && ImGui.CollapsingHeader("Commands")) {
            ImGui.TextColoredWrapped(ImGui.GetColorU32(ImGuiCol.TextDisabled), "Execute commands automatically when applying this outfit.");
                
            ImGui.Spacing();
                
            using (ImRaii.PushId("autoCommands")) {
                dirty |= CommandEditor.Show(autoCommands);
            }
        }
            
        if (ImGui.CollapsingHeader("Details")) {
            var guid = entry.Guid.ToString();
            ImGui.InputText("GUID", ref guid, 128, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
            dirty |= ImGui.InputTextWithHint("Custom Sort Name", name, ref sortName, 128);
        }
        
        return dirty;
    }

    public void ApplyTo(IListEntry toEntry) {
        toEntry.Name = name;
        toEntry.Folder = FolderGuid;
        toEntry.SortName = string.IsNullOrWhiteSpace(sortName) ? null : sortName.Trim();;
        toEntry.Description = description;
        toEntry.AutoCommands = autoCommands;
        toEntry.Dirty = true;
    }
    
}
