using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public class OutfitLinksEditor(CharacterConfigFile character, OutfitConfigFile? outfit, List<Guid> linkBefore, List<Guid> linkAfter) {

    public OutfitLinksEditor(CharacterConfigFile character, List<Guid> linkBefore, List<Guid> linkAfter) : this(character, null, linkBefore, linkAfter) {
        
    }
    
    
    private readonly LazyAsync<OrderedDictionary<Guid, IListEntry>> otherOutfits = new(character.GetEntries);
    private readonly LazyAsync<OrderedDictionary<Guid, IListEntry>> sharedOutfits = new(() => SharedCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, IListEntry>()) : SharedCharacter.GetEntries());

    [Flags]
    private enum Button : uint  {
        None = 0,
        Delete = 1,
        Up = 2,
        Down = 4
    }

    private Button DrawButtons(Button disabled = Button.None) {
        var clicked = Button.None;
        using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(8, 8))) 
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0))) {
            using (ImRaii.Disabled(disabled.HasFlag(Button.Delete))) {
                if (ImGuiExt.IconButton("delete", FontAwesomeIcon.Trash)) {
                    clicked = Button.Delete;
                }
            }
            
            ImGui.SameLine();
            using (ImRaii.Disabled(disabled.HasFlag(Button.Up))) {
                if (ImGuiExt.IconButton("up", FontAwesomeIcon.ArrowUp)) {
                    clicked = Button.Up;
                }
            }

            ImGui.SameLine();
            using (ImRaii.Disabled(disabled.HasFlag(Button.Down))) {
                if (ImGuiExt.IconButton("down", FontAwesomeIcon.ArrowDown)) {
                    clicked = Button.Down;
                }
            }
        }

        return clicked;
    }


    private string OutfitName(Guid outfitGuid) {
        if (character.Guid != CharacterConfigFile.SharedDataGuid && otherOutfits.IsValueCreated && otherOutfits.Value.TryGetValue(outfitGuid, out var o)) {
            return o.Name;
        }

        if (sharedOutfits.IsValueCreated && sharedOutfits.Value.TryGetValue(outfitGuid, out o)) {
            return $"[Shared] {o.Name}";
        }
        
        return $"{outfitGuid}";
    }
    
    private FontAwesomeIcon OutfitTypeIcon(Guid guid) {
        if (character.Guid != CharacterConfigFile.SharedDataGuid && otherOutfits.IsValueCreated && otherOutfits.Value.TryGetValue(guid, out var o)) {
            return o.TypeIcon;
        }
        
        if (sharedOutfits.IsValueCreated && sharedOutfits.Value.TryGetValue(guid, out o)) {
            return o.TypeIcon;
        }
        
        return FontAwesomeIcon.Question;
    }
    
    
    public bool Draw(string outfitName) {
        otherOutfits.CreateValueIfNotCreated();
        var modified = false;
         using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get())) {
            ImGui.TextWrapped("Outfit links allow other outfits, emotes or minions to be applied before or after an outfit. Emotes and Minions will not be executed or summoned when applied using an outfit link.");
        }


        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 0 })) {
            DrawButtons(Button.Delete | Button.Down | Button.Up);
            ImGui.SameLine();
            var insertBefore = Guid.Empty;
            if (DrawOutfitPicker("##insertBefore", "Add Outfit...", FontAwesomeIcon.Plus, ref insertBefore, outfit?.Guid ?? Guid.Empty) && insertBefore != Guid.Empty) {
                linkBefore.Insert(0, insertBefore);
                modified = true;
            }
        }
   
        
        for (var b = 0; b < linkBefore.Count; b++) {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 0 }))
            using (ImRaii.PushId($"linkBefore_{b}")) {
                var link = linkBefore[b];

                switch (DrawButtons(b == 0 ? Button.Up : Button.None)) {
                    case Button.Up:
                        (linkBefore[b], linkBefore[b - 1]) = (linkBefore[b - 1], linkBefore[b]);
                        modified = true;
                        break;
                    case Button.Down:
                        if (b == linkBefore.Count - 1) {
                            linkAfter.Insert(0, link);
                            linkBefore.RemoveAt(b);
                        } else {
                            (linkBefore[b], linkBefore[b + 1]) = (linkBefore[b + 1], linkBefore[b]);
                        }
                        break;
                    case Button.Delete:
                        linkBefore.RemoveAt(b);
                        b--;
                        modified = true;
                        break;
                }
                
                
                
                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                
                if (DrawOutfitPicker(string.Empty, OutfitName(link), OutfitTypeIcon(link), ref link) && link != Guid.Empty) {
                    linkBefore[b] = link;
                    modified = true;
                }
                
                // CustomInput.ReadOnlyInputText(string.Empty, otherOutfits.Value[link].Name, style: new TextInputStyle() { BorderSize = 2, PadTop = false, FramePadding = new Vector2(16, 8)});
            }
        }


        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 0 })) {
            switch (DrawButtons(Button.Delete | (linkBefore.Count == 0 ? Button.Up : Button.None) | (linkAfter.Count == 0 ? Button.Down : Button.None))) {
                case Button.Up:
                    
                    linkAfter.Insert(0, linkBefore[^1]);
                    linkBefore.RemoveAt(linkBefore.Count - 1);
                    
                    modified = true;
                    break;
                case Button.Down:
                    linkBefore.Add(linkAfter[0]);
                    linkAfter.RemoveAt(0);
                    modified = true;
                break;
            }
            
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            CustomInput.ReadOnlyInputText("##currentOutfit", outfitName, style: new TextInputStyle() { BorderSize = 2, PadTop = false, FramePadding = new Vector2(16, 8), BorderColour = ImGuiColors.DalamudOrange}, icon: FontAwesomeIcon.PersonHalfDress);
        }
        
        for (var a = 0; a < linkAfter.Count; a++) {
            using (ImRaii.PushId($"linkAfter_{a}")) {
                var link = linkAfter[a];

                using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 0 })) {
                    switch (DrawButtons(a >= linkAfter.Count - 1 ? Button.Down : Button.None)) {
                        case Button.Up:
                            if (a > 0) {
                                (linkAfter[a], linkAfter[a - 1]) = (linkAfter[a - 1], linkAfter[a]);
                            } else {
                                linkBefore.Add(link);
                                linkAfter.RemoveAt(a);
                                
                            }
                            modified = true;
                            break;
                        case Button.Down:
                            if (a < linkAfter.Count - 1) {
                                (linkAfter[a], linkAfter[a + 1]) = (linkAfter[a + 1], linkAfter[a]);
                            }
                            modified = true;
                            break;
                        case Button.Delete:
                            linkAfter.RemoveAt(a);
                            a--;
                            modified = true;
                            break;
                    }
                
                    ImGui.SameLine();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                    
                    
                    if (DrawOutfitPicker(string.Empty, OutfitName(link), OutfitTypeIcon(link), ref link, outfit?.Guid ?? Guid.Empty) && link != Guid.Empty) {
                        linkAfter[a] = link;
                        modified = true;
                    }
                }
            }
        }
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 0 })) {
            DrawButtons(Button.Delete | Button.Down | Button.Up);
            ImGui.SameLine();
            var insertAfter = Guid.Empty;
            if (DrawOutfitPicker("##insertAfter", "Add Outfit...", FontAwesomeIcon.Plus, ref insertAfter, outfit?.Guid ?? Guid.Empty) && insertAfter != Guid.Empty) {
                linkAfter.Add(insertAfter);
                modified = true;
            }
        }

        return modified;
    }



    private bool DrawOutfitPicker(string label, string previewText, FontAwesomeIcon previewIcon, ref Guid picked, params Guid[] exclude) {
        var guid = picked;

        bool DrawComboContents(string search) {
            var modified = false;

            if (!otherOutfits.IsValueCreated) return false;


            void ShowEntries(OrderedDictionary<Guid, IListEntry> entries, CharacterConfigFile chr) {
                foreach (var (outfitGuid, o) in entries.OrderBy(kvp => chr.ParseFolderPath(kvp.Value.Folder)).ThenBy(kvp => kvp.Value.SortName)) {
                    var isShared = chr.Guid == CharacterConfigFile.SharedDataGuid;
                    if (isShared) {
                        // Hide Invalid Shared Files
                        if (o.Folder == Guid.Empty) continue;
                        if (!chr.Folders.TryGetValue(o.Folder, out var f)) continue;
                    }
                    
                    if (outfitGuid != guid && (linkAfter.Contains(outfitGuid) || linkBefore.Contains(outfitGuid) || (exclude?.Contains(outfitGuid) ?? false))) continue;
                    if (ImGui.IsWindowAppearing() && guid == outfitGuid) {
                        ImGui.SetScrollHereY(0.5f);
                    }
                    var fullName = chr.ParseFolderPath(o.Folder, isShared) + " / " + o.Name;
                    var fullNameCollapse = string.Join('/', chr.ParseFolderPath(o.Folder, isShared).Split('/', StringSplitOptions.TrimEntries)) + "/" + o.Name;
                    if (!(fullName.Contains(search, StringComparison.InvariantCultureIgnoreCase) || fullNameCollapse.Contains(search, StringComparison.InvariantCultureIgnoreCase))) continue;
                
                    using (ImRaii.Group())
                    using (ImRaii.PushId(o.Guid.ToString())) {
                        using (ImRaii.PushColor(ImGuiCol.Text, ImGuiCol.TextDisabled.Get(), o.Folder != Guid.Empty)) {
                            if (ImGui.Selectable(o.Folder == Guid.Empty ? o.Name : chr.ParseFolderPath(o.Folder, isShared) + " /", guid == outfitGuid)) {
                                guid = outfitGuid;
                                modified = true;
                            }
                        }

                        if (o.Folder != Guid.Empty) {
                            ImGui.SameLine();
                            ImGui.Text(o.Name);
                        }
                    }

                    if (ImGui.IsItemHovered()) {
                        var hasImage = o.TryGetImage(out var wrap);
                        using (ImRaii.Tooltip()) {
                            Polaroid.Draw(hasImage ? wrap : null, o.ImageDetail, o.Name, chr.Folders.GetValueOrDefault(o.Folder)?.OutfitPolaroidStyle ?? chr.OutfitPolaroidStyle);
                        }
                    }
                }
            }
            
            if (character.Guid != CharacterConfigFile.SharedDataGuid) {
                ShowEntries(otherOutfits.Value, character);
            }
            
            if (SharedCharacter != null) {
                ShowEntries(sharedOutfits.Value, SharedCharacter);
            }

            return modified;
        }


        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (CustomInput.Combo(label, previewText, DrawComboContents, style: new ComboStyle() { FramePadding = new Vector2(16, 8), PadTop = false}, icon: previewIcon)) {
            picked = guid;
            return true;
        }
        
        if (ImGui.IsItemHovered() && otherOutfits.IsValueCreated && otherOutfits.Value.TryGetValue(picked, out var o)) {
            var hasImage = o.TryGetImage(out var wrap);
            using (ImRaii.Tooltip()) {
                Polaroid.Draw(hasImage ? wrap : null, o.ImageDetail, o.Name, character.Folders.GetValueOrDefault(o.Folder)?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle);
            }
        }
        

        return false;
    }
    
}
