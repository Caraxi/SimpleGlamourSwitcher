using System.Diagnostics;
using SimpleGlamourSwitcher.Service;

namespace SimpleGlamourSwitcher.Utility;

public class Cached<T>(TimeSpan maxAge, Func<T> getValue) {
    private readonly Stopwatch age = Stopwatch.StartNew();
    private bool clear;

    public T Value {
        get {
            if (!clear && field != null && age.Elapsed <= maxAge) return field;
            clear = false;
            field = getValue();
            age.Restart();
            return field;
        }
    } = getValue();

    public void Clear() {
        clear = true;
    }
}

public class RevokeableCached<T>(Func<T> getValue) : IDisposable {
    private bool clear;
    private bool disposed;
    
    public T Value {
        get {
            
            if (!clear && field != null) return field;
            clear = false;
            PluginState.InvalidateEntryCache -= Clear;
            if (!disposed) PluginState.InvalidateEntryCache += Clear;
            field = getValue();
            return field;
        }
    } = getValue();

    public void Clear() {
        PluginState.InvalidateEntryCache -= Clear;
        clear = true;
    }
    
    public void Dispose() {
        disposed = true;
        PluginState.InvalidateEntryCache -= Clear;
    }
}

public class NullableCached<T>(TimeSpan maxAge, Func<T?> getValue) where T : class {
    private readonly Stopwatch age = Stopwatch.StartNew();

    private bool isFetched;
    private bool clear;

    public bool HasValue => isFetched && age.Elapsed <= maxAge;
    public TimeSpan Age => age.Elapsed;

    public T? Value {
        get {
            if (!clear && isFetched && age.Elapsed <= maxAge) return field;
            clear = false;
            field = getValue();
            isFetched = true;
            age.Restart();
            return field;
        }
    } = getValue();

    public void Clear() {
        clear = true;
    }
}
