using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Page;

namespace SimpleGlamourSwitcher.UserInterface.Windows;

public class ActiveGearWindow() : Window("Simple Glamour Switcher | Equipped###SimpleGlamourSwitcherEquipped", ImGuiWindowFlags.AlwaysAutoResize) {

    private OutfitConfigFile? OutfitCache { get; set; } = null;
    private OutfitConfigFile? updatedCache;
    private bool updatingOutfit = false;
    private Stopwatch updateOutfitTimer = Stopwatch.StartNew();
    private bool dirty;
    
    
    public override void OnOpen() {
        dirty = false;
        updatingOutfit = false;
        updateOutfitTimer.Restart();
        
        AllowClickthrough = false;
        AllowPinning = false;
        RespectCloseHotkey = false;
        UpdateOutfit();
    }

    private void UpdateOutfit() {
        if (updatingOutfit || dirty) return;
        updateOutfitTimer.Restart();
        updatingOutfit = true;
        Task.Run(() => {
            var outfit = ActiveCharacter == null ? null : OutfitConfigFile.CreateFromLocalPlayer(ActiveCharacter, Guid.Empty, DefaultOptions.Equipment);
            updatedCache = outfit;
            updatingOutfit = false;
            updateOutfitTimer.Restart();
        });
    }
    

    
    public override void Draw() {
        var outfit = OutfitCache;
        if (updateOutfitTimer.ElapsedMilliseconds > 1000 && !updatingOutfit) {
            if (dirty && outfit != null) {
                outfit.Apply().ConfigureAwait(false);
                dirty = false;
                updatedCache = null;
                updateOutfitTimer.Restart();
            } else if (updatedCache != null) {
                OutfitCache = updatedCache;
                updatedCache = null;
                outfit = OutfitCache;
            } else {
                UpdateOutfit();
            }
        }
        
        if (outfit == null) {
            ImGui.TextDisabled("No Outfit");
            return;
        }
        
        dirty |= EquipmentDisplay.DrawEquipment(outfit.Equipment, EquipmentDisplayFlags.Simple | EquipmentDisplayFlags.EnableCustomItemPicker);

        if (PluginConfig.EquippedWindowConfig.ShowSaveButton && ActiveCharacter != null) {
            ImGui.SetCursorScreenPos(ImGui.GetItemRectMax() - new Vector2(-ImGui.GetStyle().ItemSpacing.X, ImGui.GetTextLineHeightWithSpacing()));
            if (ImGui.Button("Save Outfit", ImGui.GetContentRegionAvail())) {
                Plugin.MainWindow.IsOpen = true;
                Plugin.MainWindow.OpenPage(new EditOutfitPage(ActiveCharacter, Guid.Empty, outfit));
            }
        }
    }
}
