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

        if (ActiveCharacter.DefaultOutfit != null) {
            var outfits = await ActiveCharacter.GetOutfits();
            if (outfits.TryGetValue(ActiveCharacter.DefaultOutfit.Value, out var defaultOutfit)) {
                defaultOutfit.Apply();
            }
        }
        
        HonorificIpc.SetLocalPlayerIdentity?.Invoke(ActiveCharacter.HonorificIdentity.Name, ActiveCharacter.HonorificIdentity.World);
        
        await Task.Delay(1000);
        PluginLog.Warning("Redrawing Character");
        await Framework.RunOnFrameworkThread(() => {
            PenumbraIpc.RedrawObject.Invoke(0);
        });

    }
    
    
}
