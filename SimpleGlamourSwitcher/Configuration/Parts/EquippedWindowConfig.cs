using Penumbra.GameData.Enums;

namespace SimpleGlamourSwitcher.Configuration.Parts;

public class EquippedWindowConfig {
    public const int QuickSwitchMaxRows = 2;
    
    public bool ShowSaveButton = true;
    public Dictionary<HumanSlot, int> QuickSwitchRowCount = new();
}
