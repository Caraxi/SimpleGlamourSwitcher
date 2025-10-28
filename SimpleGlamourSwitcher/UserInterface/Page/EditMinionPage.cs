using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Components;
using Lumina.Excel.Sheets;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditMinionPage(CharacterConfigFile character, Guid folderGuid, MinionConfigFile? minion) : Page {
    
    public bool IsNewMinion { get; } = minion == null;
    public MinionConfigFile Minion { get; } = minion ?? MinionConfigFile.CreateFromLocalPlayer(character, folderGuid);

    private readonly string folderPath = character.ParseFolderPath(folderGuid);
    private const float SubWindowWidth = 600f;

    private readonly FileDialogManager fileDialogManager = new();


    private string minionName = minion?.Name ?? string.Empty;
    private string? sortName = minion?.SortName ?? string.Empty;
    
    private List<AutoCommandEntry> autoCommands = minion?.AutoCommands ?? [];

    private uint? minionId;
    private bool resummon = minion?.Resummon ?? false;
    
    
    private bool dirty;
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNewMinion ? "Creating" : "Editing Minion", shadowed: true);
        ImGuiExt.CenterText(IsNewMinion ? $"New Minion in {folderPath}" : $"{folderPath} / {Minion.Name}", shadowed: true);
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
        minionId ??= Minion.MinionId;
        
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
            dirty |= CustomInput.InputText("Minion Name", ref minionName, 100, errorMessage: minionName.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.ReadOnlyInputText("Path", folderPath);
        }
        
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        
        if (ImGui.BeginChild("minion", new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {


            DrawMinion();
            
            
            
            if (ImGui.CollapsingHeader("Image")) {
                var folder = character.Folders.GetValueOrDefault(folderGuid);
                var minionStyle = folder?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
                ImageEditor.Draw(Minion, minionStyle, Minion.Name, ref controlFlags);
            }
            
            if (PluginConfig.EnableOutfitCommands && ImGui.CollapsingHeader("Commands")) {
                ImGui.TextColoredWrapped(ImGui.GetColorU32(ImGuiCol.TextDisabled), "Execute commands automatically when changing into this minion.");
                
                ImGui.Spacing();
                
                using (ImRaii.PushId("autoCommands")) {
                    dirty |= CommandEditor.Show(autoCommands);
                }
            }
            
            if (ImGui.CollapsingHeader("Details")) {
                var guid = Minion.Guid.ToString();
                ImGui.InputText("GUID", ref guid, 128, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
                dirty |= ImGui.InputTextWithHint("Custom Sort Name", minionName, ref sortName, 128);
            }
            
        }
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));
        
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(subWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon("Save Minion", FontAwesomeIcon.Save, new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                Minion.Name = minionName;
                Minion.SortName = string.IsNullOrWhiteSpace(sortName) ? null : sortName.Trim();
                Minion.AutoCommands = autoCommands;
                Minion.MinionId = minionId ?? 0;
                Minion.Resummon = resummon;
                Minion.Save(true);
                MainWindow.PopPage();
            }
        }
    }

    private void DrawMinion() {
        var minionData = DataManager.GetExcelSheet<Companion>().GetRowOrDefault(minionId ?? 0);
        var minionDataName = minionId is null or 0 ? "Not Selected" : SeStringEvaluator.EvaluateObjStr(ObjectKind.Companion, minionId.Value);
        ImGui.Spacing();
        ImGui.Spacing();
        GameIcon.Draw(minionData?.Icon ?? 0);
        ImGui.SameLine();
        var s = new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);
        using (ImRaii.Group()) {
            ImGui.SetNextItemWidth(s.X - s.Y);
            dirty |= CustomInput.Combo("Minion Selection", minionDataName, DrawMinionSearch, style: Style.Default.Combo with { PadTop = false });
            dirty |= ModListDisplay.Show(Minion, $"{minionDataName}");
        }
        
        ImGui.Checkbox("Resummon Minion", ref resummon);
        
        ImGui.SameLine();
        ImGuiComponents.HelpMarker("If enabled, the minion will be resummoned if it was already summoned.\nHolding SHIFT when selecting this minion will also force a resummon.");
    }

    private bool DrawMinionSearch(string arg) {
        foreach (var m in DataManager.GetExcelSheet<Companion>()) {
            var name = SeStringEvaluator.EvaluateObjStr(ObjectKind.Companion, m.RowId);
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!(string.IsNullOrWhiteSpace(arg) || name.Contains(arg, StringComparison.CurrentCultureIgnoreCase))) continue;
            if (ImGui.Selectable(name, m.RowId == minionId)) {
                minionId = m.RowId;
                return true;
            }
        }
        
        return false;
    }
}
