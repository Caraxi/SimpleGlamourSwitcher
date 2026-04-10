using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditEmotePage(CharacterConfigFile character, Guid folderGuid, EmoteConfigFile? emote) : EntryEditorPage<EmoteConfigFile>(character, folderGuid, emote), IHasModConfigs {
    public override string TypeName => "Emote";

    private EmoteIdentifier? emoteId;
    private List<OutfitModConfig>? modConfigs;

    public List<OutfitModConfig> ModConfigs {
        get {
            modConfigs ??= Entry.ModConfigs.Clone();
            return modConfigs;
        } 
        set =>  modConfigs = value;
    }

    protected override void DrawEditor(ref WindowControlFlags controlFlags) {
        emoteId ??= Entry.EmoteIdentifier;
        modConfigs ??= Entry.ModConfigs.Clone();
        DrawEmote();
    }

    protected override void SaveEntry() {
        Entry.EmoteIdentifier = emoteId ?? Entry.EmoteIdentifier;
        Entry.ModConfigs = ModConfigs;
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
            Dirty |= CustomInput.Combo("Emote Selection", selectedEmoteName, DrawEmoteSearch, style: Style.Default.Combo with { PadTop = false });
            Dirty |= ModListDisplay.Show(this, selectedEmoteName);
        }
    }

    private bool DrawEmoteSearch(string arg) {
        foreach (var m in EmoteIdentifier.List) {
            var name = m.Name;
            if (string.IsNullOrWhiteSpace(name)) continue;
            if (!(string.IsNullOrWhiteSpace(arg) || name.Contains(arg, StringComparison.CurrentCultureIgnoreCase))) continue;
            if (ImGui.Selectable($"{name}##{m}", m == emoteId)) {
                emoteId = m;
                
                var activeCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
                if (activeCollection.ObjectValid) {
                    modConfigs = OutfitModConfig.GetModListFromEmote(m, activeCollection.EffectiveCollection.Id);
                }
                
                return true;
            }
            
            ImGui.SameLine();
            ImGui.TextDisabled($"{m}");
        }
        
        return false;
    }
}
