using System.Diagnostics;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Penumbra.Api.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Page;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.UserInterface.Windows;

public class ActiveGearWindow() : Window("SGS###SimpleGlamourSwitcherEquipped", ImGuiWindowFlags.AlwaysAutoResize) {

    private OutfitConfigFile? OutfitCache { get; set; }
    private OutfitConfigFile? updatedOutfitCache;
    private EmoteConfigFile? EmoteCache;
    private Stopwatch updateEmoteCacheStopwatch = Stopwatch.StartNew();
    
    private bool updatingCache;
    private bool dirty;
    private bool applyingOutfit;
    private bool compact;
    private bool locked;

    private void UpdateButtons() {
        TitleBarButtons = [];

        if (locked) {
            TitleBarButtons.Add(lockButton);
            return;
        }

        TitleBarButtons.Add(compactButton);
        if (compact) {
            TitleBarButtons.Add(lockButton);
        }
    }

    private readonly TitleBarButton compactButton = new() {
        Icon = FontAwesomeIcon.ArrowRight,
        ShowTooltip = () => { },
    };

    private readonly TitleBarButton lockButton = new() {
        Icon = FontAwesomeIcon.Lock,
        ShowTooltip = () => { },
    };

    public override void OnClose() {
        if (Plugin.IsDisposing) return;
        PluginConfig.EquippedWindowConfig.WindowOpen = false;
        PluginConfig.Save(true);
        GlamourerIpc.StateChanged.Event -= OnGlamourerStateChanged;
        PenumbraIpc.ModSettingChanged.Event -= OnPenumbraSettingChanged;
    }

    private void OnPenumbraSettingChanged(ModSettingChange change, Guid collectionGuid, string modDirectory, bool inherited) {
        if (PenumbraIpc.GetCollectionForObject.Invoke(0).EffectiveCollection.Id == collectionGuid) UpdateOutfit();
    }

    private void OnGlamourerStateChanged(IntPtr obj) {
        if (obj == Objects.LocalPlayer?.Address) UpdateOutfit();
    }

    public override bool DrawConditions() => ActiveCharacter != null && PlayerStateService.IsLoaded && Objects.LocalPlayer != null;

    public override void OnOpen() {
        PluginConfig.EquippedWindowConfig.WindowOpen = true;

        compact = PluginConfig.EquippedWindowConfig.UseCompactWindow;
        if (compact) {
            locked = PluginConfig.EquippedWindowConfig.LockWindow;
        } else {
            PluginConfig.EquippedWindowConfig.LockWindow = false;
        }
        PluginConfig.Save(true);

        compactButton.Click = ToggleCompactMode;
        lockButton.Click = ToggleLock;
        UpdateButtons();

        dirty = false;
        updatingCache = false;

        AllowClickthrough = false;
        AllowPinning = false;
        RespectCloseHotkey = false;
        UpdateOutfit();
        UpdateEmote();

        GlamourerIpc.StateChanged.Event -= OnGlamourerStateChanged;
        PenumbraIpc.ModSettingChanged.Event -= OnPenumbraSettingChanged;
        GlamourerIpc.StateChanged.Event += OnGlamourerStateChanged;
        PenumbraIpc.ModSettingChanged.Event += OnPenumbraSettingChanged;
    }

    private void ToggleLock(ImGuiMouseButton obj) {
        PluginConfig.EquippedWindowConfig.LockWindow = locked = !locked;
        PluginConfig.Save(true);
        UpdateButtons();
    }

    private void ToggleCompactMode(ImGuiMouseButton obj) {
        PluginConfig.EquippedWindowConfig.UseCompactWindow = compact = !compact;
        PluginConfig.Save(true);
        UpdateButtons();
    }


