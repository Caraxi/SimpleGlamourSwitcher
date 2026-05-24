namespace SimpleGlamourSwitcher.Configuration.ConfigSystem;

public class RootConfig : ConfigFile, IParentConfig<RootConfig> {
    public static readonly RootConfig Instance = new();

    public static DirectoryInfo GetChildDirectory(RootConfig? parent) => new(PluginInterface.GetPluginConfigDirectory());
}
