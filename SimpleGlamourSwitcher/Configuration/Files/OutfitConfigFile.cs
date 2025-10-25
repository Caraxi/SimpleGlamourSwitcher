using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using Newtonsoft.Json;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Configuration.Files;

public class OutfitConfigFile : ConfigFile<OutfitConfigFile, CharacterConfigFile>, INamedConfigFile, IImageProvider, IListEntry {
    public FontAwesomeIcon TypeIcon => FontAwesomeIcon.PersonHalfDress;
    public string Name = string.Empty;
    string IImageProvider.Name => Name;
    string IListEntry.Name {
        get => Name;
        set => Name = value;
    }

    public string Description = string.Empty;
    public Guid Folder { get; set; } = Guid.Empty;

    public List<Guid> ApplyBefore = new();
    public List<Guid> ApplyAfter = new();
    
    public string? SortName { get; set; } = string.Empty;

    public List<AutoCommandEntry> AutoCommands = new();
    
    public ImageDetail ImageDetail { get; set; } = new();

    public OutfitEquipment Equipment = new();
    public OutfitAppearance Appearance = new();
    // public OutfitMods Mods = new();

    public static OutfitConfigFile Create(CharacterConfigFile parent, Guid folderGuid) {
        var instance = Create(parent);
        instance.Folder = folderGuid;

        if (folderGuid != Guid.Empty && parent.Folders.TryGetValue(folderGuid, out var folder)) {
            instance.ApplyBefore = folder.DefaultLinkBefore.Clone();
            instance.ApplyAfter = folder.DefaultLinkAfter.Clone();
        } else {
            instance.ApplyBefore = parent.DefaultLinkBefore.Clone();
            instance.ApplyAfter = parent.DefaultLinkAfter.Clone();
        }
        
        return instance;
    }
    
    public async Task Apply() {
        Notice.Show($"Apply Outfit: {Name}");

        var stack = await GlamourSystem.HandleLinks(this);
        
        var redraw = false;

        await Framework.RunOnTick(async () => {

            await GlamourerIpc.ApplyOutfit(stack.Appearance, stack.Equipment);
            
            stack.Appearance.ApplyToCharacter(ref redraw);
            await Framework.RunOnTick(async () => {
                stack.Equipment.ApplyToCharacter(ref redraw);

                foreach (var a in stack.Additionals) {
                    redraw |= await a.ApplyMods();
                }

                if (redraw) {
                    await Framework.RunOnTick(() => {
                        PenumbraIpc.RedrawObject.Invoke(0);
                    }, delayTicks: 2);
                }

            }, delayTicks: 2);
        });
        
        EnqueueAutoCommands();
    }

    public void EnqueueAutoCommands() {
        if (!PluginConfig.EnableOutfitCommands) return;
        var parent = GetParent() ?? throw new Exception("Invalid OutfitConfigFile");
        
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

    protected override void Validate(List<string> errors) {
        foreach(var (slotName, applicable) in Enumerable.Concat(Equipment, Appearance)) {
            if (applicable is IHasModConfigs modable) {
                if (!ModListDisplay.IsValid(modable)) {
                    errors.Add($"Invalid mod setup in '{slotName}' slot.");
                }
            }
        }
    }

    public async Task<OutfitConfigFile> CreateClone() {
        return await Task.Run(() => {
            var guid = Guid.NewGuid();
            var parent = this.GetParent();
            SaveAs(guid, true);
            return Load(guid, parent);
        }) ?? throw new Exception("Failed to clone outfit.");
    }
    
    public void Delete() {
        
        PluginLog.Warning($"Deleting Outfit: {Name}");
        GetConfigPath(GetParent(), Guid).Delete();
    }
}


