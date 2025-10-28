using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace SimpleGlamourSwitcher.Service;

public record EmoteIdentifier(uint EmoteModeId, uint EmoteId, byte CPoseState) {
    
    public virtual bool Equals(EmoteIdentifier? other) => other != null && EmoteModeId == other.EmoteModeId && CPoseState == other.CPoseState && other.EmoteId == EmoteId;
    public override int GetHashCode() => HashCode.Combine(EmoteModeId, EmoteId, CPoseState);

    public override string ToString() {
        return EmoteModeId == 0 ? EmoteId == 0 ? $"IdleEmote#{CPoseState}" : $"Emote#{EmoteId}" : $"EmoteModeId#{EmoteModeId}-{CPoseState}";
    }

    public static Lazy<HashSet<EmoteIdentifier>> EmoteList = new(() => {
        var l = new HashSet<EmoteIdentifier>();
        for (byte i = 0; i < 7; i++) {
            l.Add(new EmoteIdentifier(0, 0, i));
        }
        
        foreach (var emoteMode in DataManager.GetExcelSheet<EmoteMode>()!) {
            if (emoteMode.RowId == 0) continue;
            if (emoteMode.StartEmote.RowId == 0) continue;
            // Looping Emotes
            for (byte i = 0; i < emoteMode.RowId switch { 1 => 4, 2 => 5, 3 => 3, _ => 1 }; i++) 
                l.Add(new EmoteIdentifier(emoteMode.RowId, 0, i));
        
        }
        
        foreach (var e in DataManager.GetExcelSheet<Emote>()) {
            if (e.EmoteMode.RowId != 0) continue;
            if (e.EmoteCategory.RowId == 3) continue;
            if (e.Icon == 0) continue;
            var name = e.Name.ExtractText();
            if (string.IsNullOrWhiteSpace(name)) continue;
            l.Add(new EmoteIdentifier(0, e.RowId, 0));
        }

        return l;
    });

    public static IReadOnlySet<EmoteIdentifier> List => EmoteList.Value;

    private static readonly Dictionary<EmoteIdentifier, string> Names = new();

    private static readonly Dictionary<uint, EmoteIdentifier> EmoteAlias = new() {
        [147] = new EmoteIdentifier(0, 146, 0), // Dote -> Dote
        [0] = new EmoteIdentifier(0, 0, 0), // Idle 1
        [91] = new EmoteIdentifier(0, 0, 1), // Idle 2,
        [92] = new EmoteIdentifier(0, 0, 2), // Idle 3,
        [107] = new EmoteIdentifier(0, 0, 3), // Idle 4,
        [108] = new EmoteIdentifier(0, 0, 4), // Idle 5,
        [218] = new EmoteIdentifier(0, 0, 5), // Idle 6,
        [219] = new EmoteIdentifier(0, 0, 6), // Idle 7,
    };

    private static readonly Dictionary<EmoteIdentifier, uint> Icons = new() {
        [new EmoteIdentifier(3, 0, 0)] = 64013, // Sleep -> Doze
        [new EmoteIdentifier(3, 0, 1)] = 64013,
        [new EmoteIdentifier(3, 0, 2)] = 64013,
    };

    private static string FetchModeName(uint emoteModeId) {
        var emoteMode = DataManager.GetExcelSheet<EmoteMode>()?.GetRow(emoteModeId);
        if (emoteMode == null) return $"EmoteMode#{emoteModeId}";
        var emote = emoteMode.Value.StartEmote;
        if (!emote.IsValid || emote.RowId == 0) return $"EmoteMode#{emoteModeId}";
        return emote.Value.Name.ExtractText();
    }
    
    private static string FetchEmoteName(uint emoteId) {
        var emoteMode = DataManager.GetExcelSheet<Emote>()?.GetRow(emoteId);
        if (emoteMode == null) return $"Emote#{emoteId}";
        return emoteMode.Value.Name.ExtractText();
    }

    private static uint FetchModeIcon(uint emoteModeId) {
        var emoteMode = DataManager.GetExcelSheet<EmoteMode>()?.GetRow(emoteModeId);
        if (emoteMode == null) return 0;
        var emote = emoteMode.Value.StartEmote;
        if (!emote.IsValid || emote.RowId == 0) return 0;

        return emote.Value.Icon;
    }
    
    private static uint FetchEmoteIcon(uint emoteId) {
        var emote = DataManager.GetExcelSheet<Emote>()?.GetRow(emoteId);
        if (emote == null) return 0;
        return emote.Value.Icon;
    }

    [JsonIgnore]
    public string EmoteName {
        get {
            if (Names.TryGetValue(this, out var name)) return name;
            
            if (EmoteId == 0 && EmoteModeId == 0) {
                name = "Idle";
            } else if (EmoteModeId == 0) {
                name = FetchEmoteName(EmoteId);
            } else {
                name = FetchModeName(EmoteModeId);
            }
            
            Names.TryAdd(this, name);
            return name;
        }
    }

    [JsonIgnore] public string Name => EmoteModeId is 1 or 2 or 3 || (EmoteModeId == 0 && EmoteId == 0) ? $"{EmoteName} Pose {CPoseState + 1}" : EmoteName;

    [JsonIgnore]
    public uint Icon {
        get {
            if (Icons.TryGetValue(this, out var icon)) return icon;

            if (EmoteId == 0 && EmoteModeId == 0) {
                icon = 64453;
            } else if (EmoteModeId == 0) {
                icon = FetchEmoteIcon(EmoteId);
            } else {
                icon = FetchModeIcon(EmoteModeId);
            }
            
            Icons.TryAdd(this, icon);
            return icon;
        }
    }

    public static unsafe EmoteIdentifier? Get(IPlayerCharacter? playerCharacter) => Get((Character*) (playerCharacter?.Address ?? 0));
    
    private static unsafe EmoteIdentifier? Get(Character* character) {
        if (character == null) return null;
        if (character->Mode is CharacterModes.InPositionLoop or CharacterModes.EmoteLoop) {
            return new EmoteIdentifier(character->ModeParam, 0, character->EmoteController.CPoseState);
        }

        if (character->Mode == CharacterModes.Normal) {
            var id = new EmoteIdentifier(0, character->EmoteController.EmoteId, 0);
            if (EmoteAlias.TryGetValue(id.EmoteId, out var aliasId)) {
                id = aliasId;
            }
            
            if (List.Contains(id)) return id;
        }
        
        return null;
    }

    public static async Task<EmoteIdentifier?> GetLocalPlayer() {
        EmoteIdentifier? emoteIdentifier = null;

        await Framework.RunOnFrameworkThread(() => {
            emoteIdentifier = Get(ClientState.LocalPlayer);
        });

        return emoteIdentifier;
    }
}
