using Dalamud.Interface.Utility.Raii;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.UserInterface.Components;

namespace SimpleGlamourSwitcher.Utility;

public class CustomRaii : IDisposable {

    private IDisposable[] disposables = [];
    
    public static CustomRaii PushColors(Colour colour, params ImGuiCol[] cols) => PushColors(colour, true, cols);
    public static CustomRaii PushColors(Colour colour, bool condition, params ImGuiCol[] cols) {
        return new CustomRaii {
            disposables = cols.Select(c => ImRaii.PushColor(c, colour.U32, condition)).ToArray<IDisposable>()
        };
    }
    
    
    
    public void Dispose() {
        foreach(var d in disposables) d.Dispose();
    }
}
