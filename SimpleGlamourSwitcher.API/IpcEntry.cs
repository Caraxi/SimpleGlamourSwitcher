namespace SimpleGlamourSwitcher.API;

public class IpcEntry(Guid guid, string name, string folderPath, IpcImage? image, string typeName) {
    public Guid Guid { get; } = guid;
    public string Name { get; } = name;
    public string FolderPath { get; } = folderPath;
    public IpcImage? Image { get; } = image;
    public string TypeName { get; } = typeName;
}
