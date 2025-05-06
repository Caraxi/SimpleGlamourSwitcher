using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
