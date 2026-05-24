using SimpleGlamourSwitcher.Configuration.Files;

namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface ICreatableListEntry<T> where T : ICreatableListEntry<T>, IListEntry {
    public static abstract T Create(CharacterConfigFile character, Guid guid);
    public static abstract T CreateFromLocalPlayer(CharacterConfigFile character, Guid guid, IDefaultOutfitOptionsProvider? defaultOptionsProvider = null);
}
