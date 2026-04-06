using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Components;
using Lumina.Excel.Sheets;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditMinionPage(CharacterConfigFile character, Guid folderGuid, MinionConfigFile? minion) : EntryEditorPage<MinionConfigFile>(character, folderGuid, minion) {
    public override string TypeName => "Minion";

    private uint? minionId;
    private bool resummon = minion?.Resummon ?? false;

    protected override void DrawEditor(ref WindowControlFlags controlFlags) {
        minionId ??= Entry.MinionId;
        DrawMinion();
    }

    protected override void SaveEntry() {
        Entry.MinionId = minionId ?? Entry.MinionId;
        Entry.Resummon = resummon;
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
            dirty |= ModListDisplay.Show(Entry, $"{minionDataName}");
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
