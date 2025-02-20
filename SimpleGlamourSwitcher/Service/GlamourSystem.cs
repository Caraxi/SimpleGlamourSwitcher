using Penumbra.Api.Enums;
using SimpleGlamourSwitcher.IPC;

namespace SimpleGlamourSwitcher.Service;

public static class GlamourSystem {

    public static async Task ApplyCharacter() {
        if (ActiveCharacter == null) return;
        ModManager.RemoveAllMods();
        Notice.Show($"Applying Character: {ActiveCharacter.Name}");

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

        PluginLog.Warning("Redrawing Character");

        await Task.Delay(1000);
        
        await Framework.RunOnFrameworkThread(() => {
            PenumbraIpc.RedrawObject.Invoke(0);
        });

    }
    
    
}
