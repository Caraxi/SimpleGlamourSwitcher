namespace SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

public abstract record StyleProvider<T> where T : StyleProvider<T>, new() {
    public static T Default { get; } = new T();
    
    public static bool DrawEditor(ref T style) {
        // TODO:
        return false;
    }
}
