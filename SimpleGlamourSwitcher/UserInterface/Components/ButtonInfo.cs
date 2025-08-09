using Dalamud.Interface;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Bindings.ImGui;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public class ButtonInfo {

    public ButtonInfo() { }

    public ButtonInfo(FontAwesomeIcon icon, string text, Action onClick) {
        Text = text;
        Icon = icon;
        Action = onClick;
    }
    
    public ButtonInfo(string text, Action onClick) {
        Text = text;
        Icon = 0;
        Action = onClick;
    }

    public string Text { get; init; } = string.Empty;
    public string Tooltip { get; init; } = string.Empty;
    public Action Action { get; init; } = () => { };
    public FontAwesomeIcon Icon { get; init; } = 0;
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    public int DisplayPriority { get; init; } = 0;

    public bool Disabled {
        get => IsDisabled();
        set => IsDisabled = () => value;
    }

    public Func<bool> IsDisabled { get; set; } = () => false;


}
