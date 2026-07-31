namespace SimpleGlamourSwitcher.API;

public class IpcCharacter(Guid guid, string name, IpcImage? image) {
    public Guid Guid { get; } = guid;
    public string Name { get; } = name;
    public IpcImage? Image { get; } = image;
}
