using System.Diagnostics;

namespace SimpleGlamourSwitcher.Utility;

public class Cached<T>(TimeSpan maxAge, Func<T> getValue) {
    private readonly Stopwatch age = Stopwatch.StartNew();
    
    public T Value {
        get {
            if (field != null && age.Elapsed <= maxAge) return field;
            field = getValue();
            age.Restart();
            return field;
        }
    } = getValue();
}

public class NullableCached<T>(TimeSpan maxAge, Func<T?> getValue) where T : class {
    private readonly Stopwatch age = Stopwatch.StartNew();

    private bool isFetched;

    public bool HasValue => isFetched && age.Elapsed <= maxAge;
    public TimeSpan Age => age.Elapsed;
    
    public T? Value {
        get {
            if (isFetched && age.Elapsed <= maxAge) return field;
            field = getValue();
            isFetched = true;
            age.Restart();
            return field;
        }
    } = getValue();
}
