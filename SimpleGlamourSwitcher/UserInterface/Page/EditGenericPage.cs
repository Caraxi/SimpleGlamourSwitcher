using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditGenericPage(CharacterConfigFile character, Guid folderGuid, GenericEntryConfigFile? entry) : EntryEditorPage<GenericEntryConfigFile>(character, folderGuid, entry) {
    public override string TypeName => "Generic Entry";

    private string? identifier;
    
    protected override void DrawEditor(ref WindowControlFlags controlFlags) {
        identifier ??= Entry.Identifier;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        dirty |= CustomInput.InputText("Identifier", ref identifier, 32);
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One)) {
            dirty |= ModListDisplay.Show(Entry, $"{CommonDetailsEditor.Name}", ImGui.GetContentRegionAvail().X);
        }
    }

    protected override void SaveEntry() {
        Entry.Identifier = identifier ?? "Generic";
    }
}
