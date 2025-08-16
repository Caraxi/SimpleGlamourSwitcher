using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

namespace SimpleGlamourSwitcher.Configuration.Files;



public class CharacterConfigFile : ConfigFile<CharacterConfigFile, PluginConfigFile>, INamedConfigFile, IParentConfig<CharacterConfigFile>, IImageProvider, IDefaultOutfitOptionsProvider {
    public string Name = string.Empty;
    public bool Hidden = false;
    public bool ApplyOnLogin = true;
    public bool ApplyOnPluginReload = false;

    public Guid? PenumbraCollection;
    [Obsolete("Use Automation")] public Guid? DefaultOutfit;
    public Guid? CustomizePlusProfile;
    public (string Name, uint World) HonorificIdentity = (string.Empty, 0);
    public (string Name, uint World) HeelsIdentity = (string.Empty, 0);

    public List<AutoCommandEntry> AutoCommandBeforeOutfit = new();
    public List<AutoCommandEntry> AutoCommandAfterOutfit = new();
    
    public bool Deleted;
    
    public PolaroidStyle? OutfitPolaroidStyle;
    public PolaroidStyle? FolderPolaroidStyle;
    
    string IImageProvider.Name => Name;

    public ImageDetail ImageDetail { get; set; } = new();
    
    public Dictionary<Guid, CharacterFolder> Folders = new();
    
    public AutomationConfig Automation = new();

    public List<Guid> DefaultLinkBefore { get; set; } = [];
    public List<Guid> DefaultLinkAfter { get; set; } = [];
    
    public delegate bool IncludeCharacter(CharacterConfigFile character);
    public static class Filters {
        public static readonly IncludeCharacter Default = c => c is { Deleted: false, Hidden: false };
        public static readonly IncludeCharacter ShowHiddenCharacter = c => c is { Deleted: false };
        public static readonly IncludeCharacter DeletedCharacter = c => c is { Deleted: true };
    }
    
    public static async Task<Dictionary<Guid, CharacterConfigFile>> GetCharacterConfigurations(IncludeCharacter? filter = null, CancellationToken cancellationToken = default) {
        var dict = new Dictionary<Guid, CharacterConfigFile>();

        filter ??= Filters.Default;

        await Task.Run(() => {
            var loadedCharacters = new Dictionary<Guid, CharacterConfigFile>();
            var dir = GetDirectory();
            if (dir.Exists) {
                foreach (var f in GetDirectory().GetDirectories()) {
                    try {
                        if (!Guid.TryParse(f.Name, out var guid)) continue;
                        var charCfg = Load(guid);
                        if (charCfg != null && filter(charCfg)) {
                            loadedCharacters.Add(guid, charCfg);
                        }
                    } catch (Exception ex) {
                        PluginLog.Error(ex, $"Error loading character file - {f.FullName}");
                    }

                }
            }

            dict = loadedCharacters;
        }, cancellationToken).WaitAsync(cancellationToken);
        

        return dict;
    }
    
    public static FileInfo GetFile(Guid? guid) {
        if (guid == null) throw new Exception(nameof(CharacterConfigFile) + " requires a guid to be provided.");
        return new FileInfo(Path.Join(GetDirectory().FullName, guid.ToString(), "character.json"));
    }
    
    public static DirectoryInfo GetDirectory() {
        return new DirectoryInfo(Path.Join(PluginInterface.GetPluginConfigDirectory(), "characters"));
    }

    public bool TryGetImage([NotNullWhen(true)] out IDalamudTextureWrap? wrap, out FileInfo filePath) {
        var fileName = Path.Join(GetDirectory().FullName, Guid.ToString(), "character");
        filePath = new FileInfo(fileName + ".png");
        foreach (var type in IImageProvider.SupportedImageFileTypes) {
            if (File.Exists($"{fileName}.{type}")) {
                filePath = new FileInfo($"{fileName}.{type}");
            }
        }
        
        if (!filePath.Exists) {
            wrap = null;
            return false;
        }
        
        wrap = TextureProvider.GetFromFile(filePath.FullName).GetWrapOrDefault();
        return wrap is not null;
    }
    
