using Dalamud.Plugin.Services;

namespace SimpleGlamourSwitcher.Service;

public class CleanupManager : IDisposable {

    private List<Action> cleanupActions = [];

    public event Action Cleanup {
        add {
            cleanupActions.Remove(value);
            cleanupActions.Add(value);
        }
        remove => cleanupActions.Remove(value);
    }

    private readonly CancellationTokenSource cancellationTokenSource = new();

    private IFramework framework;
    public CleanupManager(IFramework framework) {
        this.framework = framework;
        QueueCleanup();
    }

    private void QueueCleanup() {
        framework.RunOnTick(() => {
            if (cancellationTokenSource.IsCancellationRequested) return;
            QueueCleanup();
            PluginLog.Verbose("Running Cleanup");
            try {
                foreach (var action in cleanupActions) {
                    try {
                        if (cancellationTokenSource.IsCancellationRequested) return;
                        action();
                    } catch (Exception ex) {
                        PluginLog.Error(ex, "Error in cleanup action");
                    }
                }
            } catch (Exception ex) {
                PluginLog.Error(ex, "Error in cleanup");
            }

        }, cancellationToken: cancellationTokenSource.Token, delay: TimeSpan.FromSeconds(60));
    }

    public void Dispose() {
        cancellationTokenSource.Cancel();
        cleanupActions = [];
    }
}
