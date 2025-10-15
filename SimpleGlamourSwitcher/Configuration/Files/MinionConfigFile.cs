using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;
using Companion = Lumina.Excel.Sheets.Companion;

namespace SimpleGlamourSwitcher.Configuration.Files;

public class MinionConfigFile : ConfigFile<MinionConfigFile, CharacterConfigFile>, INamedConfigFile, IImageProvider, IListEntry, IHasModConfigs {

    public string Name = string.Empty;
    string IImageProvider.Name => Name;
    
    string IListEntry.Name {
        get => Name;
        set => Name = value;
    }

    public string Description = string.Empty;
    public Guid Folder { get; set; } = Guid.Empty;
    public string? SortName { get; set; }

    public List<AutoCommandEntry> AutoCommands = new();

    public ImageDetail ImageDetail { get; set; } = new();


    public uint MinionId { get; set; } = 0;
    public bool Resummon { get; set; } 
    public List<OutfitModConfig> ModConfigs { get; set; } = new();

    public static MinionConfigFile Create(CharacterConfigFile parent, Guid folderGuid) {
        var instance = Create(parent);
        instance.Folder = folderGuid;
        return instance;
    }
    
    public async Task Apply() {
        var isActive = GameHelper.GetActiveCompanionId() == MinionId;

        var resummonIfActive = Resummon || KeyState[VirtualKey.SHIFT];
        
        
        if (isActive && resummonIfActive) {
            ActionQueue.QueueCommand("/minion");
            await Task.Delay(TimeSpan.FromMilliseconds(1250 + Random.Shared.Next(250)));
        }
        
        if (DataManager.GetExcelSheet<Companion>().TryGetRow(MinionId, out var row)) {
            Notice.Show($"Apply Minion: {Name} for {row.Singular.ExtractText()}");
            ModManager.ApplyMods(row, ModConfigs);
            EnqueueAutoCommands();
            
            if (isActive && !resummonIfActive) {
                ActionQueue.QueueCommand($"/penumbra redraw {SeStringEvaluator.EvaluateObjStr(ObjectKind.Companion, MinionId)}");
            } else {
                ActionQueue.QueueCommand($"/minion \"{SeStringEvaluator.EvaluateObjStr(ObjectKind.Companion, MinionId)}");
            }
        }
    }

    public void EnqueueAutoCommands() {
        if (!PluginConfig.EnableOutfitCommands) return;
        var parent = GetParent() ?? throw new Exception("Invalid MinionConfigFile");
        
        List<string> commands = [];
        
        if (parent.Folders.TryGetValue(Folder, out var folder)) {
            if (folder.AutoCommandsSkipCharacter) {
                commands.AddRange(folder.AutoCommandBeforeOutfit.Where(c => c.Enabled).Select(c => c.Command));
                commands.AddRange(AutoCommands.Where(c => c.Enabled).Select(c => c.Command));
                commands.AddRange(folder.AutoCommandAfterOutfit.Where(c => c.Enabled).Select(c => c.Command));
            } else {
                commands.AddRange(parent.AutoCommandBeforeOutfit.Where(c => c.Enabled).Select(c => c.Command));
                commands.AddRange(folder.AutoCommandBeforeOutfit.Where(c => c.Enabled).Select(c => c.Command));
                commands.AddRange(AutoCommands.Where(c => c.Enabled).Select(c => c.Command));
                commands.AddRange(folder.AutoCommandAfterOutfit.Where(c => c.Enabled).Select(c => c.Command));
                commands.AddRange(parent.AutoCommandAfterOutfit.Where(c => c.Enabled).Select(c => c.Command));
            }
        } else {
            commands.AddRange(parent.AutoCommandBeforeOutfit.Where(c => c.Enabled).Select(c => c.Command));
            commands.AddRange(AutoCommands.Where(c => c.Enabled).Select(c => c.Command));
            commands.AddRange(parent.AutoCommandAfterOutfit.Where(c => c.Enabled).Select(c => c.Command));
        }

        foreach (var c in commands) {
            if (PluginConfig.DryRunOutfitCommands) {
                Chat.Print($"{c}", "Dry Run - SGS");
            } else {
                ActionQueue.QueueCommand(c);
            }
        }
    }
    
    /*
    public static FileInfo GetFile(CharacterConfigFile characterConfig, Guid guid) {
        var dir = characterConfig.OutfitDirectory;
        return new FileInfo(Path.Join(dir.FullName, $"{guid}.json"));
    }
    */

    public static string GetFileName(Guid? guid) {
        return $"minions/{guid}.json";
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

    protected override void Validate(List<string> errors) {
        
    }

    public async Task<MinionConfigFile> CreateClone() {
        return await Task.Run(() => {
            var guid = Guid.NewGuid();
            var parent = this.GetParent();
            SaveAs(guid, true);
            return Load(guid, parent);
        }) ?? throw new Exception("Failed to clone outfit.");
    }

    public static MinionConfigFile CreateFromLocalPlayer(CharacterConfigFile character, Guid folderGuid) {
        var cfg = Create(character, folderGuid);
        cfg.MinionId = GameHelper.GetActiveCompanionId();
        
        if (cfg.MinionId != 0) {
            var penumbraCollection = PenumbraIpc.GetCollectionForObject.Invoke(1);
            if (penumbraCollection.ObjectValid) {
                cfg.ModConfigs = OutfitModConfig.GetModListFromMinion(cfg.MinionId, penumbraCollection.EffectiveCollection.Id);
            }
        }

        return cfg;
    }

    public void Delete() {
        PluginLog.Warning($"Deleting Minion: {Name}");
        var path = GetConfigPath(GetParent(), Guid);
        PluginLog.Warning($"Deleting: {path}");
        GetConfigPath(GetParent(), Guid).Delete();
    }
}


