using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using SimpleGlamourSwitcher.Service;

namespace SimpleGlamourSwitcher.Utility;

public static class HotkeyHelper {
    private static string? _settingKey;
    private static string? _focused;
    private static HashSet<VirtualKey> _newKeys = [];
    private static readonly Stopwatch Safety = Stopwatch.StartNew();

    private static readonly NativeKeyState NativeKeyState;

    public static IReadOnlySet<VirtualKey> HeldKeys => NativeKeyState.HeldKeys;

    static HotkeyHelper() {
        NativeKeyState = new NativeKeyState();
        NativeKeyState.Enable();
        NativeKeyState.OnKeystroke += NativeKeyStateOnOnKeystroke;
    }
    
    private static void NativeKeyStateOnOnKeystroke(VirtualKey key, bool down, ref NativeKeyState.KeyHandleType handleType) {
        if (down) return;

        if (key == VirtualKey.ESCAPE) {
            _settingKey = null;
            _focused = null;
            _newKeys = [];
            return;
        }
        
        
        if (key is VirtualKey.LMENU or VirtualKey.RMENU or VirtualKey.LSHIFT or VirtualKey.RSHIFT or VirtualKey.LCONTROL or VirtualKey.RCONTROL) return; // Ignore keyboard side
        if (key is VirtualKey.LWIN or VirtualKey.RWIN) return; // Ignore windows key
        
        if (IsSettingHotkey) {
            _newKeys.Add(key);
            handleType = NativeKeyState.KeyHandleType.Block;
        } else {
            
            // if (ClientState.LocalContentId == 0) return;
            // if (ClientState.LocalPlayer == null) return;
            if (IsSettingHotkey) return;
            if (Condition.Any(ConditionFlag.LoggingOut, ConditionFlag.BetweenAreas, ConditionFlag.BetweenAreas51, ConditionFlag.InCombat, ConditionFlag.WatchingCutscene, ConditionFlag.WatchingCutscene78, ConditionFlag.OccupiedInCutSceneEvent)) return;
            if (HeldKeys.SetEquals(PluginConfig.Hotkey)) {
                PluginState.ShowGlamourSwitcher();
                handleType = NativeKeyState.KeyHandleType.Block;
            }
            
        }
        
    }

    public static bool IsSettingHotkey => !string.IsNullOrEmpty(_settingKey);
    
    private static void CheckSafety() {
        if (Safety is { IsRunning: true, ElapsedMilliseconds: > 500 }) {
            PluginLog.Verbose("Hotkey editor safety triggered.");
            _settingKey = null;
            _focused = null;
            Safety.Reset();
        } 
    }

    private static readonly Dictionary<VirtualKey, string> NamedKeys = new() {
        { VirtualKey.KEY_0, "0"},
        { VirtualKey.KEY_1, "1"},
        { VirtualKey.KEY_2, "2"},
        { VirtualKey.KEY_3, "3"},
        { VirtualKey.KEY_4, "4"},
        { VirtualKey.KEY_5, "5"},
        { VirtualKey.KEY_6, "6"},
        { VirtualKey.KEY_7, "7"},
        { VirtualKey.KEY_8, "8"},
        { VirtualKey.KEY_9, "9"},
        { VirtualKey.CONTROL, "Ctrl"},
        { VirtualKey.MENU, "Alt"},
        { VirtualKey.SHIFT, "Shift"},
        { VirtualKey.OEM_3, "Tilde"}
    };
    
    public static string GetKeyName(this VirtualKey k) => NamedKeys.TryGetValue(k, out var value) ? value : k.ToString();
    
    public static bool DrawHotkeyConfigEditor(string name, HashSet<VirtualKey> keys, [NotNullWhen(true)] out HashSet<VirtualKey>? outKeys) {
        outKeys = [];
        var modified = false;
        var identifier = name.Contains("###") ? $"{name.Split("###", 2)[1]}" : name;
        var strKeybind = keys.Count == 0 ? "Not Set" : string.Join("+", keys.Select(k => k.GetKeyName()));

        ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);

        if (_settingKey == identifier) {
            CheckSafety();
            /*
            if (ImGui.GetIO()
                    .KeyAlt && !_newKeys.Contains(VirtualKey.MENU))
                _newKeys.Add(VirtualKey.MENU);
            if (ImGui.GetIO()
                    .KeyShift && !_newKeys.Contains(VirtualKey.SHIFT))
                _newKeys.Add(VirtualKey.SHIFT);
            if (ImGui.GetIO()
                    .KeyCtrl && !_newKeys.Contains(VirtualKey.CONTROL))
                _newKeys.Add(VirtualKey.CONTROL);

            for (var k = 0;
                 k < ImGui.GetIO()
                     .KeysDown.Count && k < 160;
                 k++) {
                if (ImGui.GetIO()
                    .KeysDown[k]) {
                    if (!_newKeys.Contains((VirtualKey)k)) {
                        if ((VirtualKey)k == VirtualKey.ESCAPE) {
                            _settingKey = null;
                            _newKeys.Clear();
                            _focused = null;
                            break;
                        }

                        _newKeys.Add((VirtualKey)k);
                    }
                }
            }
            */
            var sorted = _newKeys.ToList();
            sorted.Sort();
            
            strKeybind = string.Join("+", sorted.Select(k => k.GetKeyName()));
            
        }

        using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 2))
        using (ImRaii.PushColor(ImGuiCol.Border, 0xFF00A5FF, _settingKey == identifier)) {
            ImGui.InputText(name, ref strKeybind, 100, ImGuiInputTextFlags.ReadOnly);
        }

        var active = ImGui.IsItemActive();

        if (_settingKey == identifier) {
            if (_focused != identifier) {
                ImGui.SetKeyboardFocusHere(-1);
                _focused = identifier;
            } else {
                Safety.Restart();
                ImGui.SameLine();
                
                if (ImGui.Button(_newKeys.Count > 0 ?  $"Confirm##{identifier}" : $"Cancel##{identifier}")) {
                    Safety.Reset();
                    _settingKey = null;
                    if (_newKeys.Count > 0) {
                        outKeys = _newKeys;
                        modified = true;
                        _newKeys = [];
                    }

                    _newKeys.Clear();
                } else {
                    if (!active) {
                        Safety.Reset();
                        _focused = null;
                        _settingKey = null;
                        if (_newKeys.Count > 0) {
                            outKeys = _newKeys;
                            modified = true;
                        }

                        _newKeys = [];
                    }
                }
            }
        } else {
            ImGui.SameLine();
            if (ImGui.Button($"Set Keybind###setHotkeyButton{identifier}")) {
                Safety.Restart();
                _settingKey = identifier;
                _newKeys = [];
            }
            
            ImGui.SameLine();
            if (ImGui.Button($"Clear Keybind###clearKeybind_{identifier}")) {
                modified = true;
                _newKeys = [];
                outKeys = [];
            }
            
            return modified;
           
        }

        return modified;
    }


    public static void Dispose() {
        NativeKeyState.Dispose();
    }

    public static void Initialize() {
        
    }
}
