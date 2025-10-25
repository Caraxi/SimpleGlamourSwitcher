using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;

namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface IListEntry {
    public Guid Guid { get; }
    public string Name { get; set; }
    public string SortName { get; set; }
    
    public Guid Folder { get; set; }
    
    public bool Dirty { get; set; }
    public ImageDetail ImageDetail { get; set; }
    public void Save(bool force = false);
    public Task Apply();
    
    public bool IsValid { get; }
    public IReadOnlyList<string> ValidationErrors { get; }
    FontAwesomeIcon TypeIcon { get; }
    public FileInfo? GetImageFile();
    public bool TryGetImage([NotNullWhen(true)] out IDalamudTextureWrap? wrap);
    
    void Delete();

    IListEntry? CloneTo(CharacterConfigFile characterConfigFile);
    public void SetImage(FileInfo fileInfo);
}