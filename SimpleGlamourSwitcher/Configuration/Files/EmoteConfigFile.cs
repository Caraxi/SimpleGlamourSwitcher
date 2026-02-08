using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Files;

public class EmoteConfigFile : ConfigFile<EmoteConfigFile, CharacterConfigFile>, INamedConfigFile, IImageProvider, IListEntry, IHasModConfigs, IAdditionalLink {
    public FontAwesomeIcon TypeIcon => FontAwesomeIcon.KissWinkHeart;
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
    
    public EmoteIdentifier? EmoteIdentifier { get; set; }

    public List<OutfitModConfig> ModConfigs { get; set; } = new();

    public static EmoteConfigFile Create(CharacterConfigFile parent, Guid folderGuid) {
        var instance = Create(parent);
        instance.Folder = folderGuid;
        return instance;
    }
    
    protected override void Setup() {
        base.Setup();
        (this as IHasModConfigs).UpdateHeliosphereMods();
    }

    public async Task Apply() {
        var activeEmote = await EmoteIdentifier.GetLocalPlayer();
        if (EmoteIdentifier == null) return;
        
        var isActive = EmoteIdentifier.Get(Objects.LocalPlayer) == EmoteIdentifier;
        ModManager.ApplyMods(EmoteIdentifier, ModConfigs);
        EnqueueAutoCommands();
        
        if (isActive) {
            ActionQueue.QueueCommand("/penumbra redraw self");
        } else {
            if (EmoteIdentifier.EmoteModeId == 0 && EmoteIdentifier.EmoteId == 0) {
                // Idle
                unsafe {
                    if (activeEmote is { EmoteModeId: 1 or 2 }) {
                        // Sitting
                        PlayerState.Instance()->SelectedPoses[0] = EmoteIdentifier.CPoseState;
                        ActionQueue.QueueCommand($"/sit");
                    } else if (activeEmote is { EmoteModeId: 3}) {
                        // Dozing
                        PlayerState.Instance()->SelectedPoses[0] = EmoteIdentifier.CPoseState;
                        ActionQueue.QueueCommand($"/doze");
                    } else {
                        if (EmoteIdentifier.CPoseState == 0) {
                            PlayerState.Instance()->SelectedPoses[0] = 6;
                        }else {
                            PlayerState.Instance()->SelectedPoses[0] = (byte)(EmoteIdentifier.CPoseState - 1);
                        }
                        ActionQueue.QueueCommand($"/cpose");
                    }
                }
            } else if (EmoteIdentifier.EmoteModeId == 0) {
                // Set by Emote
                if (DataManager.GetExcelSheet<Emote>().TryGetRow(EmoteIdentifier.EmoteId, out var emoteData)) {
                    var command = emoteData.TextCommand.Value.Command.ExtractText();
                    if (command.StartsWith("/")) {
                        ActionQueue.QueueCommand($"{command} motion");
                    }
                }
            } else if (EmoteIdentifier.EmoteModeId is 1) {
                unsafe {
                    if (activeEmote is { EmoteModeId: 1 }) {
                        if (EmoteIdentifier.CPoseState == 0) {
                            PlayerState.Instance()->SelectedPoses[3] = 3;
                        } else {
                            PlayerState.Instance()->SelectedPoses[3] = (byte) (EmoteIdentifier.CPoseState - 1);
                        }
                        ActionQueue.QueueCommand($"/cpose");
                    } else {
                        PlayerState.Instance()->SelectedPoses[3] = EmoteIdentifier.CPoseState;
                        ActionQueue.QueueCommand($"/groundsit");
                    }
                }
            } else if (EmoteIdentifier.EmoteModeId is 2) {
                // Chair Sit
                unsafe {
                    if (activeEmote is { EmoteModeId: 2 }) {
                        if (EmoteIdentifier.CPoseState == 0) {
                            PlayerState.Instance()->SelectedPoses[2] = 5;
                        } else {
                            PlayerState.Instance()->SelectedPoses[2] = (byte) (EmoteIdentifier.CPoseState - 1);
                        }
                        ActionQueue.QueueCommand($"/cpose");
                    } else {
                        PlayerState.Instance()->SelectedPoses[2] = EmoteIdentifier.CPoseState;
                        
                        ActionQueue.QueueCommand($"/sit");
                    }
                }
            } else if (EmoteIdentifier.EmoteModeId is 3) {
                // Bed Sleep
                unsafe {
                    if (activeEmote is { EmoteModeId: 3 }) {
                        if (EmoteIdentifier.CPoseState == 0) {
                            PlayerState.Instance()->SelectedPoses[4] = 2;
                        } else {
                            PlayerState.Instance()->SelectedPoses[4] = (byte) (EmoteIdentifier.CPoseState - 1);
                        }
                        ActionQueue.QueueCommand($"/cpose");
                    } else {
                        PlayerState.Instance()->SelectedPoses[4] = EmoteIdentifier.CPoseState;
                        ActionQueue.QueueCommand($"/doze");
                    }
                }
            } else {
                if (DataManager.GetExcelSheet<EmoteMode>().TryGetRow(EmoteIdentifier.EmoteModeId, out var emoteModeData)) {
                    var command = emoteModeData.StartEmote.Value.TextCommand.Value.Command.ExtractText();
                    if (command.StartsWith("/")) {
                        ActionQueue.QueueCommand($"{command} motion");
                    }
                }
            }
        }
    }

