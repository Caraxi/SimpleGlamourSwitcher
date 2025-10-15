using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Glamourer.Api.Enums;
using Dalamud.Bindings.ImGui;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Service;

namespace SimpleGlamourSwitcher.UserInterface.Windows;

public unsafe class DebugWindow() : Window("Simple Glamour Switcher Debug") {
    private OrderedDictionary<Guid, IListEntry> entries = new();
    private List<OutfitConfigFile> stack = new();
    public override void Draw() {

        if (ImGui.Button("Copy Glamourer State")) {
            var glamourerState = GlamourerIpc.GetStateJson.Invoke(0);
            if (glamourerState is { Item1: GlamourerApiEc.Success, Item2: not null }) {
                ImGui.SetClipboardText(glamourerState.Item2.ToString());
            }
        }

        if (ImGui.Button("Fetch Outfits")) {
            entries = new OrderedDictionary<Guid, IListEntry>();
            ActiveCharacter?.GetEntries().ContinueWith((t) => {

                var outfitEntries = t.Result.Select((kvp) => {
                    var fullPath = ActiveCharacter.ParseFolderPath(kvp.Value.Folder) + " / " + kvp.Value.Name;
                    return (kvp.Key, kvp.Value, fullPath);
                });
                
                foreach (var (guid, outfit, fullPathName) in outfitEntries.OrderBy(outfitEntry => outfitEntry.fullPath)) {
                    entries.TryAdd(guid, outfit);    
                }
            });
        }

        if (ImGui.CollapsingHeader("Outfits")) {


            if (ImGui.BeginTable("outfits", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders)) {
                foreach (var (guid, entry) in entries) {
                    using (ImRaii.PushId($"{guid}")) {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{guid}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{ActiveCharacter?.ParseFolderPath(entry.Folder)} / {entry.Name}");
                        ImGui.TableNextColumn();
                        
                        if (entry is OutfitConfigFile outfit) {
                            if (ImGui.SmallButton("Copy Appearance JSON")) {
                                var a = GlamourerIpc.GetCustomizationJObject(outfit.Appearance, outfit.Equipment);
                                ImGui.SetClipboardText(a?.ToString() ?? "null");
                            }
                            ImGui.SameLine();
                            if (ImGui.SmallButton("+ Stack")) {
                                stack.Add(outfit);
                            }
                        }
                    }
                
                }
                
                ImGui.EndTable();
            }
        }

        if (ImGui.CollapsingHeader("Stacking")) {
            var stackIndex = -1;
            if (ImGui.BeginTable("outfitStack", 3, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders)) {
                foreach (var outfit in stack.ToArray()) {
                    using (ImRaii.PushId($"{outfit.Guid}_stack_{++stackIndex}")) {
                        ImGui.TableNextColumn();
                        ImGui.Text($"{outfit.Guid}");
                        ImGui.TableNextColumn();
                        ImGui.Text($"{ActiveCharacter?.ParseFolderPath(outfit.Folder)} / {outfit.Name}");
                        ImGui.TableNextColumn();
                        if (ImGui.SmallButton("- Stack")) {
                            stack.RemoveAt(stackIndex);
                        }
                    }
                }
                ImGui.EndTable();
            }

            if (ImGui.Button("Stack to Outfit")) {
                var result = GlamourSystem.StackOutfits(stack.ToArray());
                var outfit = OutfitConfigFile.Create(ActiveCharacter);
                outfit.Name = $"Stack: [{string.Join(", ", stack.Select(o => o.Name))}]";
                outfit.Appearance = result.Appearance;
                outfit.Equipment = result.Equipment;
                outfit.Save(true);
            }
        }
        
        if (ImGui.CollapsingHeader("Customize+")) {

            if (CustomizePlus.IsReady()) {
                
                ImGui.Text("Ready");

                var chr = ClientState.LocalPlayer;
                if (chr == null) {
                    ImGui.TextUnformatted("No Character");
                } else {

                    if (CustomizePlus.TryGetActiveProfileOnCharacter(0, out var data)) {
                        ImGui.TextUnformatted($"Active Profile: {data.Name}");
                        ImGui.SameLine();
                        ImGui.TextDisabled($"{data.UniqueId}");
                    }
                    
                    var profiles = CustomizePlus.GetProfileList();

                    foreach (var p in profiles) {
                        using (ImRaii.PushId(p.UniqueId.ToString())) {
                            var hasActiveCharacter = p.Characters.Any(c => c is { CharacterType: 1, CharacterSubType: 0 } && c.Name == chr.Name.TextValue && (c.WorldId == chr.HomeWorld.RowId || c.WorldId == ushort.MaxValue));
                            
                            if (hasActiveCharacter) {
                                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.DalamudRed)) {
                                    if (ImGui.Button("Remove Self", new Vector2(100, ImGui.GetTextLineHeightWithSpacing()))) {
                                        CustomizePlus.TryRemovePlayerCharacterFromProfile(p.UniqueId, chr.Name.TextValue, chr.HomeWorld.RowId);
                                        CustomizePlus.TryRemovePlayerCharacterFromProfile(p.UniqueId, chr.Name.TextValue, ushort.MaxValue);
                                    }
                                }
                                
                            } else {
                                using (ImRaii.PushColor(ImGuiCol.Text, ImGuiColors.ParsedGreen)) {
                                    if (ImGui.Button("Add Self", new Vector2(100, ImGui.GetTextLineHeightWithSpacing()))) {
                                        CustomizePlus.TryAddPlayerCharacterToProfile(p.UniqueId, chr.Name.TextValue, chr.HomeWorld.RowId);
                                    }
                                }
                                
                            }
                            ImGui.SameLine();
                            ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(p.IsEnabled ? ImGui.GetColorU32(ImGuiCol.Text) :  ImGui.GetColorU32(ImGuiCol.TextDisabled)), p.Name);

                        }
                    }
                }
            } else {
                ImGui.Text("Compatible C+ version not detected.");
            }
        }
    }
}
