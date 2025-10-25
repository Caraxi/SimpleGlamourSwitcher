global using static SimpleGlamourSwitcher.Service.PluginService;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Interface;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Glamourer.GameData;
using OtterGui.Log;
using OtterGui.Services;
using Penumbra.GameData.Data;
using Penumbra.GameData.DataContainers;

#pragma warning disable SeStringEvaluator
namespace SimpleGlamourSwitcher.Service;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public class PluginService {
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; } = null!;
    [PluginService] public static IDataManager DataManager { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IKeyState KeyState { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static ITextureReadbackProvider TextureReadbackProvider { get; private set; } = null!;
    [PluginService] public static IPluginLog PluginLog { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] public static ISeStringEvaluator SeStringEvaluator { get; private set; } = null!;
    public static IUiBuilder PluginUi => PluginInterface.UiBuilder;

    public static ActionQueueService ActionQueue { get; private set; } = new();

    public static ItemManager ItemManager => _serviceManager.GetService<ItemManager>();
    public static DictBonusItems DictBonusItems => _serviceManager.GetService<DictBonusItems>();
    public static CustomizeManager CustomizeManager => _serviceManager.GetService<CustomizeManager>();
    
    private static ServiceManager _serviceManager = null!;

    public void Initialize() {
        var logger = new Logger();
        _serviceManager = new ServiceManager(logger)
            .AddDalamudService<IDataManager>(PluginInterface)
            .AddDalamudService<ISeStringEvaluator>(PluginInterface)
            .AddDalamudService<ITextureProvider>(PluginInterface)
            .AddDalamudService<IPluginLog>(PluginInterface)
            .AddSingleton<DictAction>()
            .AddSingleton<DictBNpc>()
            .AddSingleton<DictBNpcNames>()
            .AddSingleton<DictBonusItems>()
            .AddSingleton<DictCompanion>()
            .AddSingleton<DictEmote>()
            .AddSingleton<DictENpc>()
            .AddSingleton<DictModelChara>()
            .AddSingleton<DictMount>()
            .AddSingleton<DictOrnament>()
            .AddSingleton<DictStain>()
            .AddSingleton<DictWorld>()
            .AddSingleton<GamePathParser>()
            .AddSingleton<IdentificationListEquipment>()
            .AddSingleton<IdentificationListModels>()
            .AddSingleton<IdentificationListWeapons>()
            .AddSingleton<ItemData>()
            .AddSingleton<ItemManager>()
            .AddSingleton<ItemsByType>()
            .AddSingleton<ItemsPrimaryModel>()
            .AddSingleton<ItemsSecondaryModel>()
            .AddSingleton<ItemsTertiaryModel>()
            .AddSingleton<NameDicts>()
            .AddSingleton<ObjectIdentification>()
            .AddSingleton<RestrictedGear>()
            .AddSingleton<RestrictedItemsFemale>()
            .AddSingleton<RestrictedItemsMale>()
            .AddSingleton<RestrictedItemsRace>()
            .AddSingleton<NpcCustomizeSet>()
            .AddSingleton<CustomizeManager>();
        

        _serviceManager.EnsureRequiredServices();
    }

    internal static void Dispose() {
        _serviceManager.Dispose();
    }
}
