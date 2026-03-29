using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Components;
using FFXIVClientStructs.FFXIV.Client.Graphics.Scene;
using ImSharp;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditItemPage(CharacterConfigFile character, Guid folderGuid, ItemConfigFile? item) : Page {
    
    public bool IsNewItem { get; } = item == null;
    public ItemConfigFile Item { get; } = item ?? ItemConfigFile.Create(character, folderGuid);

    private readonly string folderPath = character.ParseFolderPath(folderGuid);
    private const float SubWindowWidth = 600f;

    private readonly FileDialogManager fileDialogManager = new();


    private string itemName = item?.Name ?? string.Empty;
    private string? sortName = item?.SortName ?? string.Empty;

    private ApplicableItem<HumanSlot>? applicable;
    private HumanSlot slot = item?.Slot ?? HumanSlot.Body;
    
    private bool dirty;
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNewItem ? "Creating" : "Editing Item", shadowed: true);
        ImGuiExt.CenterText(IsNewItem ? $"New Item in {folderPath}" : $"{folderPath} / {Item.Name}", shadowed: true);
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
        applicable ??= slot == HumanSlot.Face ? Item.Bonus.Clone() : Item.Equipment.Clone();
        
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
            dirty |= CustomInput.InputText("Item Name", ref itemName, 100, errorMessage: itemName.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.ReadOnlyInputText("Path", folderPath);
        }
        
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        
        
        
        if (ImGui.BeginChild("item", new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {

            ImGui.SetNextItemWidth(150 * ImGuiHelpers.GlobalScale);
            if (ImGui.BeginCombo("Slot", $"{slot.ToName()}", ImGuiComboFlags.HeightLarge)) {
                ImGui.TextDisabled("Changing slot will cause the item to be lost. Hold SHIFT to confirm");
                foreach (var s in Common.GetGearSlots()) {
                    if (ImGui.Selectable($"{s.ToName()}", slot == s, ImGui.GetIO().KeyShift ? ImGuiSelectableFlags.None : ImGuiSelectableFlags.Disabled)) {

                        if (slot != s) {
                            applicable = s == HumanSlot.Face ? ApplicableBonus.FromNothing() : ApplicableEquipment.FromNothing(s);
                        }
                        
                        slot = s;
                    }
                }
                
                ImGui.EndCombo();
            }
            
            EquipmentDisplay.ShowSlot(applicable, slot, false, EquipmentDisplayFlags.NoApplyToggles, character, folderGuid);
            
            if (ImGui.CollapsingHeader("Image")) {
                var folder = character.Folders.GetValueOrDefault(folderGuid);
                var itemStyle = folder?.OutfitPolaroidStyle ?? character.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
                ImageEditor.Draw(Item, itemStyle, Item.Name, ref controlFlags);
            }
            
            if (ImGui.CollapsingHeader("Details")) {
                var guid = Item.Guid.ToString();
                ImGui.InputText("GUID", ref guid, 128, ImGuiInputTextFlags.ReadOnly | ImGuiInputTextFlags.AutoSelectAll);
                sortName ??= itemName;
                dirty |= ImGui.InputTextWithHint("Custom Sort Name", itemName, ref sortName, 128);
            }
            
        }
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));
        
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(subWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon("Save Item", FontAwesomeIcon.Save, new Vector2(subWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                Item.Name = itemName;
                Item.SortName = string.IsNullOrWhiteSpace(sortName) ? null : sortName.Trim();
                Item.Item = applicable;
                Item.Slot = slot;
                Item.Save(true);
                MainWindow.PopPage();
            }
        }
    }
}
