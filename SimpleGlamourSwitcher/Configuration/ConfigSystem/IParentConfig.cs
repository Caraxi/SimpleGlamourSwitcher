namespace SimpleGlamourSwitcher.Configuration.ConfigSystem;

public interface IParentConfig<T> {
    public static abstract DirectoryInfo GetChildDirectory(T? parent);
}
