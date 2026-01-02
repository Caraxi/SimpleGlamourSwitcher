using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Bindings.ImGui;
using Newtonsoft.Json;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;
using SimpleGlamourSwitcher.UserInterface.Enums;

namespace SimpleGlamourSwitcher.Configuration.Files;

public class PluginConfigFile : ConfigFile<PluginConfigFile, RootConfig>, IParentConfig<PluginConfigFile>, INamedConfigFile {
    public HashSet<VirtualKey> Hotkey = [];
    public Vector4 BackgroundColour = ImGui.ColorConvertU32ToFloat4(ImGui.GetColorU32(ImGuiCol.WindowBg));
    public float SidebarSize = 280f;
    public bool ShowActiveCharacterInCharacterList = true;
    public Dictionary<ulong, Guid> SelectedCharacter = new();
    public string ImageFilePickerLastPath = string.Empty;
    public bool AutoCloseAfterApplying = true;
    public bool FullScreenMode = false;
    public Vector2 WindowPosition = new(50, 50);
    public Vector2 WindowSize = new(800, 600);
    public Vector2 FullscreenOffset = new(0, 0);
    public Vector2 FullscreenPadding = new(0, 0);
    public string DebugDefaultPage = string.Empty;
    public FolderSortStrategy FolderSortStrategy = FolderSortStrategy.Manual;
    
    [JsonIgnore]
    public Style? CustomStyle {
        get {
            if (CustomCharacterPolaroidStyle == null) return null;
            return Style.Default with {
                CharacterPolaroid = CustomCharacterPolaroidStyle ?? Style.Default.CharacterPolaroid
            };
        }

        set => CustomCharacterPolaroidStyle = value?.CharacterPolaroid;
    }
    
    public Dictionary<string, int> ModSlotIdentifier = [];
    public PolaroidStyle? CustomCharacterPolaroidStyle = null;
    
    public bool ShowHiddenCharacters;
    public bool LogActionsToChat;
    public bool ShowButtonIcons = true;
    public bool AllowHotkeyInGpose = true;
    public bool EnableOutfitCommands = false;
    public bool DryRunOutfitCommands = false;
    public bool OpenDebugOnStartup = false;
    public GridlineStyle ScreenshotGridlineStyle;

    public HashSet<CustomizeIndex> DisableAutoModsCustomize = [];
    public HashSet<HumanSlot> DisableAutoModsEquip = [];
    public HashSet<EquipSlot> DisableAutoModsWeapons = [];
    
    public static FileInfo GetFile(Guid? guid = null) {
        if (guid != null) throw new Exception($"{nameof(PluginConfigFile)} does not support GUID");
        return new FileInfo(Path.Join(PluginInterface.GetPluginConfigDirectory(), "config.json"));
    }

    protected override void Setup() {
        if (Version == 0) {
            Version = 1;
        }
    }
    
    public static DirectoryInfo GetChildDirectory(PluginConfigFile? config) {
        return new DirectoryInfo(Path.Join(PluginInterface.GetPluginConfigDirectory()));
    }

    public static string GetFileName(Guid? guid) => "config.json";
}
