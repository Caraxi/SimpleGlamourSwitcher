using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;
using ItemManager = SimpleGlamourSwitcher.Service.ItemManager;

namespace SimpleGlamourSwitcher.Configuration.Files;

public class ItemConfigFile : ConfigFile<ItemConfigFile, CharacterConfigFile>, INamedConfigFile, IImageProvider, IListEntry, IHasModConfigs, IAdditionalLink {
    public FontAwesomeIcon TypeIcon => Slot switch {
        HumanSlot.Head => FontAwesomeIcon.HatCowboy,
        HumanSlot.Body => FontAwesomeIcon.Tshirt,
        HumanSlot.Legs => FontAwesomeIcon.QuestionCircle, // Why no Pants
        HumanSlot.Feet => FontAwesomeIcon.Socks,
        HumanSlot.Hands => FontAwesomeIcon.Mitten,
        HumanSlot.Ears => FontAwesomeIcon.QuestionCircle,
        HumanSlot.Neck => FontAwesomeIcon.QuestionCircle,
        HumanSlot.Wrists => FontAwesomeIcon.QuestionCircle,
        HumanSlot.RFinger => FontAwesomeIcon.Ring,
        HumanSlot.LFinger => FontAwesomeIcon.Ring,
        HumanSlot.Face => FontAwesomeIcon.Glasses,
        _ => FontAwesomeIcon.QuestionCircle,
    };
    
    public string Name = string.Empty;
    string IImageProvider.Name => Name;
    
    string IListEntry.Name {
        get => Name;
        set => Name = value;
    }

    public string Description = string.Empty;
    public Guid Folder { get; set; } = Guid.Empty;
    public string? SortName { get; set; }

    public ImageDetail ImageDetail { get; set; } = new();

    public List<OutfitModConfig> ModConfigs { get; set; } = new();
    public HumanSlot Slot { get; set; } = HumanSlot.Body;
    
    public ApplicableEquipment? Equipment { get; set; } = new();
    public bool ShouldSerializeEquipment() => Slot != HumanSlot.Face;
    
    public ApplicableBonus? Bonus { get; set; } = new();
    public bool ShouldSerializeBonus() => Slot == HumanSlot.Face;
    
    [JsonIgnore]
    public ApplicableItem<HumanSlot> Item {
        get => Slot == HumanSlot.Face ? Bonus ?? ApplicableBonus.FromNothing() : Equipment ?? ApplicableEquipment.FromNothing(Slot);
        set {
            if (value is ApplicableBonus bonus) {
                Slot = HumanSlot.Face;
                Bonus = bonus;
            } else if (value is ApplicableEquipment equipment) {
                if (Slot == HumanSlot.Face) Slot = HumanSlot.Body;
                Equipment = equipment;
            }
        }
    }

    public static ItemConfigFile Create(CharacterConfigFile parent, Guid folderGuid) {
        var instance = Create(parent);
        instance.Folder = folderGuid;

        instance.Item = ApplicableEquipment.FromNothing(HumanSlot.Body);
        
        return instance;
    }
    
    protected override void Setup() {
        base.Setup();
        (this as IHasModConfigs).UpdateHeliosphereMods();
    }

    public async Task Apply() {
        await Framework.RunOnTick(async () => {
            await GlamourerIpc.ApplyItem(Slot, Item);
            await Framework.RunOnTick(async () => {
                var redraw = false;
                Item.ApplyToCharacter(Slot, ref redraw);
                if (redraw) {
                    await Framework.RunOnTick(() => {
                        PenumbraIpc.RedrawObject.Invoke(0);
                    }, delayTicks: 2);
                }
            }, delayTicks: 2);
        });
    }

    public async Task<bool> ApplyMods() {
        return false;
    }

    public static string GetFileName(Guid? guid) {
        return $"{CharacterDirectory.Items}/{guid}.json";
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
            wrap = CustomTextureProvider.GetFromFileAbsolute(value.path).GetWrapOrDefault();
            return wrap != null;
        }
        
        var filePath = Path.Join(GetParent()?.ImagesDirectory.FullName ?? throw new Exception("Outfit Config requires a parent."), $"{Guid}");
        var file = Common.GetImageFile(filePath);
        if (file is not { Exists: true }) {
            wrap = null;
            return false;
        }
        wrap = CustomTextureProvider.GetFromFile(file).GetWrapOrDefault();
        return wrap is not null;
    }
    
    
    [JsonIgnore] public CharacterConfigFile? ConfigFile => GetParent();

    
    private readonly static Dictionary<Guid, (string path, Stopwatch Sw)> TempImagePath = new();
    
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

    public async Task<ItemConfigFile> CreateClone() {
        return await Task.Run(() => {
            var guid = Guid.NewGuid();
            var parent = this.GetParent();
            SaveAs(guid, true);
            return Load(guid, parent);
        }) ?? throw new Exception("Failed to clone outfit.");
    }
    

    public void Delete() {
        PluginLog.Warning($"Deleting Item: {Name}");
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

