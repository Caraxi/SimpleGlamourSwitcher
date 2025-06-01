using System.Diagnostics.CodeAnalysis;

namespace SimpleGlamourSwitcher.Utility;

public class LazyAsync<T>(Func<Task<T>> func) where T : new() {
    
    private Task<T>? task;

    public bool IsValueCreated => task is { IsCompletedSuccessfully: true };

    public void CreateValueIfNotCreated() {
        task ??= func();
    }
    

    [field: AllowNull, MaybeNull]
    public T Value {
        get {
            if (field != null) return field;

            task ??= func();

            if (!task.IsCompletedSuccessfully) return new T();
            
            field = task.Result;
            return field;
        }
    }
}
