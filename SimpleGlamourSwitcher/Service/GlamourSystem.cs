using Penumbra.Api.Enums;
using SimpleGlamourSwitcher.IPC;

namespace SimpleGlamourSwitcher.Service;

public static class GlamourSystem {

    public static async Task ApplyCharacter(bool revert = true) {
        if (ActiveCharacter == null) return;
        
        Notice.Show($"Applying Character: {ActiveCharacter.Name}");

        if (revert) {
            ModManager.RemoveAllMods();
            GlamourerIpc.RevertState.Invoke(0);
            await Task.Delay(250);
        }
        
        if (ActiveCharacter.PenumbraCollection != null) {
            PluginLog.Debug($"Set Penumbra Collection: {ActiveCharacter.PenumbraCollection}");
            PenumbraIpc.SetCollection.Invoke(ApiCollectionType.Current, ActiveCharacter.PenumbraCollection);
            PenumbraIpc.SetCollectionForObject.Invoke(0, ActiveCharacter.PenumbraCollection);
        }

        try {
            if (ActiveCharacter.CustomizePlusProfile != null && CustomizePlus.IsReady()) {
                var profileId = ActiveCharacter.CustomizePlusProfile.Value;
                PluginLog.Debug($"Set Customize Plus Profile: {ActiveCharacter.CustomizePlusProfile}");
                await Framework.RunOnTick(() => {
                    var player = ClientState.LocalPlayer;
                    if (player == null) return;

                    var playerName = player.Name.TextValue;
                    var playerHomeWorld = player.HomeWorld.RowId;
                    
                    foreach (var p in CustomizePlus.GetProfileList()) {
                        if (!p.IsEnabled) continue;
                        PluginLog.Debug($"Remove {playerName} @ {playerHomeWorld} from Customize Profile {p.UniqueId} / {p.Name}");
                        CustomizePlus.TryRemovePlayerCharacterFromProfile(p.UniqueId, playerName, playerHomeWorld);
                        CustomizePlus.TryRemovePlayerCharacterFromProfile(p.UniqueId, playerName, ushort.MaxValue);
                    }
                    
                    if (profileId != Guid.Empty) {
                        PluginLog.Debug($"Add {playerName} @ {playerHomeWorld} to Customize Profile {profileId}");
                        CustomizePlus.TryAddPlayerCharacterToProfile(profileId, playerName, playerHomeWorld);
                        CustomizePlus.EnableByUniqueId(profileId);
                    }
                });
            }
        } catch (Exception ex) {
            PluginLog.Warning(ex, "Failed to set customize plus profile.");
        }
        
        if (ActiveCharacter.DefaultOutfit != null) {
            var outfits = await ActiveCharacter.GetOutfits();
            if (outfits.TryGetValue(ActiveCharacter.DefaultOutfit.Value, out var defaultOutfit)) {
                defaultOutfit.Apply();
            }
        }

        try {
            HonorificIpc.SetLocalPlayerIdentity?.Invoke(ActiveCharacter.HonorificIdentity.Name, ActiveCharacter.HonorificIdentity.World);
        } catch (Exception ex) {
            PluginLog.Warning(ex, "Failed to set honorific identity.");
        }
        
        try {
            HeelsIpc.SetLocalPlayerIdentity?.Invoke(ActiveCharacter.HeelsIdentity.Name, ActiveCharacter.HeelsIdentity.World);
        } catch (Exception ex) {
            PluginLog.Warning(ex, "Failed to set heels identity.");
        }
        
        await Task.Delay(1000);
        PluginLog.Warning("Redrawing Character");
        await Framework.RunOnFrameworkThread(() => {
            PenumbraIpc.RedrawObject.Invoke(0);
        });
    }
}
