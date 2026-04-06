using SimpleGlamourSwitcher.Configuration.Files;

namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface ICreatableListEntry<T> where T : ICreatableListEntry<T>, IListEntry {
    public abstract static T Create(CharacterConfigFile character, Guid guid);
    public abstract static T CreateFromLocalPlayer(CharacterConfigFile character, Guid guid, IDefaultOutfitOptionsProvider? defaultOptionsProvider = null);
}