    public bool TryGetImage([NotNullWhen(true)] out IDalamudTextureWrap? wrap) => TryGetImage(out wrap, out _);
    
    public bool Delete() {
        Notice.Show($"Deleting Character Config: {Name} / {Guid}");
        Deleted = true;
        Dirty = true;
        Save();
        return true;
    }

    protected override void Setup() {
        foreach (var (fGuid, f) in Folders) {
            f.ConfigFile = this;
            f.FolderGuid = fGuid;
            
            if (f.Parent == Guid.Empty) continue;
            if (!Folders.ContainsKey(f.Parent)) f.Parent = Guid.Empty;
        }
        
#pragma warning disable CS0612 // Type or member is obsolete
        if (DefaultOutfit != null) {
            Automation.Login = DefaultOutfit;
            Automation.CharacterSwitch = DefaultOutfit;
            DefaultOutfit = null;
        }
#pragma warning restore CS0612 // Type or member is obsolete
        
        base.Setup();
    }

    
    public async Task<OrderedDictionary<Guid, OutfitConfigFile>> GetOutfits() => await GetOutfits(null, CancellationToken.None);
    public async Task<OrderedDictionary<Guid, OutfitConfigFile>> GetOutfits(CancellationToken cancellationToken) => await GetOutfits(null, cancellationToken);
    public async Task<OrderedDictionary<Guid, OutfitConfigFile>> GetOutfits(Guid? folder) => await GetOutfits(folder, CancellationToken.None);
    public async Task<OrderedDictionary<Guid, OutfitConfigFile>> GetOutfits(Guid? folder, CancellationToken cancellationToken) {
        return await Task.Run(() => {
            var outfits = new List<OutfitConfigFile>();
            
            PluginLog.Verbose($"Getting Outfits from {OutfitDirectory.FullName}");
            
            foreach (var f in OutfitDirectory.GetFiles("*.json")) {
                if (!Guid.TryParse(Path.GetFileNameWithoutExtension(f.FullName), out var guid)) continue;
                var outfitCfg = OutfitConfigFile.Load(guid, this);
                if (outfitCfg == null) continue;
                var outfitFolder = Folders.ContainsKey(outfitCfg.Folder) ? outfitCfg.Folder : Guid.Empty;
                if (folder != null && outfitFolder != folder) continue;
                outfits.Add(outfitCfg);
            }
            
            var dict = new OrderedDictionary<Guid, OutfitConfigFile>();
            foreach (var outfit in outfits.OrderBy(o => string.IsNullOrWhiteSpace(o.SortName) ? o.Name  : o.SortName)) {
                dict.Add(outfit.Guid, outfit);
            }

            return dict;
        }, cancellationToken).WaitAsync(cancellationToken);
    }
    
    public static string GetFileName(Guid? guid) {
        return Path.Join("characters", $"{guid}", "character.json");
    }

    [JsonIgnore]
    public DirectoryInfo OutfitDirectory {
        get {
            var dir = new DirectoryInfo(Path.Join(GetChildDirectory(this).FullName, "outfits"));
            if (!dir.Exists) dir.Create();
            
            PluginLog.Debug($"Character Outfit Directory [{Guid}] is {dir.FullName}");
            
            
            return dir;
        }
    }
    
    [JsonIgnore]
    public DirectoryInfo ImagesDirectory {
        get {
            var dir = new DirectoryInfo(Path.Join(GetChildDirectory(this).FullName, "images"));
            if (!dir.Exists) dir.Create();
            return dir;
        }
    }

    private static readonly Dictionary<Guid, (string path, Stopwatch Sw)> TempImagePath = new();
    
