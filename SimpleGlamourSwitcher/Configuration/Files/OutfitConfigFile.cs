using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Textures.TextureWraps;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Files;

public class OutfitConfigFile : ConfigFile<OutfitConfigFile, CharacterConfigFile>, INamedConfigFile, IImageProvider {

    public string Name = string.Empty;
    string IImageProvider.Name => Name;
    
    public string Description = string.Empty;
    public Guid Folder = Guid.Empty;

    public List<Guid> ApplyBefore = new();
    public List<Guid> ApplyAfter = new();
    
    public ImageDetail ImageDetail { get; set; } = new();

    public OutfitEquipment Equipment = new();
    public OutfitAppearance Appearance = new();
    // public OutfitMods Mods = new();

    public static OutfitConfigFile Create(CharacterConfigFile parent, Guid folderGuid) {
        var instance = Create(parent);
        instance.Folder = folderGuid;
        return instance;
    }
    
    public void Apply() {
        Notice.Show($"Apply Outfit: {Name}");
        var redraw = false;

        Framework.RunOnTick(() => {

            GlamourerIpc.ApplyOutfit(this);
            
            Appearance.ApplyToCharacter(ref redraw);
            Framework.RunOnTick(() => {
                Equipment.ApplyToCharacter(ref redraw);

                if (redraw) {
                    Framework.RunOnTick(() => {
                        PenumbraIpc.RedrawObject.Invoke(0);
                    }, delayTicks: 2);
                }

            }, delayTicks: 2);
        });
    }
    
    /*
    public static FileInfo GetFile(CharacterConfigFile characterConfig, Guid guid) {
        var dir = characterConfig.OutfitDirectory;
        return new FileInfo(Path.Join(dir.FullName, $"{guid}.json"));
    }
    */

    public static string GetFileName(Guid? guid) {
        return $"outfits/{guid}.json";
    }


    public FileInfo? GetImageFile() {
        var filePath = Path.Join(GetParent()?.ImagesDirectory.FullName ?? throw new Exception("Outfit Config requires a parent."), $"{Guid}");
        var file = Common.GetImageFile(filePath);
        if (file is not { Exists: true }) {
            return null;
        }
        return file;
    }
    
    public bool TryGetImage([NotNullWhen(true)] out IDalamudTextureWrap? wrap) {
        if (TempImagePath.TryGetValue(Guid, out var value) && value.Sw.ElapsedMilliseconds < 10000) {
            wrap = TextureProvider.GetFromFileAbsolute(value.path).GetWrapOrDefault();
            return wrap != null;
        }
        
        var filePath = Path.Join(GetParent()?.ImagesDirectory.FullName ?? throw new Exception("Outfit Config requires a parent."), $"{Guid}");
        var file = Common.GetImageFile(filePath);
        if (file is not { Exists: true }) {
            wrap = null;
            return false;
        }
        wrap = TextureProvider.GetFromFile(file).GetWrapOrDefault();
        return wrap is not null;
    }
    
    
    [JsonIgnore] public CharacterConfigFile? ConfigFile => GetParent();

    
    private static readonly Dictionary<Guid, (string path, Stopwatch Sw)> TempImagePath = new();
    
    public void SetImage(FileInfo fileInfo) {
        if (ConfigFile == null || Guid == Guid.Empty) return;
        
        var dir = ConfigFile.ImagesDirectory;
        var fileName = Path.Join(dir.FullName, $"{Guid}");
        
        foreach (var type in IImageProvider.SupportedImageFileTypes) {
            if (File.Exists($"{fileName}.{type}")) {
                File.Delete($"{fileName}.{type}");
            }
        }

        TempImagePath[Guid] = (fileInfo.FullName, Stopwatch.StartNew());
        fileInfo.CopyTo(fileName + Path.GetExtension(fileInfo.FullName));
        
        return;
    }

    public void SetImageDetail(ImageDetail imageDetail) {
        ImageDetail = imageDetail.Clone();
        Dirty = true;
        Save();
    }

    public static OutfitConfigFile CreateFromLocalPlayer(CharacterConfigFile character, Guid folderGuid, IDefaultOutfitOptionsProvider defaultOptionsProvider) {
        
        var instance = Create(character, folderGuid);
        PluginLog.Debug("Creating Outfit from Local Player");
        var glamourerState = GlamourerIpc.GetState(0);
        var penumbraCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
        
        if (glamourerState != null && penumbraCollection.ObjectValid) {
            instance.Equipment = OutfitEquipment.FromExistingState(defaultOptionsProvider, glamourerState, penumbraCollection.EffectiveCollection.Id);
            instance.Appearance = OutfitAppearance.FromExistingState(defaultOptionsProvider, glamourerState, penumbraCollection.EffectiveCollection.Id);
        }

        return instance;


    }
}


