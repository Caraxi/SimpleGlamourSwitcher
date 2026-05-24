using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;

namespace SimpleGlamourSwitcher.Configuration.Interface;

public interface ICommonDetails : IImageProvider {
    public string Name { get; set; }
    public Guid Guid { get; }
    public string Description { get; set; }
    public Guid Folder { get; set; }
    public bool Dirty { get; set; }
    string TypeName { get; }
}

public interface ISortNameProvider {
    public string? SortName { get; set; }
}

public interface IAutoCommandProvider {
    public List<AutoCommandEntry> AutoCommands { get; set; }
}

public interface IListEntry : ICommonDetails, ISortNameProvider, IAutoCommandProvider {
    public void Save(bool force = false);
    public Task Apply();
    
    public bool IsValid { get; }
    public IReadOnlyList<string> ValidationErrors { get; }
    FontAwesomeIcon TypeIcon { get; }
    public FileInfo? GetImageFile();
    
    void Delete();

    IListEntry? CloneTo(CharacterConfigFile characterConfigFile);
}

