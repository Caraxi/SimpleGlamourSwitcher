using System.Globalization;
using System.Runtime.CompilerServices;
using Penumbra.GameData.Enums;
using Penumbra.GameData.Files.MaterialStructs;
using Penumbra.GameData.Interop;

namespace SimpleGlamourSwitcher.IPC.Glamourer;

// Adjusted from Glamourer
//      https://github.com/Ottermandias/Glamourer/blob/1cc7c2f0cd6a4bc738044a63227c9c6e7ff19848/Glamourer/Interop/Material/MaterialValueIndex.cs

public readonly record struct MaterialValueIndex(MaterialValueIndex.DrawObjectType DrawObject, byte SlotIndex, byte MaterialIndex, byte RowIndex) {
    public const int MaterialsPerModel = 10;

    public static readonly MaterialValueIndex Invalid = new(DrawObjectType.Invalid, 0, 0, 0);

    public uint Key => ToKey(DrawObject, SlotIndex, MaterialIndex, RowIndex);

    public bool Valid => Validate(DrawObject) && ValidateSlot(DrawObject, SlotIndex) && ValidateMaterial(MaterialIndex) && ValidateRow(RowIndex);

    public static bool FromKey(uint key, out MaterialValueIndex index) {
        index = new MaterialValueIndex(key);
        return index.Valid;
    }

    public static MaterialValueIndex FromSlot(EquipSlot slot) {
        if (slot is EquipSlot.MainHand)
            return new MaterialValueIndex(DrawObjectType.Mainhand, 0, 0, 0);
        if (slot is EquipSlot.OffHand)
            return new MaterialValueIndex(DrawObjectType.Offhand, 0, 0, 0);

        var idx = slot.ToIndex();
        if (idx < 10)
            return new MaterialValueIndex(DrawObjectType.Human, (byte)idx, 0, 0);

        return Invalid;
    }

    public static MaterialValueIndex FromSlot(BonusItemFlag slot) {
        var idx = slot.ToIndex();
        return idx > 2 ? Invalid : new MaterialValueIndex(DrawObjectType.Human, (byte)(idx + 16), 0, 0);
    }

    public EquipSlot ToEquipSlot() {
        return DrawObject switch {
            DrawObjectType.Human when SlotIndex < 10 => ((uint)SlotIndex).ToEquipSlot(),
            DrawObjectType.Mainhand when SlotIndex == 0 => EquipSlot.MainHand,
            DrawObjectType.Offhand when SlotIndex == 0 => EquipSlot.OffHand,
            _ => EquipSlot.Unknown,
        };
    }

    public HumanSlot ToHumanSlot() {
        if (DrawObject is DrawObjectType.Human && SlotIndex == 16) return HumanSlot.Face;
        return ToEquipSlot().ToHumanSlot();
    }

    public unsafe bool TryGetModel(Actor actor, out Model model) {
        if (!actor.Valid) {
            model = Model.Null;
            return false;
        }

        model = DrawObject switch {
            DrawObjectType.Human => actor.Model,
            DrawObjectType.Mainhand => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponData[0].DrawObject : Model.Null,
            DrawObjectType.Offhand => actor.IsCharacter ? actor.AsCharacter->DrawData.WeaponData[1].DrawObject : Model.Null,
            _ => Model.Null,
        };
        return model.IsCharacterBase;
    }

    public static MaterialValueIndex FromKey(uint key) => new(key);

    public static MaterialValueIndex Min(DrawObjectType drawObject = 0, byte slotIndex = 0, byte materialIndex = 0, byte rowIndex = 0) => new(drawObject, slotIndex, materialIndex, rowIndex);

    public static MaterialValueIndex Max(DrawObjectType drawObject = (DrawObjectType)byte.MaxValue, byte slotIndex = byte.MaxValue, byte materialIndex = byte.MaxValue, byte rowIndex = byte.MaxValue) => new(drawObject, slotIndex, materialIndex, rowIndex);

    public enum DrawObjectType : byte {
        Invalid,
        Human,
        Mainhand,
        Offhand,
    };

    public static bool Validate(DrawObjectType type) => type is not DrawObjectType.Invalid && Enum.IsDefined(type);

    public static bool ValidateSlot(DrawObjectType type, byte slotIndex) {
        return type switch {
            DrawObjectType.Human => slotIndex < 18,
            DrawObjectType.Mainhand => slotIndex == 0,
            DrawObjectType.Offhand => slotIndex == 0,
            _ => false,
        };
    }

    public static bool ValidateMaterial(byte materialIndex) => materialIndex < MaterialsPerModel;

    public static bool ValidateRow(byte rowIndex) => rowIndex < ColorTable.NumRows;

    private static uint ToKey(DrawObjectType type, byte slotIndex, byte materialIndex, byte rowIndex) {
        var result = (uint)rowIndex;
        result |= (uint)materialIndex << 8;
        result |= (uint)slotIndex << 16;
        result |= (uint)((byte)type << 24);
        return result;
    }

    private MaterialValueIndex(uint key) : this((DrawObjectType)(key >> 24), (byte)(key >> 16), (byte)(key >> 8), (byte)key) { }

    public override string ToString() {
        return DrawObject switch {
            DrawObjectType.Invalid => "Invalid",
            DrawObjectType.Human when SlotIndex < 10 => $"{((uint)SlotIndex).ToEquipSlot().ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 10 => $"{BodySlot.Hair} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 11 => $"{BodySlot.Face} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 12 => $"{BodySlot.Tail} / {BodySlot.Ear} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 13 => $"Connectors {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 16 => $"{BonusItemFlag.Glasses.ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Human when SlotIndex == 17 => $"{BonusItemFlag.UnkSlot.ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Mainhand when SlotIndex == 0 => $"{EquipSlot.MainHand.ToName()} {MaterialString()} {RowString()}",
            DrawObjectType.Offhand when SlotIndex == 0 => $"{EquipSlot.OffHand.ToName()} {MaterialString()} {RowString()}",
            _ => $"{DrawObject} Slot {SlotIndex} {MaterialString()} {RowString()}",
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string MaterialString() => $"Material {(char)(MaterialIndex + 'A')}";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string RowString() => $"Row {RowIndex / 2 + 1}{(char)(RowIndex % 2 + 'A')}";

    public static implicit operator MaterialValueIndex(string keyString) => FromKey(uint.Parse(keyString, NumberStyles.HexNumber));
}
