using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditEmotePage(CharacterConfigFile character, Guid folderGuid, EmoteConfigFile? emote) : Page {
    public bool IsNewEmote { get; } = emote == null;
    public EmoteConfigFile Emote { get; } = emote ?? EmoteConfigFile.CreateFromLocalPlayer(character, folderGuid);
    private readonly string folderPath = character.ParseFolderPath(folderGuid);
    private const float SubWindowWidth = 600f;
    private readonly FileDialogManager fileDialogManager = new();
    private string emoteName = emote?.Name ?? string.Empty;
    private string? sortName = emote?.SortName ?? string.Empty;
    private List<AutoCommandEntry> autoCommands = emote?.AutoCommands ?? [];
    private EmoteIdentifier? emoteId;
    private bool dirty;
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNewEmote ? "Creating" : "Editing Emote", shadowed: true);
        ImGuiExt.CenterText(IsNewEmote ? $"New Emote in {folderPath}" : $"{folderPath} / {Emote.Name}", shadowed: true);
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
        emoteId ??= Emote.EmoteIdentifier;
        
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
            dirty |= CustomInput.InputText("Emote Name", ref emoteName, 100, errorMessage: emoteName.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.ReadOnlyInputText("Path", folderPath);
        }
        
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        
        if (ImGui.BeginChild("emote", new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {
            DrawEmote();
            if (ImGui.CollapsingHeader("Image")) {
                var folder = character.Folders.GetValueOrDefault(folderGuid);
                var emoteStyle = folder?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
                ImageEditor.Draw(Emote, emoteStyle, Emote.Name, ref controlFlags);
            }
            
            if (PluginConfig.EnableOutfitCommands && ImGui.CollapsingHeader("Commands")) {
                ImGui.TextColoredWrapped(ImGui.GetColorU32(ImGuiCol.TextDisabled), "Execute commands automatically when changing into this emote.");
                
                ImGui.Spacing();
                
                using (ImRaii.PushId("autoCommands")) {
                    dirty |= CommandEditor.Show(autoCommands);
                }
            }
            
            if (ImGui.CollapsingHeader("Details")) {

                var guid = Emote.Guid.ToString();
                ImGui.InputText("GUID", ref guid, 128, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);

                dirty |= ImGui.InputTextWithHint("Custom Sort Name", emoteName, ref sortName, 128);
            }
            
        }
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));
        
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(subWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon("Save Emote", FontAwesomeIcon.Save, new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                Emote.Name = emoteName;
                Emote.SortName = string.IsNullOrWhiteSpace(sortName) ? null : sortName.Trim();
                Emote.AutoCommands = autoCommands;
                Emote.EmoteIdentifier = emoteId;
                Emote.Save(true);
                MainWindow.PopPage();
            }
        }
    }

    private void DrawEmote() {
        var selectedEmoteName = emoteId == null ? "Not Selected" : emoteId.Name;
        ImGui.Spacing();
        ImGui.Spacing();
        GameIcon.Draw(emoteId?.Icon ?? 0);
        ImGui.SameLine();
        var s = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);
        using (ImRaii.Group()) {
            ImGui.SetNextItemWidth(s.X - s.Y);
            dirty |= CustomInput.Combo("Emote Selection", selectedEmoteName, DrawEmoteSearch, style: Style.Default.Combo with { PadTop = false });
            dirty |= ModListDisplay.Show(Emote, selectedEmoteName);
        }
    }

    private bool DrawEmoteSearch(string arg) {
        foreach (var m in EmoteIdentifier.List) {
            var name = m.Name;
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!(string.IsNullOrWhiteSpace(arg) || name.Contains(arg, StringComparison.CurrentCultureIgnoreCase))) continue;
            if (ImGui.Selectable($"{name}##{m}", m == emoteId)) {
                emoteId = m;
                return true;
            }
            
            ImGui.SameLine();
            ImGui.TextDisabled($"{m}");
        }
        
        return false;
    }
}