    public async Task<bool> ApplyMods() {
        var activeEmote = await EmoteIdentifier.GetLocalPlayer();
        if (EmoteIdentifier == null) return false;
        var isActive = activeEmote == EmoteIdentifier;
        
        ModManager.ApplyMods(EmoteIdentifier, ModConfigs);
        return isActive;
    }
    
    public void EnqueueAutoCommands() {
        if (!PluginConfig.EnableOutfitCommands) return;
        var parent = GetParent() ?? throw new Exception("Invalid EmoteConfigFile");
        
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
        return $"{CharacterDirectory.Emotes}/{guid}.json";
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
    }

    public void SetImageDetail(ImageDetail imageDetail) {
        ImageDetail = imageDetail.Clone();
        Dirty = true;
        Save();
    }

    protected override void Validate(List<string> errors) {
        
    }

    public async Task<EmoteConfigFile> CreateClone() {
        return await Task.Run(() => {
            var guid = Guid.NewGuid();
            var parent = this.GetParent();
            SaveAs(guid, true);
            return Load(guid, parent);
        }) ?? throw new Exception("Failed to clone outfit.");
    }

    public static EmoteConfigFile CreateFromLocalPlayer(CharacterConfigFile character, Guid folderGuid) {
        var cfg = Create(character, folderGuid);
        cfg.EmoteIdentifier = EmoteIdentifier.Get(Objects.LocalPlayer);
        
        if (cfg.EmoteIdentifier != null) {
            var penumbraCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
            if (penumbraCollection.ObjectValid) {
                cfg.ModConfigs = OutfitModConfig.GetModListFromEmote(cfg.EmoteIdentifier, penumbraCollection.EffectiveCollection.Id);
            }
        }

        return cfg;
    }

    public void Delete() {
        PluginLog.Warning($"Deleting Emote: {Name}");
        var path = GetConfigPath(GetParent(), Guid);
        PluginLog.Warning($"Deleting: {path}");
        GetConfigPath(GetParent(), Guid).Delete();
    }

    public IListEntry? CloneTo(CharacterConfigFile characterConfigFile) => SaveTo(characterConfigFile);
    
    public bool TryGetImageFileInfo([NotNullWhen(true)] out FileInfo? fileInfo) {
        fileInfo = GetImageFile();
        return fileInfo is { Exists: true };
    }
}

