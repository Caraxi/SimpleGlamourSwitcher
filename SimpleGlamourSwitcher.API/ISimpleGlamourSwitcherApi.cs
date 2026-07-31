namespace SimpleGlamourSwitcher.API;

public interface ISimpleGlamourSwitcherApi {
    public (int, int)? ApiVersion();
    public List<IpcEntry>? GetAvailableEntryList();
    public List<IpcEntry>? GetAvailableOutfitList();
    public List<IpcEntry>? GetAvailableEmoteList();
    public List<IpcEntry>? GetAvailableMinionList();
    public List<IpcEntry>? GetAvailableItemList();
    public List<IpcEntry>? GetAvailableGenericEntryList();
    
    public List<IpcEntry>? GetCharacterEntryList(Guid characterGuid);
    public List<IpcEntry>? GetCharacterOutfitList(Guid characterGuid);
    public List<IpcEntry>? GetCharacterEmoteList(Guid characterGuid);
    public List<IpcEntry>? GetCharacterMinionList(Guid characterGuid);
    public List<IpcEntry>? GetCharacterItemList(Guid characterGuid);
    public List<IpcEntry>? GetCharacterGenericEntryList(Guid characterGuid);

    public List<IpcCharacter>? GetCharacterList();

    public string? SwitchCharacter(Guid characterGuid);
    public string? Apply(Guid entryGuid);
    
    public Guid? GetActiveCharacterGuid();
    public Guid? GetSharedCharacterGuid();
}
