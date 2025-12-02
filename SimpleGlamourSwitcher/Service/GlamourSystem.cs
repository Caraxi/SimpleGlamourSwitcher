using Penumbra.Api.Enums;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Interface;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC;
using SimpleGlamourSwitcher.Utility;

namespace SimpleGlamourSwitcher.Service;

public static class GlamourSystem {

    public static async Task ApplyCharacter(bool revert = true, bool isLogin = false) {
        if (ActiveCharacter == null) return;
        
        Notice.Show($"Applying Character: {ActiveCharacter.Name}");

        if (revert) {
            ModManager.RemoveAllMods();
            GlamourerIpc.RevertStateName.Invoke(GameHelper.PlayerNameString);
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
                    var player = Objects.LocalPlayer;
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

        await ActiveCharacter.ApplyAutomation(isLogin, !isLogin);

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
    
    public static async Task<List<IListEntry>> GetOutfitLinks(OutfitConfigFile outfit, bool throwOnCircular = true) {
        if (outfit.ConfigFile == null) return [];
        
        List<IListEntry> outfitList = [];
        HashSet<Guid> guids = [];
        
        var outfits = await outfit.ConfigFile.GetEntries();
        
        void AddOutfit(OutfitConfigFile addOutfit) {
            if (guids.Contains(addOutfit.Guid)) return;
            
            foreach (var pre in addOutfit.ApplyBefore) {
                if (guids.Contains(pre)) {
                    if (throwOnCircular) throw new Exception("Circular Link Detected");
                    continue;
                }

                if (outfits.TryGetValue(pre, out var preEntry)) {
                    if (preEntry is OutfitConfigFile preOutfit) {
                        AddOutfit(preOutfit);
                    } else {
                        outfitList.Add(preEntry);
                        guids.Add(preEntry.Guid);
                    }
                }
            }

            if (guids.Contains(addOutfit.Guid)) {
                if (throwOnCircular) throw new Exception("Circular Link Detected");
                return;
            }
            
            outfitList.Add(addOutfit);
            guids.Add(addOutfit.Guid);
            
            foreach (var post in addOutfit.ApplyAfter) {

                if (guids.Contains(post)) {
                    if (throwOnCircular) throw new Exception("Circular Link Detected");
                    continue;
                }
                
                if (outfits.TryGetValue(post, out var postEntry)) {
                    if (postEntry is OutfitConfigFile postOutfit) {
                        AddOutfit(postOutfit);
                    } else {
                        outfitList.Add(postEntry);
                        guids.Add(postEntry.Guid);
                    }
                }
            }
        }

        AddOutfit(outfit);

        return outfitList;
    }
    
    
    public static async Task<(OutfitAppearance Appearance, OutfitEquipment Equipment, List<IAdditionalLink> Additionals)> HandleLinks(OutfitConfigFile outfit) {
        if (outfit.ConfigFile == null) return (outfit.Appearance, outfit.Equipment, []);
        var outfitList = await GetOutfitLinks(outfit);
        return StackOutfits(outfitList.ToArray());
    }
    
    public static (OutfitAppearance Appearance, OutfitEquipment Equipment, List<IAdditionalLink> Additionals) StackOutfits(params IListEntry[] entries) {
        var appearance = new OutfitAppearance();
        var equipment = new OutfitEquipment();
        var additionals = new List<IAdditionalLink>();
        
        foreach (var entry in entries) {
            if (entry is not OutfitConfigFile outfit) {
                if (entry is IAdditionalLink additionalLink) {
                    additionals.Add(additionalLink);
                }
                
                continue;
            }
            
            if (outfit.Appearance.Apply) {
                appearance.Apply = true;

                appearance.RevertToGame |= outfit.Appearance.RevertToGame;

                foreach (var e in Enum.GetValues<CustomizeIndex>()) {
                    if (!outfit.Appearance[e].Apply) continue;
                    
                    appearance[e].Apply = outfit.Appearance[e].Apply;
                    appearance[e].Value = outfit.Appearance[e].Value;

                    if (outfit.Appearance[e] is ApplicableCustomizeModable m && appearance[e] is ApplicableCustomizeModable am) {
                        am.ModConfigs = [];
                        foreach (var modConfig in m.ModConfigs) {
                            am.ModConfigs.Add(modConfig with { });
                        }
                    }
                    
                    if (outfit.Appearance[e] is IHasCustomizePlusTemplateConfigs c1 && appearance[e] is IHasCustomizePlusTemplateConfigs c2) {
                        c2.CustomizePlusTemplateConfigs = c1.CustomizePlusTemplateConfigs;
                    }
                }

                foreach (var e in Enum.GetValues<AppearanceParameterKind>()) {
                    if (!outfit.Appearance[e].Apply) continue;
                    
                    appearance[e].Apply = outfit.Appearance[e].Apply;
                    switch (outfit.Appearance[e]) {
                        case ApplicableParameterColorAlpha aColorAlpha when appearance[e] is ApplicableParameterColorAlpha bColorAlpha:
                            bColorAlpha.Red = aColorAlpha.Red;
                            bColorAlpha.Green = aColorAlpha.Green;
                            bColorAlpha.Blue = aColorAlpha.Blue;
                            bColorAlpha.Alpha = aColorAlpha.Alpha;
                            break;
                        case ApplicableParameterColor aColor when appearance[e] is ApplicableParameterColor bColor:
                            bColor.Red = aColor.Red;
                            bColor.Green = aColor.Green;
                            bColor.Blue = aColor.Blue;
                            break;
                        case ApplicableParameterFloat aFloat when appearance[e] is ApplicableParameterFloat bFloat:
                            bFloat.Value = aFloat.Value;
                            break;
                        case ApplicableParameterPercent aPercent when appearance[e] is ApplicableParameterPercent bPercent:
                            bPercent.Percentage = aPercent.Percentage;
                            break;
                        default:
                            throw new Exception("Unsupported Appearance Parameter Type");
                    }
                }
            }

            if (outfit.Equipment.Apply) {
                equipment.Apply = true;

                equipment.RevertToGame |= outfit.Equipment.RevertToGame;
                
                foreach (var s in Common.GetGearSlots()) {
                    var i = outfit.Equipment[s];
                    if (!i.Apply) continue;
                    equipment[s].Apply = true;

                    equipment[s].ModConfigs = [];
                    foreach (var modConfig in i.ModConfigs) {
                        equipment[s].ModConfigs.Add(modConfig with { });
                    }

                    equipment[s].Materials = [];
                    foreach (var material in i.Materials) {
                        equipment[s].Materials.Add(material with { });
                    }

                    if (i is ApplicableEquipment e1 && equipment[s] is ApplicableEquipment e2) {
                        e2.ItemId = e1.ItemId;
                        e2.Stain.Apply = e1.Stain.Apply;
                        e2.Stain.Stain = e1.Stain.Stain;
                        e2.Stain.Stain2 = e1.Stain.Stain2;
                    } else if (i is ApplicableBonus b1 && equipment[s] is ApplicableBonus b2) {
                        b2.BonusItemId = b1.BonusItemId;
                    }

                    equipment[s].CustomizePlusTemplateConfigs = i.CustomizePlusTemplateConfigs;
                }

                foreach (var t in Enum.GetValues<ToggleType>()) {
                    if (outfit.Equipment[t].Apply) {
                        equipment[t].Apply = true;
                        equipment[t].Toggle = outfit.Equipment[t].Toggle;
                    }
                }
            }
        }
        
        return (appearance, equipment, additionals);
    } 
    
}
