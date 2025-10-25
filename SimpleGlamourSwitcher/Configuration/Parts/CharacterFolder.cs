using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Textures.TextureWraps;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Parts;


public class PreviousCharacterFolder : CharacterFolder, IDefaultOutfitOptionsProvider{
    public static IDalamudTextureWrap? GetImage() {
        return Common.GetEmbeddedTexture("resources/previousFolder.png").GetWrapOrDefault();
    }
}

public class CharacterFolder : IImageProvider, IDefaultOutfitOptionsProvider {

    [JsonIgnore] private bool usingDefaultTexture = true;
    
    [JsonIgnore]
    public CharacterConfigFile? ConfigFile { get; set; }
    
    [JsonIgnore]
    public Guid? FolderGuid { get; set; }
    
    
    public string Name = string.Empty;
    string IImageProvider.Name => Name;

    public PolaroidStyle? OutfitPolaroidStyle;
    public PolaroidStyle? FolderPolaroidStyle;
    
    public Guid Parent = Guid.Empty;
    public bool Hidden;

    public static IDalamudTextureWrap? GetDefaultFolderImage() {
        return Common.GetEmbeddedTexture("resources/folder.png").GetWrapOrDefault();
    }
    
    public HashSet<CustomizeIndex>? CustomDefaultEnabledCustomizeIndexes;
    public HashSet<HumanSlot>? CustomDefaultDisabledEquipmentSlots;
    public HashSet<AppearanceParameterKind>? CustomDefaultEnabledParameterKinds;
    public HashSet<ToggleType>? CustomDefaultEnabledToggles;
    
    public List<AutoCommandEntry> AutoCommandBeforeOutfit = new();
    public List<AutoCommandEntry> AutoCommandAfterOutfit = new();
    public bool AutoCommandsSkipCharacter;

    public bool? CustomDefaultRevertEquip;
    public bool? CustomDefaultRevertCustomize;

    public DefaultLinks? CustomDefaultLinks;
    
    public class DefaultLinks {
        public List<Guid> Before = [];
        public List<Guid> After = [];
    }
    
    [JsonIgnore]
    public HashSet<CustomizeIndex> DefaultEnabledCustomizeIndexes => CustomDefaultEnabledCustomizeIndexes ?? ConfigFile?.DefaultEnabledCustomizeIndexes ?? [];
    
    [JsonIgnore]
    public HashSet<HumanSlot> DefaultDisabledEquipmentSlots => CustomDefaultDisabledEquipmentSlots ?? ConfigFile?.DefaultDisabledEquipmentSlots ?? [];

    [JsonIgnore]
    public HashSet<AppearanceParameterKind> DefaultEnabledParameterKinds => CustomDefaultEnabledParameterKinds ?? ConfigFile?.DefaultEnabledParameterKinds ?? [];
    
    [JsonIgnore]
    public HashSet<ToggleType> DefaultEnabledToggles => CustomDefaultEnabledToggles ?? ConfigFile?.DefaultEnabledToggles ?? [];

    [JsonIgnore] public bool DefaultRevertEquip => CustomDefaultRevertEquip ?? ConfigFile?.DefaultRevertEquip  ?? false;
    [JsonIgnore] public bool DefaultRevertCustomize => CustomDefaultRevertCustomize ?? ConfigFile?.DefaultRevertCustomize  ?? false;
    
    [JsonIgnore]
    public List<Guid> DefaultLinkBefore => CustomDefaultLinks?.Before ?? ConfigFile?.DefaultLinkBefore ?? [];
    
    [JsonIgnore]
    public List<Guid> DefaultLinkAfter => CustomDefaultLinks?.After ?? ConfigFile?.DefaultLinkAfter ?? [];
    
    
    public static IDalamudTextureWrap? GetImage(CharacterConfigFile? characterConfig, Guid folderGuid) => GetImage(characterConfig, folderGuid, out _);
    
    public static IDalamudTextureWrap? GetImage(CharacterConfigFile? characterConfig, Guid folderGuid, out bool isDefault) {
        if (TempImagePath.TryGetValue(folderGuid, out var value) && value.Sw.ElapsedMilliseconds < 10000) {
            isDefault = false;
            return TextureProvider.GetFromFileAbsolute(value.path).GetWrapOrDefault();
        }
        
        isDefault = true;
        if (characterConfig == null || folderGuid == Guid.Empty) return GetDefaultFolderImage();
        
        var dir = characterConfig.ImagesDirectory;
        var fileName = Path.Join(dir.FullName, $"{folderGuid}");

        foreach (var type in IImageProvider.SupportedImageFileTypes) {
            if (File.Exists($"{fileName}.{type}")) {
                isDefault = false;
                return TextureProvider.GetFromFileAbsolute($"{fileName}.{type}").GetWrapOrDefault();
            }
        }

        return GetDefaultFolderImage();
    }

    public string GetPath(CharacterConfigFile characterConfigFile) {
        if (Parent == Guid.Empty || !characterConfigFile.Folders.TryGetValue(Parent, out var parentFolder)) 
            return $"{characterConfigFile.Name}/{Name}";
        return $"{parentFolder.GetPath(characterConfigFile)}/{Name}";
    }

    public ImageDetail ImageDetail { get; set; } = new();

    public bool TryGetImage([NotNullWhen(true)] out IDalamudTextureWrap? wrap) {
        if (this is PreviousCharacterFolder folder) {
            wrap = PreviousCharacterFolder.GetImage();
            usingDefaultTexture = true;
            return wrap != null;
        }

        if (ConfigFile == null || FolderGuid == null) {
            wrap = null;
            usingDefaultTexture = false;
            return false;
        }


        wrap = GetImage(ConfigFile, FolderGuid.Value, out usingDefaultTexture);
        return wrap != null;
    }

    private static readonly Dictionary<Guid, (string path, Stopwatch Sw)> TempImagePath = new();
    
    public void SetImage(FileInfo fileInfo) {
        if (ConfigFile == null || FolderGuid == null) return;
        
        var dir = ConfigFile.ImagesDirectory;
        if (!dir.Exists) Directory.CreateDirectory(dir.FullName);
        var fileName = Path.Join(dir.FullName, $"{FolderGuid}");
        
        foreach (var type in IImageProvider.SupportedImageFileTypes) {
            if (File.Exists($"{fileName}.{type}")) {
                File.Delete($"{fileName}.{type}");
            }
        }

        TempImagePath[FolderGuid.Value] = (fileInfo.FullName, Stopwatch.StartNew());
        fileInfo.CopyTo(fileName + Path.GetExtension(fileInfo.FullName));
    }

    public void SetImageDetail(ImageDetail imageDetail) {
        this.ImageDetail = imageDetail.Clone();
        this.ConfigFile!.Dirty = true;
        this.ConfigFile?.Save();
    }

    public bool IsUsingDefaultImage() {
        return usingDefaultTexture;
    }
}


