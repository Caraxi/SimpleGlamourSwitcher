using Dalamud.Interface;
using ImGuiNET;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public class ButtonInfo {

    public ButtonInfo() { }

    public ButtonInfo(string text, Action onClick) {
        Text = text;
        Action = onClick;
    }

    public ButtonInfo(FontAwesomeIcon icon, Action onClick) {
        Text = icon.ToIconString();
        Action = onClick;
        Font = UiBuilder.IconFont;
    }

    public string Text { get; init; } = string.Empty;
    public string Tooltip { get; init; } = string.Empty;
    public Action Action { get; init; } = () => { };
    public ImFontPtr Font { get; init; } = UiBuilder.DefaultFont;
    public string Id { get; init; } = Guid.NewGuid().ToString();
    
    public int DisplayPriority { get; init; } = 0;

    public bool Disabled {
        get => IsDisabled();
        set => IsDisabled = () => value;
    }

    public Func<bool> IsDisabled { get; set; } = () => false;


}
