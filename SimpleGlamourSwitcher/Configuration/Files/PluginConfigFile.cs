using System.Numerics;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.UserInterface.Components;

namespace SimpleGlamourSwitcher.Configuration.Files;

public class PluginConfigFile : ConfigFile<PluginConfigFile, RootConfig>, IParentConfig<PluginConfigFile>, INamedConfigFile {
    
    public HashSet<VirtualKey> Hotkey = [];
    public ConditionFlag[]? IgnoreInCondition = [];
    public Vector4 BackgroundColour = new Vector4(0, 0, 0, 0.5f);
    public Vector2 CharacterListImageSize = new Vector2(128, 128);
    public float SidebarSize = 280f;
    public bool ShowActiveCharacterInCharacterList = false;
    public Dictionary<ulong, Guid> SelectedCharacter = new();
    public string ImageFilePickerLastPath = string.Empty;
    public bool AutoCloseAfterApplying = true;
    public bool FullScreenMode = true;
    public Vector2 WindowPosition = new Vector2(50, 50);
    public Vector2 WindowSize = new Vector2(800, 600);
    public Vector2 FullscreenOffset = new Vector2(0, 0);
    public Vector2 FullscreenPadding = new Vector2(0, 0);
    
    
    public Style? CustomStyle;
    
    public bool ShowHiddenCharacters;
    public bool LogActionsToChat;

    public static FileInfo GetFile(Guid? guid = null) {
        if (guid != null) throw new Exception($"{nameof(PluginConfigFile)} does not support GUID");
        return new FileInfo(Path.Join(PluginInterface.GetPluginConfigDirectory(), "config.json"));
    }

    protected override void Setup() {
        if (Version == 0) {
            Hotkey = [VirtualKey.MENU, VirtualKey.OEM_3];
            IgnoreInCondition = [ConditionFlag.InCombat, ConditionFlag.LoggingOut];
            Version = 1;
        }
    }
    
    public static DirectoryInfo GetChildDirectory(PluginConfigFile? config) {
        return new DirectoryInfo(Path.Join(PluginInterface.GetPluginConfigDirectory()));
    }

    public static string GetFileName(Guid? guid) => "config.json";
}
