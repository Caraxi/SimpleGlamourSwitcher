using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditGenericPage(CharacterConfigFile character, Guid folderGuid, GenericEntryConfigFile? entry) : Page {
    
    public bool IsNewGenericEntry { get; } = entry == null;
    public GenericEntryConfigFile GenericEntry { get; } = entry ?? GenericEntryConfigFile.Create(character, folderGuid);

    private readonly string folderPath = character.ParseFolderPath(folderGuid);
    private const float SubWindowWidth = 600f;

    private readonly FileDialogManager fileDialogManager = new();


    private string genericEntryName = entry?.Name ?? string.Empty;
    private string? sortName = entry?.SortName ?? string.Empty;
    
    private List<AutoCommandEntry> autoCommands = entry?.AutoCommands ?? [];

    private string? identifier;
    
    
    private bool dirty;
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNewGenericEntry ? "Creating" : "Editing Generic Entry", shadowed: true);
        ImGuiExt.CenterText(IsNewGenericEntry ? $"New Generic Entry in {folderPath}" : $"{folderPath} / {GenericEntry.Name}", shadowed: true);
    }
    
    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        using (ImRaii.Disabled(dirty && !ImGui.GetIO().KeyShift)) {
            if (ImGuiExt.ButtonWithIcon(dirty ? "Discard Changes": "Back", FontAwesomeIcon.CaretLeft, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                MainWindow.PopPage();
            }
        }
        
        #if DEBUG
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
            dirty = false;
        }
        #endif

        if (dirty && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Hold SHIFT to confirm.");
        }
    }


    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        identifier ??= entry?.Identifier ?? "Generic";
        fileDialogManager.Draw();
        controlFlags |= WindowControlFlags.PreventClose;
        ImGui.Spacing();
        
        var subWindowWidth = MathF.Min(SubWindowWidth, ImGui.GetContentRegionAvail().X);
        var pad = (ImGui.GetContentRegionAvail().X - subWindowWidth * ImGuiHelpers.GlobalScale) / 2f;
        if (subWindowWidth >= SubWindowWidth) {
            using (ImRaii.Group()) {
                ImGui.Dummy(new Vector2(pad, 1f));
            }
            ImGui.SameLine();
        }
        
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(subWindowWidth * ImGuiHelpers.GlobalScale)) {
            dirty |= CustomInput.InputText("Name", ref genericEntryName, 100, errorMessage: genericEntryName.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.ReadOnlyInputText("Path", folderPath);
        }
        
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        
        if (ImGui.BeginChild("genericEntry", new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {
            ImGuiExt.TextDisabledWrapped("'Generic Entry' can be used to apply a set of mods without being directly associated with other outfits. Generic entries that share the same Identifier will disable each other's mods.");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            dirty |= CustomInput.InputText("Identifier", ref identifier, 32);

            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One)) {
                dirty |= ModListDisplay.Show(GenericEntry, $"{genericEntryName}", ImGui.GetContentRegionAvail().X);
            }
            
            if (ImGui.CollapsingHeader("Image")) {
                var folder = character.Folders.GetValueOrDefault(folderGuid);
                var displayStyle = folder?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
                ImageEditor.Draw(GenericEntry, displayStyle, GenericEntry.Name, ref controlFlags);
            }
            
            if (PluginConfig.EnableOutfitCommands && ImGui.CollapsingHeader("Commands")) {
                ImGui.TextColoredWrapped(ImGui.GetColorU32(ImGuiCol.TextDisabled), "Execute commands automatically when apply this entry.");
                
                ImGui.Spacing();
                
                using (ImRaii.PushId("autoCommands")) {
                    dirty |= CommandEditor.Show(autoCommands);
                }
            }
            
            if (ImGui.CollapsingHeader("Details")) {
                var guid = GenericEntry.Guid.ToString();
                ImGui.InputText("GUID", ref guid, 128, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
                sortName ??= genericEntryName;
                dirty |= ImGui.InputTextWithHint("Custom Sort Name", genericEntryName, ref sortName, 128);
            }
            
        }
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));
        
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(subWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon("Save", FontAwesomeIcon.Save, new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                GenericEntry.Name = genericEntryName;
                GenericEntry.SortName = string.IsNullOrWhiteSpace(sortName) ? null : sortName.Trim();
                GenericEntry.AutoCommands = autoCommands;
                GenericEntry.Identifier = identifier;
                
                GenericEntry.Save(true);
                MainWindow.PopPage();
            }
        }
    }
    
}
