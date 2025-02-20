using System.Collections.Concurrent;
using Dalamud.Plugin.Services;

namespace SimpleGlamourSwitcher.Service;

public class ActionQueueService : ConcurrentQueue<Action>, IDisposable {
    public void Initialize() {
        Framework.Update += TickQueue;
    }

    public void QueueCommand(string command) {
        Enqueue(() => {
            Notice.Show($"Use Command: {command}");
        });
    }
    
    private void TickQueue(IFramework framework) {
        if (!TryDequeue(out var action)) return;
        action.Invoke();
    }

    public void Dispose() {
        Framework.Update -= TickQueue;
        Clear();
    }
}
