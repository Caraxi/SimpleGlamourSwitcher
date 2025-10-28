using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class CommandEditor {
    public static void Cleanup(this List<AutoCommandEntry> list) {
        list.RemoveAll(e => string.IsNullOrWhiteSpace(e.Command));
        list.ForEach(e => e.Command = e.Command.Trim());
    }
    
    
    [Flags]
    private enum Button : uint  {
        None = 0,
        Delete = 1,
        Up = 2,
        Down = 4
    }
    
    private static Button DrawButtons(Button disabled = Button.None) {
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
    
    private static string _newCommandInput = string.Empty;
    private static readonly TextInputStyle TextInputStyle = new() { FramePadding = new Vector2(16, 8), PadTop = false };
    
    public static bool Show(List<AutoCommandEntry> commandList, List<AutoCommandEntry>? up = null, List<AutoCommandEntry>? down = null) {
        var modified = false;
        var deleteIndex = -1;
        var swapIndex = (-1, -1);
        for (var i = 0; i < commandList.Count; i++) {
            var entry = commandList[i];
            using (ImRaii.PushId(i)) {
                switch (DrawButtons((i <= 0 && up == null ? Button.Up : Button.None) | (i >= commandList.Count - 1 && down == null ? Button.Down :  Button.None))) {
                    case Button.Up:
                        if (i > 0) {
                            swapIndex = (i, i - 1);
                        } else if (up != null) {
                            deleteIndex = i;
                            up.Add(entry);
                        }
                        break;
                    case Button.Delete:
                        deleteIndex = i;
                        break;
                    case Button.Down:
                        if (i < commandList.Count - 1) {
                            swapIndex = (i, i + 1);
                        } else if (down != null) {
                            deleteIndex = i;
                            down.Insert(0, entry);
                        }
                        
                        break;
                }
                
                ImGuiExt.SameLineNoSpace();
                using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(8, 8)))
                using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0))) {
                    if (ImGui.Checkbox("##enableCheckbox", ref entry.Enabled)) {
                        modified = true;
                    }
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Enable Command");

                ImGuiExt.SameLineNoSpace();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                if (CustomInput.InputText("", ref entry.Command, 512, style: TextInputStyle with { TextColour = ImGui.GetColorU32(entry.Enabled ? ImGuiCol.Text : ImGuiCol.TextDisabled) })) {
                    modified = true;
                }
                
                if (string.IsNullOrWhiteSpace(entry.Command) && !ImGui.IsAnyItemActive()) {
                    deleteIndex = i;
                }
                
            }
        }
        
        using (ImRaii.PushId(commandList.Count)) {
            DrawButtons(Button.Up | Button.Down | Button.Delete);
            ImGuiExt.SameLineNoSpace();
            using (ImRaii.Disabled())
            using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(8, 8)))
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(0, 0))) {
                ImGuiExt.IconButton("add", FontAwesomeIcon.Plus);
            }

            ImGuiExt.SameLineNoSpace();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            if (CustomInput.InputText("", ref _newCommandInput, 512, style: TextInputStyle)) {
                if (!string.IsNullOrWhiteSpace(_newCommandInput)) {
                    deleteIndex = -1;
                    commandList.Add(new AutoCommandEntry() { Command = _newCommandInput });
                    _newCommandInput = string.Empty;
                    modified = true;
                }
            }
        }
        
        if (swapIndex is { Item1: >= 0, Item2: >= 0 } && swapIndex.Item1 != swapIndex.Item2) {
            (commandList[swapIndex.Item1], commandList[swapIndex.Item2]) = (commandList[swapIndex.Item2], commandList[swapIndex.Item1]);
        }
        
        if (deleteIndex >= 0 && !ImGui.IsAnyItemActive()) {
            commandList.RemoveAt(deleteIndex);
            modified = true;
        }
        
        return modified;
    }
}
