﻿using SimpleGlamourSwitcher.Configuration;

namespace SimpleGlamourSwitcher.Service;

public static class PluginState {
    public static bool TryGetActiveCharacterGuid(out Guid guid) {
        return PluginConfig.SelectedCharacter.TryGetValue(ClientState.LocalContentId, out guid);
    }
    
    
    public static void OnLogout(int type, int code) {
        PluginLog.Verbose($"OnLogout(type: {type} , code: {code})");
        ActionQueue.Clear();
        Config.SwitchCharacter(null, false);
        Plugin.MainWindow.IsOpen = false;
    }
    
    public static void LoadActiveCharacter(bool isLogin, bool isPluginStartup = false) {
        PluginLog.Verbose($"LoadActiveCharacter(isLogin: {isLogin}, isPluginStartup: {isPluginStartup})");
        if (TryGetActiveCharacterGuid(out var guid)) {
            PluginLog.Verbose($"Loading character with guid: {guid}");

            Config.SwitchCharacter(guid, false);

            if (ActiveCharacter == null) {
                PluginLog.Debug("Failed to load character.");
                return;
            }
            
            PluginLog.Verbose($"Loaded character: {ActiveCharacter.Name}");

            if (isLogin && ActiveCharacter.ApplyOnLogin) {
                GlamourSystem.ApplyCharacter(isLogin: isLogin).ConfigureAwait(false);
            } else if (isPluginStartup && ActiveCharacter.ApplyOnPluginReload) {
                GlamourSystem.ApplyCharacter(isLogin: isPluginStartup).ConfigureAwait(false);
            }
        }
    }

    public static void OnLogin() {
        ActionQueue.Clear();
        PluginLog.Verbose($"OnLogin()");
        LoadActiveCharacter(true);
    }
    public static void Initialize() {
        PluginLog.Verbose("Initialize()");
        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        
        if (ClientState.IsLoggedIn) {
            LoadActiveCharacter(false);
        }
    }

    public static void Dispose() {
        PluginLog.Verbose("Dispose()");
        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;
    }

    
    public static void ShowGlamourSwitcher() {
        PluginLog.Verbose("ShowGlamourSwitcher()");
        Plugin.MainWindow.Toggle();
    }
    
}
