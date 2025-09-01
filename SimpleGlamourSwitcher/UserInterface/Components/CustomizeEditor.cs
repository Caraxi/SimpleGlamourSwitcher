using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ECommons.ImGuiMethods;
using Glamourer.GameData;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using Penumbra.GameData;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.Service;
using SimpleGlamourSwitcher.Utility;

using EnumRace = Penumbra.GameData.Enums.Race;
using Race = Lumina.Excel.Sheets.Race;

namespace SimpleGlamourSwitcher.UserInterface.Components;

public static class CustomizeEditor {
    private const float EditorInputScale = 0.7f;
    
    private static IEnumerable<(CustomizeIndex, string)> GetCustomizeTypes(byte gender, byte clan) {
        var race = GetRaceId(clan);
        var subRace = (SubRace) clan;
        
        yield return (CustomizeIndex.Clan, "Body Type");
        yield return (CustomizeIndex.Height, "Height");
        if (gender == 1) yield return (CustomizeIndex.BustSize, "Bust Size");

        if (race is EnumRace.Hyur or EnumRace.Roegadyn) yield return (CustomizeIndex.MuscleMass, "Muscle Mass");
        if (race is EnumRace.Elezen or EnumRace.Lalafell or EnumRace.Viera) yield return (CustomizeIndex.MuscleMass, "Ear Size");
        if (race is EnumRace.Miqote or EnumRace.AuRa or EnumRace.Hrothgar) yield return (CustomizeIndex.MuscleMass, "Tail Length");
        
        yield return (CustomizeIndex.Face, "Face");
        yield return (CustomizeIndex.FacialFeature1, "Facial Features");
        yield return (CustomizeIndex.Hairstyle, CustomizeIndex.Hairstyle.PrettyName());
        yield return (CustomizeIndex.SkinColor, CustomizeIndex.SkinColor.PrettyName());
        yield return (CustomizeIndex.EyeColorRight, CustomizeIndex.EyeColorRight.PrettyName());
        yield return (CustomizeIndex.EyeColorLeft, CustomizeIndex.EyeColorLeft.PrettyName());
        yield return (CustomizeIndex.FacePaintColor, CustomizeIndex.FacePaintColor.PrettyName());

        if (race is EnumRace.AuRa) {
            yield return (CustomizeIndex.TattooColor, "Limbal Ring Color");
        } else if (subRace is SubRace.KeeperOfTheMoon or SubRace.Wildwood) {
            yield return (CustomizeIndex.TattooColor, "Ear Clasp Color");
        } else {
            yield return (CustomizeIndex.TattooColor, CustomizeIndex.TattooColor.PrettyName());
        }
        
        yield return (CustomizeIndex.LipColor, CustomizeIndex.LipColor.PrettyName());
        yield return (CustomizeIndex.Eyebrows, CustomizeIndex.Eyebrows.PrettyName());
        yield return (CustomizeIndex.EyeShape, CustomizeIndex.EyeShape.PrettyName());
        yield return (CustomizeIndex.SmallIris, CustomizeIndex.SmallIris.PrettyName());
        yield return (CustomizeIndex.Nose, CustomizeIndex.Nose.PrettyName());
        yield return (CustomizeIndex.Jaw, CustomizeIndex.Jaw.PrettyName());
        yield return (CustomizeIndex.Mouth, CustomizeIndex.Mouth.PrettyName());
        yield return (CustomizeIndex.Lipstick, CustomizeIndex.Lipstick.PrettyName());
        if (race is EnumRace.Miqote or EnumRace.AuRa or EnumRace.Hrothgar) yield return (CustomizeIndex.TailShape, CustomizeIndex.TailShape.PrettyName());
        yield return (CustomizeIndex.FacePaint, CustomizeIndex.FacePaint.PrettyName());
        yield return (CustomizeIndex.FacePaintReversed, CustomizeIndex.FacePaintReversed.PrettyName());
    }
    
    
    public static bool Show(OutfitAppearance outfitAppearance) {
        var edited = false;

        foreach (var (v, label) in GetCustomizeTypes(outfitAppearance.Gender.Value, outfitAppearance.Clan.Value)) {
            edited |= v switch {
                CustomizeIndex.Clan => ShowGenderRaceClan(outfitAppearance),
                CustomizeIndex.Height => ShowSlider(label, outfitAppearance, outfitAppearance.Height, 0, 100),
                CustomizeIndex.BustSize => ShowSlider(label, outfitAppearance,  outfitAppearance.BustSize, 0, 100),
                CustomizeIndex.MuscleMass => ShowSlider(label, outfitAppearance,  outfitAppearance.MuscleMass, 0, 100),
                CustomizeIndex.Face => ShowFacePicker(label, outfitAppearance),
                CustomizeIndex.Hairstyle => ShowHairPicker(label, outfitAppearance),
                CustomizeIndex.FacialFeature1 => ShowFacialFeaturePicker(label, outfitAppearance, outfitAppearance.Gender.Value, outfitAppearance.Clan.Value, outfitAppearance.Face.Value),
                CustomizeIndex.SkinColor or CustomizeIndex.EyeColorRight or CustomizeIndex.EyeColorLeft or CustomizeIndex.LipColor or CustomizeIndex.FacePaintColor or CustomizeIndex.TattooColor => ShowColorPicker(label, outfitAppearance, v),
                
                _ => ShowCustomize(outfitAppearance, outfitAppearance[v], v, label)
            };
        }
        

        return edited;
    }