    public override void PreDraw() {
        if (compact && locked) {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, Vector4.Zero);
        } else {
            ImGui.PushStyleColor(ImGuiCol.TitleBg, ImGui.GetColorU32(ImGuiCol.TitleBg));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGui.GetColorU32(ImGuiCol.TitleBgActive));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, ImGui.GetColorU32(ImGuiCol.TitleBgCollapsed));
        }
    }

    public override void Update() {
        compactButton.Icon = compact ? FontAwesomeIcon.ArrowRight : FontAwesomeIcon.ArrowLeft;
        lockButton.Icon = locked ? FontAwesomeIcon.Lock : FontAwesomeIcon.LockOpen;
        ShowCloseButton = !compact;
        WindowName = compact ? "SGS###SimpleGlamourSwitcherEquipped" : "Simple Glamour Switcher | Equipped###SimpleGlamourSwitcherEquipped";
        if (locked) {
            Flags |= ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove;
        } else {
            Flags &= ~(ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoMove);
        }
    }

    public override void PostDraw() {
        ImGui.PopStyleColor(3);
    }

    private async void UpdateEmote() {
        try {
            
            var chr = ActiveCharacter;
            if (chr == null) return;

            await Framework.RunOnFrameworkThread(() => {
                EmoteCache = EmoteConfigFile.CreateFromLocalPlayer(chr, Guid.Empty, DefaultOptions.Empty);
            });
            
        } catch (Exception e) {
            PluginLog.Error(e, "Error updating Emote");
            //
        }
    }
    
    private void UpdateOutfit() {
        if (updatingCache || dirty) return;
        updatingCache = true;
        PluginLog.Warning("Updating Active Window Cache");
        Task.Run(() => {
            try {
                var outfit = OutfitCache;
                var chr = ActiveCharacter;
                if (chr == null) {
                    outfit = null;
                } else if (outfit == null) {
                    outfit = OutfitConfigFile.CreateFromLocalPlayer(chr, Guid.Empty, DefaultOptions.Equipment);
                } else {
                    var glamourerState = GlamourerIpc.GetState(0);
                    var penumbraCollection = PenumbraIpc.GetCollectionForObject.Invoke(0);
                    if (glamourerState != null) {
                        var newAppearance = OutfitEquipment.FromExistingState(DefaultOptions.Equipment, glamourerState, penumbraCollection.EffectiveCollection.Id);
                        foreach (var a in Common.GetGearSlots()) {
                            try {
                                outfit.Equipment[a].TryUpdate(newAppearance[a], UpdateApplicableFlags.SkipApply);
                            } catch {
                                //
                            }
                        }

                        foreach (var a in Enum.GetValues<ToggleType>()) {
                            try {
                                outfit.Equipment[a].TryUpdate(newAppearance[a], UpdateApplicableFlags.SkipApply);
                            } catch {
                                //
                            }
                        }
                    }
                }

                updatedOutfitCache = outfit;
                updatingCache = false;
                return Task.CompletedTask;
            } catch (Exception exception) {
                return Task.FromException(exception);
            }
        });
    }


    private int quickSwitchFrameCounter;
    
    private static LazyAsync<OrderedDictionary<Guid, EmoteConfigFile>> emoteList = new(() => ActiveCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, EmoteConfigFile>()) : ActiveCharacter.GetEntries<EmoteConfigFile>());
    private static LazyAsync<OrderedDictionary<Guid, EmoteConfigFile>> sharedEmoteList = new(() => SharedCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, EmoteConfigFile>()) : SharedCharacter.GetEntries<EmoteConfigFile>());

    public override void Draw() {
        var outfit = OutfitCache;
        if (!dirty && !applyingOutfit && updatedOutfitCache != null) {
            OutfitCache = updatedOutfitCache;
            outfit = OutfitCache;
            updatedOutfitCache = null;
        } else if (dirty && !applyingOutfit && outfit != null) {
            applyingOutfit = true;
            outfit.Apply().ContinueWith(_ => {
                dirty = false;
                updatedOutfitCache = null;
                applyingOutfit = false;
            });
        }

        outfit ??= OutfitConfigFile.Create(ActiveCharacter);

        dirty |= EquipmentDisplay.DrawEquipment(outfit.Equipment, EquipmentDisplayFlags.Simple | EquipmentDisplayFlags.EnableCustomItemPicker | EquipmentDisplayFlags.ContextShowSaveSlot | (compact ? EquipmentDisplayFlags.Compact : EquipmentDisplayFlags.None));

        if (PluginConfig.EquippedWindowConfig.ShowSaveButton && ActiveCharacter != null) {
            if (compact) {
                var s = new Vector2(ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);
                ImGui.SetCursorScreenPos(ImGui.GetItemRectMax() - s with { X = -1 });
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Save, s)) {
                    Plugin.MainWindow.IsOpen = true;
                    Plugin.MainWindow.OpenPage(new EditOutfitPage(ActiveCharacter, Guid.Empty, outfit) { IsNew = true });
                }
                if (ImGui.IsItemHovered()) {
                    ImGui.SetTooltip("Save Outfit");
                }
            } else {
                ImGui.SetCursorScreenPos(ImGui.GetItemRectMax() - new Vector2(-ImGui.GetStyle().ItemSpacing.X, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2));
                if (ImGui.Button("Save Outfit", ImGui.GetContentRegionAvail() with { Y = ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2})) {
                    Plugin.MainWindow.IsOpen = true;
                    Plugin.MainWindow.OpenPage(new EditOutfitPage(ActiveCharacter, Guid.Empty, outfit) { IsNew = true });
                }
            }
        }

        if (PluginConfig.EquippedWindowConfig.ShowEmote) {
            var emote = EmoteCache ?? EmoteConfigFile.Create(ActiveCharacter);
            if (updateEmoteCacheStopwatch.ElapsedMilliseconds > 1000) {
                updateEmoteCacheStopwatch.Restart();
                UpdateEmote();
            }
            
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One)) {
                GameIcon.Draw(emote.EmoteIdentifier?.Icon ?? 0);
                
                if (ImGui.IsPopupOpen($"EmotePicker")) {
                    var dl = ImGui.GetWindowDrawList();
                    dl.AddRectFilled(
                        ImGui.GetItemRectMin() - Vector2.One,
                        ImGui.GetItemRectMax() + new Vector2(8 * ImGuiHelpers.GlobalScale, 1),
                        ImGui.ColorConvertFloat4ToU32(ImGuiColors.DalamudViolet));
                }
                
                if (ImGui.IsItemHovered()) {
                    var dl = ImGui.GetWindowDrawList();
                    dl.AddRect(ImGui.GetItemRectMin() - Vector2.One, ImGui.GetItemRectMax() + Vector2.One, ImGui.GetColorU32(ImGuiCol.ButtonHovered));
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                }

                if (ImGui.IsItemClicked()) {
                    ImGui.OpenPopup($"EmotePicker");
                    ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() with { Y = -1 } + new Vector2(5 * ImGuiHelpers.GlobalScale, 0));
                }
                
                var activeEmoteName = emote.EmoteIdentifier?.Name ?? "No Emote Active";
                
                using (ImRaii.PushStyle(ImGuiStyleVar.PopupBorderSize, 2))
                using (ImRaii.PushColor(ImGuiCol.Border, ImGuiColors.DalamudViolet))
                using (var popup = ImRaii.Popup($"EmotePicker")) {
                    if (popup.Success) {
                        var s = new Vector2(300 * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);

                        if (compact) {
                            ImGui.SetNextItemWidth(s.X);
                            ImGui.InputText("##emoteName", ref activeEmoteName, 128, ImGuiInputTextFlags.ReadOnly);
                            dirty |= ModListDisplay.Show(emote, activeEmoteName, displayOnly: true, includeCustomizePlus: false);
                        }
                        
                        using (var child = ImRaii.Child($"EmotePicker_scroll", (s * 1.35f) with { Y = s.Y * 10 }, false, ImGuiWindowFlags.HorizontalScrollbar | ImGuiWindowFlags.AlwaysHorizontalScrollbar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                            if (child.Success) {
                                if (ImGui.IsWindowAppearing()) {
                                    quickSwitchFrameCounter = 0;
                                    emoteList = new(() => ActiveCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, EmoteConfigFile>()) : ActiveCharacter.GetEntries<EmoteConfigFile>());
                                    sharedEmoteList = new(() => SharedCharacter == null ? Task.FromResult(new OrderedDictionary<Guid, EmoteConfigFile>()) : SharedCharacter.GetEntries<EmoteConfigFile>());
                                }
                                quickSwitchFrameCounter++;
                                if (ImGui.IsWindowHovered()) {
                                    ImGui.SetScrollX(ImGui.GetScrollX() - ImGui.GetIO().MouseWheel * 50 * ImGuiHelpers.GlobalScale);
                                }

                                emoteList.CreateValueIfNotCreated();
                                sharedEmoteList.CreateValueIfNotCreated();
                                
                                if (!emoteList.IsValueCreated || !sharedEmoteList.IsValueCreated) {
                                    ImGui.TextDisabled("Loading Emotes...");
                                } else {
                                    var any = false;
                                    var row = 0;
                                    var rowCount = Math.Clamp(PluginConfig.EquippedWindowConfig.QuickSwitchEmoteRowCount, 1, EquippedWindowConfig.QuickSwitchMaxRows);

                                    var contentRegionSize = ImGui.GetWindowContentRegionMax() - ImGui.GetWindowContentRegionMin();
                                    var rowHeight = contentRegionSize.Y / rowCount;

                                    var group = ImRaii.Group();
                                    try {

                                        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero)) {
                                            foreach (var entry in emoteList.Value.Values.Concat(sharedEmoteList.Value.Values).Where(e => e.EmoteIdentifier?.EmoteModeId == emote.EmoteIdentifier?.EmoteModeId).OrderBy(i => i.SortName).ThenBy(i => i.Name, StringComparer.InvariantCultureIgnoreCase)) {

                                                if (any) {
                                                    if (row >= rowCount) {
                                                        group.Dispose();
                                                        ImGui.SameLine();
                                                        group = ImRaii.Group();
                                                        row = 0;
                                                    }
                                                }

                                                any = true;
                                                row++;
                                                var folder = entry.ConfigFile?.Folders.GetValueOrDefault(entry.Folder);
                                                var style = folder?.OutfitPolaroidStyle ??
                                                            entry.ConfigFile?.OutfitPolaroidStyle ?? (PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;

                                                if (Polaroid.Button(
                                                        (entry as IImageProvider).GetImageOrNull,
                                                        entry.ImageDetail,
                                                        entry.Name,
                                                        entry.Guid,
                                                        style.FitTo(contentRegionSize with { Y = rowHeight })) && quickSwitchFrameCounter > 3) {
                                                    entry.Apply().ConfigureAwait(false);
                                                }
                                            }
                                        }

                                    } finally {
                                        group.Dispose();
                                    }

                                    if (!any) {
                                        ImGui.TextDisabled($"No custom emotes saved.");
                                    }
                                }
                            }
                        }
                    }
                }
                
                if (ActiveCharacter != null && ImGui.BeginPopupContextItem("contextActiveEmote")) {
                    ImGui.Text("Emote");
                    ImGui.Separator();
                    if (ImGui.MenuItem("Save Emote")) {
                        Plugin.MainWindow.IsOpen = true;
                        Plugin.MainWindow.OpenPage(new EditEmotePage(ActiveCharacter, Guid.Empty, emote) { IsNew = true });
                    }
                    
                    ImGui.EndPopup();
                }
                
                ImGui.SameLine();
                using (ImRaii.Group()) {
                    if (compact) {
                        ModListDisplay.ShowModLinkButton(emote, true);
                    } else {
                        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
                        ImGui.InputText("##emoteName", ref activeEmoteName, 128, ImGuiInputTextFlags.ReadOnly);
                        dirty |= ModListDisplay.Show(emote, activeEmoteName, displayOnly: true, includeCustomizePlus: false);
                    }
                }
            }
        }
    }
}
