namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface IAdditionalLink : IListEntry {
    public Task<bool> ApplyMods();
}