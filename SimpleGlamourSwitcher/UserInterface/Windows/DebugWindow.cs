using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Glamourer.Api.Enums;
using ImGuiNET;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC;

namespace SimpleGlamourSwitcher.UserInterface.Windows;

public class DebugWindow() : Window("Simple Glamour Switcher Debug") {


    private OrderedDictionary<Guid, OutfitConfigFile> outfits = new();
    
    public override void Draw() {

        if (ImGui.Button("Copy Glamourer State")) {
            var glamourerState = GlamourerIpc.GetStateJson.Invoke(0);
            if (glamourerState is { Item1: GlamourerApiEc.Success, Item2: not null }) {
                ImGui.SetClipboardText(glamourerState.Item2.ToString());
            }
        }

        if (ImGui.Button("Fetch Outfits")) {
            outfits = new OrderedDictionary<Guid, OutfitConfigFile>();
            ActiveCharacter?.GetOutfits().ContinueWith((t) => {

                var outfitEntries = t.Result.Select((kvp) => {
                    var fullPath = ActiveCharacter.ParseFolderPath(kvp.Value.Folder) + " / " + kvp.Value.Name;
                    return (kvp.Key, kvp.Value, fullPath);
                });
                
                foreach (var (guid, outfit, fullPathName) in outfitEntries.OrderBy(outfitEntry => outfitEntry.fullPath)) {
                    outfits.TryAdd(guid, outfit);    
                }
            });
        }

        if (ImGui.CollapsingHeader("Outfits")) {


            if (ImGui.BeginTable("outfits", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders)) {
                foreach (var (guid, outfit) in outfits) {
                    using (ImRaii.PushId($"{guid}")) {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{guid}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{ActiveCharacter?.ParseFolderPath(outfit.Folder)} / {outfit.Name}");
                        ImGui.TableNextColumn();
                        if (ImGui.SmallButton("Copy Appearance JSON")) {
                            var a = GlamourerIpc.GetCustomizationJObject(outfit);
                            ImGui.SetClipboardText(a?.ToString() ?? "null");
                        }
                    }
                
                }
                
                ImGui.EndTable();
            }
            
           
        }
        
        
        
        

        
        
        



    }
}
