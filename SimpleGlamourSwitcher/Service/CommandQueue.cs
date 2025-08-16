using System.Collections.Concurrent;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace SimpleGlamourSwitcher.Service;

public class ActionQueueService : ConcurrentQueue<Action>, IDisposable {
    public void Initialize() {
        Framework.Update += TickQueue;
    }

    private static unsafe void SendMessage(string message) {
        var utf8 = Utf8String.FromString(message);

        try {
            if (utf8->Length == 0) {
                throw new ArgumentException("message is empty", nameof(message));
            }

            if (utf8->Length > 500) {
                throw new ArgumentException("message is longer than 500 bytes", nameof(message));
            }

            var oldLength = utf8->Length;

            utf8->SanitizeString(AllowedEntities.UppercaseLetters | AllowedEntities.LowercaseLetters | AllowedEntities.Numbers | AllowedEntities.SpecialCharacters | AllowedEntities.CharacterList | AllowedEntities.OtherCharacters | AllowedEntities.Payloads |AllowedEntities.Unknown9);

            if (utf8->Length != oldLength) {
                throw new ArgumentException($"message contained invalid characters", nameof(message));
            }

            var uiModule = UIModule.Instance();
            if (uiModule == null) {
                throw new InvalidOperationException("The UiModule is currently unavailable");
            }

            uiModule->ProcessChatBoxEntry(utf8);
        } finally {
            if (utf8 != null) {
                utf8->Dtor(true);
            }
        }
    }

    public void QueueCommand(string command) {
        Enqueue(() => {
            Notice.Show($"Use Command: {command}");
            Framework.RunOnFrameworkThread(() => {
                try {
                    SendMessage(command);
                } catch (Exception ex) {
                    PluginLog.Error(ex, $"Failed to execute command '{command}'");
                    Chat.PrintError($"Failed to execute command '{command}'. {ex.Message}");
                }
            });
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
