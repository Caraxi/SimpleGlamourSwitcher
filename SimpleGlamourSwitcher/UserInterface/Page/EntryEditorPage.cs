using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public abstract class EntryEditorPage<T>(CharacterConfigFile character, Guid folderGuid, T? entry) : Page where T : ICreatableListEntry<T>, IListEntry {

    protected CharacterConfigFile Character => character;
    protected Guid FolderGuid => commonDetailsEditor?.FolderGuid ?? folderGuid;
    private CommonDetailsEditor? commonDetailsEditor;

    protected CommonDetailsEditor CommonDetailsEditor {
        get {
            return commonDetailsEditor ??= new CommonDetailsEditor(character, Entry);
        }
    }
    

    protected virtual float SubWindowWidth => 600;

    public bool IsNew { get; protected set; }
    
    public abstract string TypeName { get; }

    protected T Entry { get; } = entry ?? T.CreateFromLocalPlayer(character, folderGuid, character.GetOptionsProvider(folderGuid));
    
    private readonly FileDialogManager fileDialogManager = new();

    protected bool Dirty;

    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNew ? "Creating" : $"Editing {TypeName}", shadowed: true);
        ImGuiExt.CenterText(IsNew ? $"New {TypeName} in {commonDetailsEditor?.FolderPath}" : $"{commonDetailsEditor?.FolderPath} / {Entry.Name}", shadowed: true);
    }

    
    
    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        using (ImRaii.Disabled(Dirty && !ImGui.GetIO().KeyShift)) {
            if (ImGuiExt.ButtonWithIcon(Dirty ? "Discard Changes": "Back", FontAwesomeIcon.CaretLeft, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                MainWindow.PopPage();
            }
        }
        
#if DEBUG
        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled) && ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
            Dirty = false;
        }
#endif

        if (Dirty && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) {
            ImGui.SetTooltip("Hold SHIFT to confirm.");
        }
    }

    protected abstract void DrawEditor(ref WindowControlFlags controlFlags);
    
    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        commonDetailsEditor ??= new CommonDetailsEditor(character, Entry);
        fileDialogManager.Draw();
        controlFlags |= WindowControlFlags.PreventClose;
        
        var pad = (ImGui.GetContentRegionAvail().X - SubWindowWidth * ImGuiHelpers.GlobalScale) / 2f;
        
        Dirty |= commonDetailsEditor.ShowNameAndFolderEditors(SubWindowWidth);
        
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        
        using (ImRaii.Child("entryEditor", new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {        
            DrawEditor(ref controlFlags);
            CommonDetailsEditor.ShowCommonDetails(ref controlFlags);
        }
        
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(SubWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon($"Save {TypeName}", FontAwesomeIcon.Save, new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                commonDetailsEditor.ApplyTo(Entry);
                SaveEntry();
                Entry.Save(true);
                MainWindow.PopPage();
            }
        }
    }

    protected abstract void SaveEntry();
}