    public void SetImage(FileInfo fileInfo) {
        if ( Guid == Guid.Empty) return;

        var dir = new DirectoryInfo(Path.Join(GetDirectory().FullName, Guid.ToString()));
        if (!dir.Exists) Directory.CreateDirectory(dir.FullName);
        
        var fileName = Path.Join(GetDirectory().FullName, Guid.ToString(), "character");
        foreach (var type in IImageProvider.SupportedImageFileTypes) {
            if (File.Exists($"{fileName}.{type}")) {
                File.Delete($"{fileName}.{type}");
            }
        }

        TempImagePath[Guid] = (fileInfo.FullName, Stopwatch.StartNew());
        fileInfo.CopyTo(fileName + Path.GetExtension(fileInfo.FullName));
    }

    public void SetImageDetail(ImageDetail imageDetail) {
        ImageDetail = imageDetail;
        Dirty = true;
        Save();
    }
    
    public static DirectoryInfo GetChildDirectory(CharacterConfigFile? parent) {
        var configFile = new FileInfo(Path.Join(PluginInterface.GetPluginConfigDirectory(), GetFileName(parent?.Guid ?? throw new InvalidOperationException())));
        return new DirectoryInfo(configFile.Directory?.FullName ?? throw new InvalidOperationException());
    }

    public string ParseFolderPath(Guid guid, bool characterName = true) => ParseFolderPath(guid, [], characterName);
    private string ParseFolderPath(Guid guid, HashSet<Guid> folders, bool characterName = true) {
        if (!folders.Add(guid)) throw new Exception("Folder loop caught");
        if (guid == Guid.Empty || !Folders.TryGetValue(guid, out var folder)) return characterName ? Name : string.Empty;
        return characterName || folder.Parent != Guid.Empty ? $"{ParseFolderPath(folder.Parent, folders)} / {folder.Name}" : folder.Name;
    }

    
    public HashSet<CustomizeIndex> DefaultEnabledCustomizeIndexes { get; set; } = new();
    public HashSet<HumanSlot> DefaultDisabledEquipmentSlots { get; set; } = new();
    public HashSet<AppearanceParameterKind> DefaultEnabledParameterKinds { get; set; } = new();
    public HashSet<ToggleType> DefaultEnabledToggles { get; set; } = new();

    public IDefaultOutfitOptionsProvider GetOptionsProvider(Guid folderGuid) {
        if (folderGuid == Guid.Empty) return this;
        return Folders.GetValueOrDefault(folderGuid) as IDefaultOutfitOptionsProvider ?? this;
    }

    
    
    public async Task ApplyAutomation(bool isLogin = false, bool isCharacterSwitch = false, bool isGearsetSwitch = false) {
        var outfits = await GetOutfits();

        var currentGearset = GameHelper.GetActiveGearset();
        if (currentGearset != null) {
            if (Automation.Gearsets.TryGetValue(currentGearset.Value.Id, out var guid)) {
                if (guid != null) {
                    if (outfits.TryGetValue(guid.Value, out var outfit)) {
                        await outfit.Apply();
                    } else {
                        Chat.PrintError($"Failed to find outfit configured for Gearset '{currentGearset.Value.Name}'.", "Simple Glamour Switcher", 500);
                    }
                }
                
                return;
            }
            
        }

        if (isGearsetSwitch && Automation.DefaultGearset != null) {
            if (outfits.TryGetValue(Automation.DefaultGearset.Value, out var outfit)) {
                await outfit.Apply();
            } else {
                Chat.PrintError($"Failed to find outfit configured for 'Any Gearset'.", "Simple Glamour Switcher", 500);
            }
            return;
        }
        
        if (isCharacterSwitch && Automation.CharacterSwitch != null) {
            if (outfits.TryGetValue(Automation.CharacterSwitch.Value, out var outfit)) {
                await outfit.Apply();
            } else {
                Chat.PrintError($"Failed to find outfit configured for Character Switch", "Simple Glamour Switcher", 500);
            }
            return;
        }
        
        if (isLogin && Automation.Login != null) {
            if (outfits.TryGetValue(Automation.Login.Value, out var outfit)) {
                await outfit.Apply();
            } else {
                Chat.PrintError($"Failed to find outfit configured for Login.", "Simple Glamour Switcher", 500);
            }
            return;
        }
    }
}