    private static bool ShowColorPicker(string label, OutfitAppearance appearance, CustomizeIndex idx) {
        var edited = false;
        edited |= ShowApplyEnableCheckbox(label, ref appearance[idx].Apply, ref appearance.Apply);
        using (ImRaii.PushId($"colorPicker_{label}")) {
            edited |= ColorPicker(appearance, idx, ImGui.GetItemRectSize() with { X =  ImGui.GetContentRegionAvail().X * EditorInputScale });
        }
       
        ImGuiExt.SameLineInner();
        ImGui.Text(label.RemoveImGuiId());
        return edited;
    }

    private static readonly CustomizeIndex[] FacialFeatures = [CustomizeIndex.FacialFeature1, CustomizeIndex.FacialFeature2, CustomizeIndex.FacialFeature3, CustomizeIndex.FacialFeature4, CustomizeIndex.FacialFeature5, CustomizeIndex.FacialFeature6, CustomizeIndex.FacialFeature7, CustomizeIndex.LegacyTattoo];
    
    private static bool ShowFacialFeaturePicker(string label, OutfitAppearance appearance, byte genderValue, byte clanValue, byte faceValue) {
        var edited = false;
        var charaMakeType = DataManager.GetExcelSheet<CharaMakeType>().FirstOrDefault(c => c.Gender == genderValue && c.Tribe.RowId == clanValue);
        var allEnabled = FacialFeatures.All(f => appearance[f].Apply);
        var allDisabled = FacialFeatures.All(f => !appearance[f].Apply);
        bool? cbState = allEnabled ? true : allDisabled ? false : null;
        if (ImGuiExt.CheckboxTriState($"##enableCustomize_facialFeaturesAny", ref cbState)) {
            foreach (var f in FacialFeatures) {
                appearance[f].Apply = cbState ?? false;
            }
            edited = true;
        }
        
        ImGui.SameLine();
        var faceDetails = charaMakeType.FacialFeatureOption[faceValue == 0 ? 0 : faceValue - 1];
        if (charaMakeType.Tribe.RowId != clanValue || charaMakeType.Gender != genderValue || faceValue == 0 || faceDetails.Option1 == 0) {
            edited |= ShowCustomize(appearance, appearance[CustomizeIndex.FacialFeature1], CustomizeIndex.FacialFeature1, label);
            edited |=  ShowCustomize(appearance, appearance[CustomizeIndex.FacialFeature2], CustomizeIndex.FacialFeature2, label);
            edited |=  ShowCustomize(appearance, appearance[CustomizeIndex.FacialFeature3], CustomizeIndex.FacialFeature3, label);
            edited |=  ShowCustomize(appearance, appearance[CustomizeIndex.FacialFeature4], CustomizeIndex.FacialFeature4, label);
            edited |=  ShowCustomize(appearance, appearance[CustomizeIndex.FacialFeature5], CustomizeIndex.FacialFeature5, label);
            edited |=  ShowCustomize(appearance, appearance[CustomizeIndex.FacialFeature6], CustomizeIndex.FacialFeature6, label);
            edited |=  ShowCustomize(appearance, appearance[CustomizeIndex.FacialFeature7], CustomizeIndex.FacialFeature7, label);
            edited |=  ShowCustomize(appearance, appearance[CustomizeIndex.LegacyTattoo], CustomizeIndex.LegacyTattoo, label);
            return edited;
        }

        var totalValue = 0;
        var w = ImGui.GetContentRegionAvail().X * EditorInputScale - 3;
        void ShowOption(CustomizeIndex index, int iconId, byte enableValue) {
            var aCustomize = appearance[index];
            using (ImRaii.Group()) {
                if (ImGui.BeginChildFrame(ImGui.GetID($"facialFeature_{index}"), new Vector2(w / 4f, 48) * ImGuiHelpers.GlobalScale, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)) {
                    edited |= ShowApplyEnableCheckbox(index.PrettyName(), ref aCustomize.Apply, ref appearance.Apply);
                    var icon = TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
                    if (ImGui.ImageButton(icon.Handle, new Vector2(ImGui.GetContentRegionAvail().Y) * ImGuiHelpers.GlobalScale, Vector2.Zero, Vector2.One, 1, ImGui.GetColorU32(ImGuiCol.Button).ToVector4(), aCustomize.Value != 0 ? Vector4.One : ImGuiColors.DalamudRed)) {
                        aCustomize.Value = aCustomize.Value != 0 ? (byte) 0 : enableValue;
                        edited = true;
                    }
                }
                ImGui.EndChildFrame();
            }

            if (aCustomize.Value > 0) totalValue += enableValue;
        }
        
        using (ImRaii.Group()) {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One)) {
                ShowOption(CustomizeIndex.FacialFeature1, faceDetails.Option1, 1);
                ImGui.SameLine();
                ShowOption(CustomizeIndex.FacialFeature2, faceDetails.Option2, 2);
                ImGui.SameLine();
                ShowOption(CustomizeIndex.FacialFeature3, faceDetails.Option3, 4);
                ImGui.SameLine();
                ShowOption(CustomizeIndex.FacialFeature4, faceDetails.Option4, 8);
                ShowOption(CustomizeIndex.FacialFeature5, faceDetails.Option5, 16);
                ImGui.SameLine();
                ShowOption(CustomizeIndex.FacialFeature6, faceDetails.Option6, 32);
                ImGui.SameLine();
                ShowOption(CustomizeIndex.FacialFeature7, faceDetails.Option7, 64);
                ImGui.SameLine();
                ShowOption(CustomizeIndex.LegacyTattoo, 91003, 128);
            }
        }

        ImGuiExt.SameLineInner();
        using (ImRaii.Group()) {
            ImGui.Text(label.RemoveImGuiId());
            ImGui.SetNextItemWidth(90 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt($"##inputInt_{label}", ref totalValue)) {
                if (totalValue < byte.MinValue) totalValue = byte.MaxValue;
                if (totalValue > byte.MaxValue) totalValue = byte.MinValue;
                byte FacialFlag(byte enableValue) => (byte) ((totalValue & enableValue) == enableValue ? enableValue : 0);
                appearance.FacialFeature1.Value = FacialFlag(1);
                appearance.FacialFeature2.Value = FacialFlag(2);
                appearance.FacialFeature3.Value = FacialFlag(4);
                appearance.FacialFeature4.Value = FacialFlag(8);
                appearance.FacialFeature5.Value = FacialFlag(16);
                appearance.FacialFeature6.Value = FacialFlag(32);
                appearance.FacialFeature7.Value = FacialFlag(64);
                appearance.LegacyTattoo.Value = FacialFlag(128);
                edited = true;
            }
        }
        
        return edited;
    }

    public static bool ShowApplyEnableCheckbox(string label, ref bool apply, ref bool parentEnabled) {
        var edited = false;
        using (ImRaii.PushColor(ImGuiCol.CheckMark, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.5f), !parentEnabled)) {
            edited |= ImGui.Checkbox($"##enableCustomize_{label}", ref apply);
        }
        
        if (ImGui.IsItemHovered()) {
            using (ImRaii.Tooltip()) {
                ImGui.Text($"Apply '");
                ImGuiExt.SameLineNoSpace();
                ImGui.TextColored(ImGuiColors.DalamudViolet, label.RemoveImGuiId());
                ImGuiExt.SameLineNoSpace();
                ImGui.Text("' with this outfit.");
                if (!parentEnabled) {
                    ImGui.TextDisabled("This option is will not be applied because the Appearance option is not enabled for this outfit.");
                }
            }
        }
        
        ImGui.SameLine();
        return edited;
    }
    
    private static bool ShowFacePicker(string label, OutfitAppearance appearance) {
        var edited = false;
        edited |= ShowApplyEnableCheckbox(label, ref appearance.Face.Apply, ref appearance.Apply);
        var charaMakeType = DataManager.GetExcelSheet<CharaMakeType>().FirstOrDefault(c => c.Gender == appearance.Gender.Value && c.Tribe.RowId == appearance.Clan.Value);
        if (charaMakeType.Tribe.RowId != appearance.Clan.Value || charaMakeType.Gender != appearance.Gender.Value || !Lumina.Extensions.LinqExtensions.TryGetFirst(charaMakeType.CharaMakeStruct, c => c.Customize == (byte) CustomizeIndex.Face, out var faceMakeType)) 
            return ShowCustomize(appearance, appearance.Face, CustomizeIndex.Face, label);

        using (ImRaii.Group()) {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * EditorInputScale);
            
            if (ImGui.BeginChildFrame(ImGui.GetID("facePickerPreview"), new Vector2(ImGui.GetContentRegionAvail().X * EditorInputScale, 48 * ImGuiHelpers.GlobalScale))) {
                var icon = TextureProvider.GetFromGameIcon(faceMakeType.SubMenuParam[appearance.Face.Value - 1]).GetWrapOrEmpty();
                ImGui.Image(icon.Handle, new Vector2(ImGui.GetContentRegionAvail().Y));
                ImGui.SameLine();
                ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y / 2 - ImGui.GetTextLineHeight() / 2);
                ImGui.Text($"Face #{appearance.Face.Value}");
            }
            ImGui.EndChildFrame();
            if (ImGui.IsItemClicked()) {
                ImGui.OpenPopup("facePicker");
            }
            
            ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() * Vector2.UnitY);
            var w = ImGui.GetItemRectSize().X;
            if (ImGui.BeginPopup("facePicker", ImGuiWindowFlags.AlwaysAutoResize)) {

                for (var i = 0; i < faceMakeType.SubMenuParam.Count; i++) {
                    var v = faceMakeType.SubMenuParam[i];
                    if (v == 0) continue;
                    var icon = TextureProvider.GetFromGameIcon(v).GetWrapOrEmpty();

                    var buttonHeight = 48 * ImGuiHelpers.GlobalScale;

                    using (ImRaii.PushStyle(ImGuiStyleVar.ButtonTextAlign, new Vector2((buttonHeight * 1.125f) / w, 0.5f))) {
                        using (ImRaii.PushColor(ImGuiCol.Button, ImGui.GetColorU32(ImGuiCol.TextDisabled), appearance.Face.Value == i + 1)) {
                            if (ImGui.Button($"Face #{i + 1}", new Vector2(w, buttonHeight))) {
                                edited = true;
                                appearance.Face.Value = (byte)(i + 1);
                                ImGui.CloseCurrentPopup();
                            }
                        }
                    }

                    ImGui.GetWindowDrawList().AddImage(icon.Handle, ImGui.GetItemRectMin(), ImGui.GetItemRectMin() + new Vector2(ImGui.GetItemRectSize().Y));
                }

                ImGui.EndCombo();

            }
        }

        ImGuiExt.SameLineInner();
        ImGui.Text(label.RemoveImGuiId());

        return edited;
    }
    
    private static bool ColorPicker(OutfitAppearance appearance, CustomizeIndex idx, Vector2? size = null) {
        var edited = false;
        var set = PluginService.CustomizeManager.GetSet((SubRace)appearance.Clan.Value, appearance.Gender.Value == 1 ? Gender.Female : Gender.Male);
        var colorSet = idx switch {
            CustomizeIndex.HairColor => set.HairColors,
            CustomizeIndex.HighlightsColor => set.HighlightColors,
            CustomizeIndex.SkinColor => set.SkinColors,
            CustomizeIndex.EyeColorLeft => set.EyeColors,
            CustomizeIndex.EyeColorRight => set.EyeColors,
            CustomizeIndex.LipColor => Common.ConcatList(set.LipColorsDark, set.LipColorsLight),
            CustomizeIndex.FacePaintColor => Common.ConcatList(set.FacePaintColorsLight, set.FacePaintColorsLight),
            CustomizeIndex.TattooColor => set.TattooColors,
            _ => throw new ArgumentOutOfRangeException(nameof(idx), idx, null)
        };
        
        

        var selected = colorSet.FirstOrDefault(c => c.Value.Value == appearance[idx].Value, colorSet[0]);
        
        if (size != null) {
            if (ImGui.ColorButton($"{idx}#{selected.Value.Value}", ImGui.ColorConvertU32ToFloat4(selected.Color), ImGuiColorEditFlags.None, size.Value)) {
                ImGui.OpenPopup($"ColorPicker_{idx}");
            }

        } else {
            if (ImGui.ColorButton($"{idx}#{selected.Value.Value}", ImGui.ColorConvertU32ToFloat4(selected.Color))) {
                ImGui.OpenPopup($"ColorPicker_{idx}");
            }

        }
  
        ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() * Vector2.UnitY);

        if (ImGui.BeginPopup($"ColorPicker_{idx}")) {
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameBorderSize, 3))
            using (ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0))
            using (ImRaii.PushColor(ImGuiCol.Border, 0)) {
                var i = 0;
                foreach (var c in colorSet) {
                    if (i++ % 8 != 0) ImGui.SameLine();
                    using (ImRaii.PushColor(ImGuiCol.Border, Vector4.One, appearance[idx].Value == c.Value.Value)) {
                        if (ImGui.ColorButton($"{idx}#{c.Value.Value}", ImGui.ColorConvertU32ToFloat4(c.Color))) {
                            appearance[idx].Value = c.Value.Value;
                            edited = true;
                            ImGui.CloseCurrentPopup();
                        }
                    }
                }
            }
                
            ImGui.EndPopup();
        }

        return edited;
    }
    
    private static bool ShowHairPicker(string label, OutfitAppearance appearance) {
        var edited = false;
        edited |= ShowApplyEnableCheckbox(label, ref appearance.Hairstyle.Apply, ref appearance.Apply);
        var cbSize = ImGui.GetItemRectSize();
        
        var hairstyles = DataCache.GetHairstyles(appearance.Clan.Value, appearance.Gender.Value);
        
        var activeHairstyle = hairstyles.FirstOrNull(c => c.FeatureID == appearance.Hairstyle.Value);

        using var _g = ImRaii.Group();
        if (ImGui.BeginChildFrame(ImGui.GetID("hairPickerPreview"), new Vector2((ImGui.GetContentRegionAvail().X * EditorInputScale) - (cbSize.X * 3 + ImGui.GetStyle().ItemInnerSpacing.X * 3), 48 * ImGuiHelpers.GlobalScale))) {
            if (activeHairstyle.HasValue) {
                var icon = TextureProvider.GetFromGameIcon(activeHairstyle.Value.Icon).GetWrapOrEmpty();
                ImGui.Image(icon.Handle, new Vector2(ImGui.GetContentRegionAvail().Y));
            } else {
                ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().Y));
            }
           
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y / 2 - ImGui.GetTextLineHeight() / 2);
            var name = $"Hairstyle#{appearance.Hairstyle.Value}";
            if (activeHairstyle?.HintItem.RowId > 0) {
                name += $" ({activeHairstyle.Value.HintItem.Value.Name.ExtractText().Split('-', 2, StringSplitOptions.TrimEntries)[1]})";
            }
            ImGui.Text(name);
        }
        ImGui.EndChildFrame();
        if (ImGui.IsItemClicked()) {
            ImGui.OpenPopup("hairPicker");
        }
        
        ImGui.SetNextWindowPos(ImGui.GetItemRectMin() + ImGui.GetItemRectSize() * Vector2.UnitY);
        ImGui.SetNextWindowSize(new Vector2(ImGui.GetItemRectSize().X, 500 * ImGuiHelpers.GlobalScale));
        
        if (ImGui.BeginPopup("hairPicker")) {
            var dl = ImGui.GetWindowDrawList();
            
            foreach (var cmc in hairstyles) {
                
                IDalamudTextureWrap icon;
                try {
                    icon = TextureProvider.GetFromGameIcon(cmc.Icon).GetWrapOrEmpty();
                } catch {
                    continue;
                }

                var active = cmc.FeatureID == appearance.Hairstyle.Value;

                if (ImGui.IsWindowAppearing() && active) {
                    ImGui.SetScrollHereY();
                }
                
                if (ImGui.BeginChildFrame(ImGui.GetID($"hairstyle_{cmc.FeatureID}"), new Vector2(ImGui.GetContentRegionAvail().X, 48 * ImGuiHelpers.GlobalScale), ImGuiWindowFlags.NoBackground)) {
                    try {
                        ImGui.Image(icon.Handle, new Vector2(ImGui.GetContentRegionAvail().Y));
                    } catch {
                        ImGui.Dummy(new Vector2(ImGui.GetContentRegionAvail().Y));
                    }
                    
                    ImGui.SameLine();
                    var name = $"Hairstyle#{cmc.FeatureID}";
                    if (cmc.HintItem.RowId != 0) {
                        name += $"\n{cmc.HintItem.Value.Name.ExtractText().Split('-', 2, StringSplitOptions.TrimEntries)[1]}";
                    }
                    

                    ImGui.SetCursorPosY(ImGui.GetContentRegionAvail().Y / 2 - ImGui.CalcTextSize(name).Y / 2);
                    ImGui.Text(name);
                    
                }
                ImGui.EndChildFrame();
                
                
                dl.AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), ImGui.GetColorU32(ImGui.IsItemHovered() ? ImGuiCol.FrameBgHovered : active ? ImGuiCol.FrameBgActive : ImGuiCol.FrameBg), ImGui.GetStyle().FrameRounding);
                
                if (ImGui.IsItemClicked()) {
                    edited = true;
                    appearance.Hairstyle.Value = cmc.FeatureID;
                    ImGui.CloseCurrentPopup();
                }
            }
            
            ImGui.EndCombo();
        }
        
        ImGuiExt.SameLineInner();

        using (ImRaii.Group()) {
            edited |= ShowApplyEnableCheckbox(CustomizeIndex.HairColor.PrettyName(), ref appearance.HairColor.Apply, ref appearance.Apply);
            ImGuiExt.SameLineInner();
            edited |= ShowApplyEnableCheckbox(CustomizeIndex.Highlights.PrettyName(), ref appearance.Highlights.Apply, ref appearance.Apply);
            ImGuiExt.SameLineInner();
            edited |= ShowApplyEnableCheckbox(CustomizeIndex.HighlightsColor.PrettyName(), ref appearance.HighlightsColor.Apply, ref appearance.Apply);
            
            ImGui.NewLine();
            edited |= ColorPicker(appearance, CustomizeIndex.HairColor);
            ImGuiExt.SameLineInner();

            var highlightsEnable = appearance.Highlights.Value > 0;
            if (ImGui.Checkbox("##enableHighlights", ref highlightsEnable)) {
                appearance.Highlights.Value = (byte) (highlightsEnable ? 128 : 0);
                edited = true;
            }

            if (ImGui.IsItemHovered()) {
                ImGui.SetTooltip("Enable Highlights");
            }
            
            ImGuiExt.SameLineInner();
            edited |= ColorPicker(appearance, CustomizeIndex.HighlightsColor);
        }
       

        ImGuiExt.SameLineInner();
        using (ImRaii.Group()) {
            ImGui.Text(label.RemoveImGuiId());
            ImGui.Text("& Colours");
        }

        edited |= ModListDisplay.Show(appearance.Hairstyle, "Hairstyle");
        
        return edited;
    }

    private static bool ShowSlider(string label, OutfitAppearance appearance, ApplicableCustomize customize, byte min, byte max) {
        var edited = false;
        
        edited |= ShowApplyEnableCheckbox(label, ref customize.Apply, ref appearance.Apply);

        var v = (int)customize.Value;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * EditorInputScale);
        if (ImGui.SliderInt($"{label}##customizeEditor", ref v, min, max)) {
            customize.Value = Math.Clamp((byte)v, min, max);
            edited = true;
        }

        return edited;
    }

    private static string GetGenderRaceClanName(byte gender, Tribe tribe, bool pad = false) {
        var raceId = GetRaceId((byte)tribe.RowId);
        if (!DataManager.GetExcelSheet<Race>().TryGetRow((uint)raceId, out var race)) return $"Unknown ({gender}, {tribe.RowId})";
        var genderName = (gender == 0 ? "Male" : "Female").PadLeft(pad ? 6 : 0);
        var tribeName = gender == 0 ? tribe.Masculine : tribe.Feminine;
        var raceName = gender == 0 ? race.Masculine : race.Feminine;
        return $"{genderName} {tribeName.ExtractText()} {raceName.ExtractText()}";
    }

    private static string GetGenderRaceClanName(byte gender, byte clanId, bool pad = false) {
        if (!DataManager.GetExcelSheet<Tribe>().TryGetRow(clanId, out var clan)) return $"Unknown ({gender}, {clanId})";
        return GetGenderRaceClanName(gender, clan, pad);
    }

    private static EnumRace GetRaceId(uint tribeId) {
        return tribeId switch {
            1 or 2 => EnumRace.Hyur,
            3 or 4 => EnumRace.Elezen,
            5 or 6 => EnumRace.Lalafell,
            7 or 8 => EnumRace.Miqote,
            9 or 10 => EnumRace.Roegadyn,
            11 or 12 => EnumRace.AuRa,
            13 or 14 => EnumRace.Hrothgar,
            15 or 16 => EnumRace.Viera,
            _ => EnumRace.Hyur,
        };
    }

    private static bool ShowGenderRaceClan(OutfitAppearance appearance) {
        var edited = false;
        var gender = appearance.Gender.Value;
        var clan = appearance.Clan.Value;
        var enabled = appearance.Clan.Apply;
        using (ImRaii.Group()) {

            if (ShowApplyEnableCheckbox("Body Type", ref enabled, ref appearance.Apply)) {
                appearance.Clan.Apply = enabled;
                appearance.Race.Apply = enabled;
                appearance.Gender.Apply = enabled;
                edited = true;
            }
            
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * EditorInputScale);
            if (ImGui.BeginCombo("Body Type", GetGenderRaceClanName(gender, clan))) {

                foreach (var tribe in DataManager.GetExcelSheet<Tribe>()) {
                    if (tribe.RowId == 0) continue;
                    using (PluginService.UiBuilder.MonoFontHandle.Push()) {
                        if (ImGui.Selectable(GetGenderRaceClanName(0, tribe, true), gender == 0 && clan == tribe.RowId)) {
                            appearance.Clan.Value = (byte) tribe.RowId;
                            appearance.Race.Value = (byte) GetRaceId(tribe.RowId);
                            appearance.Gender.Value = 0;
                            edited = true;
                        }

                        if (ImGui.Selectable(GetGenderRaceClanName(1, tribe, true), gender == 1 && clan == tribe.RowId)) {
                            appearance.Clan.Value = (byte) tribe.RowId;
                            appearance.Race.Value = (byte) GetRaceId(tribe.RowId);
                            appearance.Gender.Value = 1;
                            edited = true;
                        }
                    }

                }

                ImGui.EndCombo();
            }
        }

        return edited;
    }

    private static bool ShowCustomize(OutfitAppearance appearance, ApplicableCustomize customize, CustomizeIndex customizeIndex, string label) {
        var edited = false;
        edited |= ShowApplyEnableCheckbox(label, ref customize.Apply, ref appearance.Apply);
        ShowReadOnly($"{label}##customizeEditor_{customizeIndex}", customizeIndex, customize);
        return edited;
    }

    public static void ShowReadOnly(string label, CustomizeIndex index, ApplicableCustomize applicableCustomize) {
        ShowEditor(label, index, applicableCustomize, true);
    }

    private static bool ShowEditorEntry(string label, CustomizeIndex index, ApplicableCustomize customize, bool isReadOnly = false) {
        switch (index) {
            case CustomizeIndex.Race: {
                var selectedRace = DataManager.GetExcelSheet<Race>().GetRow(customize.Value);
                if (isReadOnly) {
                    var v = selectedRace.Feminine.ExtractText();
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * EditorInputScale);
                    ImGui.InputText(label, ref v, 32, ImGuiInputTextFlags.ReadOnly);
                    return false;
                }

                goto default;
            }

            case CustomizeIndex.Gender:
                if (isReadOnly) {
                    var v = customize.Value == 0 ? "Male" : "Female";
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * EditorInputScale);
                    ImGui.InputText(label, ref v, 32, ImGuiInputTextFlags.ReadOnly);
                    return false;
                }
                
                goto default;
                
            case CustomizeIndex.BodyType:
            case CustomizeIndex.Height:
            case CustomizeIndex.Clan:
            case CustomizeIndex.Face:
            case CustomizeIndex.Hairstyle:
            case CustomizeIndex.Highlights:
            case CustomizeIndex.SkinColor:
            case CustomizeIndex.EyeColorRight:
            case CustomizeIndex.HairColor:
            case CustomizeIndex.HighlightsColor:
            case CustomizeIndex.FacialFeature1:
            case CustomizeIndex.FacialFeature2:
            case CustomizeIndex.FacialFeature3:
            case CustomizeIndex.FacialFeature4:
            case CustomizeIndex.FacialFeature5:
            case CustomizeIndex.FacialFeature6:
            case CustomizeIndex.FacialFeature7:
            case CustomizeIndex.LegacyTattoo:
            case CustomizeIndex.TattooColor:
            case CustomizeIndex.Eyebrows:
            case CustomizeIndex.EyeColorLeft:
            case CustomizeIndex.EyeShape:
            case CustomizeIndex.SmallIris:
            case CustomizeIndex.Nose:
            case CustomizeIndex.Jaw:
            case CustomizeIndex.Mouth:
            case CustomizeIndex.Lipstick:
            case CustomizeIndex.LipColor:
            case CustomizeIndex.MuscleMass:
            case CustomizeIndex.TailShape:
            case CustomizeIndex.BustSize:
            case CustomizeIndex.FacePaint:
            case CustomizeIndex.FacePaintReversed:
            case CustomizeIndex.FacePaintColor:
            default:
                if (isReadOnly) {
                    var v = $"{customize.Value}";
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * EditorInputScale);
                    ImGui.InputText(label, ref v, 4, ImGuiInputTextFlags.ReadOnly);
                    ImGui.SameLine(-48 * ImGuiHelpers.GlobalScale);
                    using (PluginService.UiBuilder.IconFontHandle.Push()) 
                    using (ImRaii.PushColor(ImGuiCol.TextDisabled, ImGui.GetColorU32(ImGuiCol.TextDisabled, 0.25f))) {
                        ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
                    }

                    if (ImGui.IsItemHovered()) {
                        ImGui.SetTooltip($"'{label.Split("##")[0]}' cannot currently be modified within Simple Glamour Switcher");
                    }
                } else {
                    var v = (int)customize.Value;
                    ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * EditorInputScale);
                    if (ImGui.InputInt(label, ref v)) {
                        customize.Value = (byte)v;
                        return true;
                    }
                }
                
                return false;
        }
    }

    public static bool ShowEditor(string label, CustomizeIndex index, ApplicableCustomize customize, bool isReadOnly = false) {
        using var group = ImRaii.Group();
        var r = ShowEditorEntry(label, index, customize, isReadOnly);
        if (customize is ApplicableCustomizeModable modable) {
            ModListDisplay.Show(modable, $"{index.PrettyName()}");
        }
        
        return r;
    }
}