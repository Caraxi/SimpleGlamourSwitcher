namespace SimpleGlamourSwitcher.Configuration.ConfigSystem;

public interface INamedConfigFile {
    public static abstract string GetFileName(Guid? guid);
}
