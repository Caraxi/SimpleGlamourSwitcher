namespace SimpleGlamourSwitcher.IPC.Glamourer;

public class GlamourerDesignFile : GlamourerState {
    public uint FileVersion = 0;
    public Guid Identifier = Guid.Empty;
    
    public DateTime CreationTime = DateTime.MinValue;
    public DateTime LastEdit = DateTime.MinValue;
    
    public string Name = string.Empty;
    public string Description = string.Empty;

    public bool ForcedRedraw;
    public bool ResetAdvancedDyes;
    public bool ResetTemporarySettings;

    public string Color = string.Empty;
    public bool QuickDesign;
    public string[] Tags = [];
    
    public string SortKey = string.Empty;
}