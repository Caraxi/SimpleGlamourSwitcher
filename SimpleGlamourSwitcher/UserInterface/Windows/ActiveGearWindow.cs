using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Penumbra.Api.Enums;
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
    private OutfitConfigFile? updatedCache;
    private bool updatingOutfit;
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
        ShowTooltip = () => { }
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
        updatingOutfit = false;
        
        AllowClickthrough = false;
        AllowPinning = false;
        RespectCloseHotkey = false;
        UpdateOutfit();

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

    private void UpdateOutfit() {
        if (updatingOutfit || dirty) return;
        updatingOutfit = true;
        Task.Run(() => {

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

            updatedCache = outfit;
            updatingOutfit = false;
        });
    }
    

    
    public override void Draw() {
        var outfit = OutfitCache;
        if (!dirty && updatedCache != null && !applyingOutfit) {
            OutfitCache = updatedCache;
            outfit = OutfitCache;
            updatedCache = null;
        } else if (dirty && !applyingOutfit && outfit != null) {
            applyingOutfit = true;
            outfit.Apply().ContinueWith(_ => {
                dirty = false;
                updatedCache = null;
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
                    Plugin.MainWindow.OpenPage(new EditOutfitPage(ActiveCharacter, Guid.Empty, outfit));
                }
            } else {
                ImGui.SetCursorScreenPos(ImGui.GetItemRectMax() - new Vector2(-ImGui.GetStyle().ItemSpacing.X, ImGui.GetTextLineHeightWithSpacing()));
                if (ImGui.Button("Save Outfit", ImGui.GetContentRegionAvail())) {
                    Plugin.MainWindow.IsOpen = true;
                    Plugin.MainWindow.OpenPage(new EditOutfitPage(ActiveCharacter, Guid.Empty, outfit));
                }
            }
        }
    }
}
