using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using ImGuiNET;
using Lumina.Excel.Sheets;
using Penumbra.GameData.Enums;
using SimpleGlamourSwitcher.Configuration.ConfigSystem;
using SimpleGlamourSwitcher.Configuration.Enum;
using SimpleGlamourSwitcher.Configuration.Files;
using SimpleGlamourSwitcher.Configuration.Parts;
using SimpleGlamourSwitcher.Configuration.Parts.ApplicableParts;
using SimpleGlamourSwitcher.IPC.Glamourer;
using SimpleGlamourSwitcher.UserInterface.Components;
using SimpleGlamourSwitcher.UserInterface.Enums;
using SimpleGlamourSwitcher.Utility;
using UiBuilder = Dalamud.Interface.UiBuilder;

namespace SimpleGlamourSwitcher.UserInterface.Page;

public class EditOutfitPage(CharacterConfigFile character, Guid folderGuid, OutfitConfigFile? outfit) : Page {
    public bool IsNewOutfit { get; } = outfit == null;
    public OutfitConfigFile Outfit { get; } = outfit ?? OutfitConfigFile.CreateFromLocalPlayer(character, folderGuid, character.GetOptionsProvider(folderGuid));

    private readonly string folderPath = character.ParseFolderPath(folderGuid);
    private const float SubWindowWidth = 600f;
    
    private readonly FileDialogManager fileDialogManager = new();
    
    public override void DrawTop(ref WindowControlFlags controlFlags) {
        base.DrawTop(ref controlFlags);
        ImGuiExt.CenterText(IsNewOutfit ? "Creating" : "Editing Outfit", shadowed: true);
        ImGuiExt.CenterText(IsNewOutfit ? $"New Outfit in {folderPath}" : $"{folderPath} / {Outfit.Name}", shadowed: true);
    }

    private string outfitName = outfit?.Name ?? string.Empty;

    public override void DrawLeft(ref WindowControlFlags controlFlags) {
        if (ImGuiExt.ButtonWithIcon("Back", FontAwesomeIcon.CaretLeft, new Vector2(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeightWithSpacing() * 2))) {
            MainWindow?.PopPage();
        }
    }

    public override void DrawCenter(ref WindowControlFlags controlFlags) {
        fileDialogManager.Draw();
        controlFlags |= WindowControlFlags.PreventClose;
        ImGui.Spacing();
        
        var pad = (ImGui.GetContentRegionAvail().X - SubWindowWidth * ImGuiHelpers.GlobalScale) / 2f;
        using (ImRaii.Group()) {
            ImGui.Dummy(new Vector2(pad, 1f));
        }
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(SubWindowWidth * ImGuiHelpers.GlobalScale)) {
            CustomInput.InputText("Outfit Name", ref outfitName, 100, errorMessage: outfitName.Length == 0 ? "Please enter a name" : string.Empty);
            CustomInput.ReadOnlyInputText("Path", folderPath);
        }
        
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));


        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        if (ImGui.BeginChild("equipment", new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetContentRegionAvail().Y - ImGui.GetTextLineHeightWithSpacing() * 3), false)) {

            ImGui.Checkbox("##applyAppearance", ref Outfit.Appearance.Apply);
            ImGui.SameLine();
            using (ImRaii.Group()) {
                if (ImGui.CollapsingHeader("Appearance")) {
                    using (ImRaii.PushIndent()) {
                        DrawAppearance();
                    }
                }
            
                if (ImGui.CollapsingHeader("Advanced Appearance")) {
                    using (ImRaii.PushIndent()) {
                        DrawParameters();
                    }
                }
            }
           
            
            ImGui.Checkbox("##applyEquipment", ref Outfit.Equipment.Apply);
            ImGui.SameLine();
            if (ImGui.CollapsingHeader("Equipment")) {
                using (ImRaii.PushIndent()) {
                    DrawEquipment();
                }
            }
            
            

            if (ImGui.CollapsingHeader("Image")) {
                var folder = character.Folders.GetValueOrDefault(folderGuid);
                var outfitStyle = folder?.OutfitPolaroidStyle ?? (character.CustomStyle ?? PluginConfig.CustomStyle ?? Style.Default).OutfitList.Polaroid;
                ImageEditor.Draw(Outfit, outfitStyle, Outfit.Name, ref controlFlags);
            }
            

            if (ImGui.CollapsingHeader("Details")) {
                ImGui.Text($"GUID: {Outfit.Guid}");
            }
            
        }
        ImGui.EndChild();
        ImGui.GetWindowDrawList().AddRect(ImGui.GetItemRectMin() - ImGui.GetStyle().FramePadding, ImGui.GetItemRectMax() + ImGui.GetStyle().FramePadding, ImGui.GetColorU32(ImGuiCol.Separator));

        
        ImGui.Spacing();
        ImGui.Dummy(new Vector2(pad, 1f));
        ImGui.SameLine();
        using (ImRaii.Group())
        using (ImRaii.ItemWidth(SubWindowWidth * ImGuiHelpers.GlobalScale)) {

            if (ImGuiExt.ButtonWithIcon("Save Outfit", FontAwesomeIcon.Save, new Vector2(SubWindowWidth * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 2))) {
                Outfit.Name = outfitName;
                
                Outfit.Save(true);
                MainWindow?.PopPage();
            }
            
        }

    }

    private void DrawAppearance() {
        foreach (var v in System.Enum.GetValues<CustomizeIndex>()) {
            ShowCustomize(v);
        }
    }

    private void ShowCustomize(CustomizeIndex customizeIndex) {
        var customize = Outfit.Appearance[customizeIndex];
        ImGui.Checkbox($"##enableCustomize_{customizeIndex}", ref customize.Apply);
        ImGui.SameLine();
        CustomizeEditor.ShowReadOnly($"{customizeIndex}##customizeEditor_{customizeIndex}", customizeIndex, customize);
    }

    private void DrawParameters() {
        foreach (var v in System.Enum.GetValues<AppearanceParameterKind>()) {
            DrawParameter(v);
        }
    }

    private void DrawParameter(AppearanceParameterKind kind) {
        var param = Outfit.Appearance[kind];
        
        ImGui.Checkbox($"##enableParameter_{kind}", ref param.Apply);
        ImGui.SameLine();

        param.ShowEditor($"{kind}##paramEditor_{kind}", kind, true);
    }

    private void DrawEquipment() {
        foreach (var s in Common.GetGearSlots()) {
            ShowSlot(s);
        }
    }

    public void ShowSlot(HumanSlot slot) {
        var equip = Outfit.Equipment[slot];

        using (ImRaii.Group()) {
            using (ImRaii.Group()) {
                ImGui.Dummy(new Vector2(ImGui.GetTextLineHeight() / 2f));
                ImGui.Checkbox($"##enable_{slot}", ref equip.Apply);
            }

            ImGui.SameLine();
            using (ImRaii.Group()) {
                ShowSlot($"{slot}", slot, equip);
            }
        }

        if (slot == HumanSlot.Head) {
            ImGui.SameLine();
            using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One))
            using (ImRaii.Group()) {
                Outfit.Equipment.HatVisible.ShowToggleEditor("Headwear Visible");
                Outfit.Equipment.VisorToggle.ShowToggleEditor("Visor Toggle");
            }
        }
    }
    
    private void ShowSlot(string slotName, HumanSlot equipSlot, ApplicableItem equipment) {
        try {
            var equipItem = equipment.GetEquipItem(equipSlot);
            ShowSlot(slotName, equipItem.Name, equipItem.IconId.Id, equipment, equipment is ApplicableEquipment ae ? ae.Stain : null, equipment.Materials);
        } catch (Exception ex) {
            ImGui.TextWrapped($"{ex}");
        }
    }
    
    private void ShowSlot(string slotName, string itemName, uint iconId, ApplicableItem equipment, ApplicableStain? stains = null, List<ApplicableMaterial>? materials = null) {
        using (ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.One))
        using (ImRaii.PushId($"State_{slotName}")) {
           
            var tex = TextureProvider.GetFromGameIcon(iconId).GetWrapOrEmpty();
            ImGui.Image(tex.ImGuiHandle, new Vector2(ImGui.GetTextLineHeight() * 2 + ImGui.GetStyle().FramePadding.Y * 4 + ImGui.GetStyle().ItemSpacing.Y));
#if DEBUG
            if (ImGui.GetIO().KeyAlt && ImGui.GetIO().KeyShift) {
                ImGui.GetWindowDrawList().AddText(ImGui.GetItemRectMin(), 0xFF0000FF, $"{iconId}");
            }
#endif
            ImGui.SameLine();

            var s = new Vector2(280 * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeight() + ImGui.GetStyle().FramePadding.Y * 2);

            using (ImRaii.Group()) {
                ImGui.SetNextItemWidth(s.X - (materials is { Count: > 0 } ? s.Y + ImGui.GetStyle().ItemSpacing.X : 0) - (stains != null ? s.Y * 2 + ImGui.GetStyle().ItemSpacing.X * 2 : 0));

                ImGui.BeginGroup();
                ImGui.InputText("##itemName", ref itemName, 64, ImGuiInputTextFlags.ReadOnly);

                if (materials is { Count: > 0 }) {
                    ImGui.SameLine();
                    using (ImRaii.PushFont(UiBuilder.IconFont)) {
                        if (ImGui.Button(FontAwesomeIcon.Palette.ToIconString(), new Vector2(s.Y))) { }
                    }

                    if (ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();

                        ImGui.Text($"{slotName} Advanced Dyes");
                        ImGui.Separator();

                        using (ImRaii.PushColor(ImGuiCol.FrameBg, Vector4.Zero))
                        using (ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(3, ImGui.GetStyle().CellPadding.Y))) {
                            if (ImGui.BeginTable("materialsTable", 4)) {
                                foreach (var material in materials) {
                                    ImGui.TableNextColumn();
                                    var t = $"{material.MaterialValueIndex.MaterialString()} {material.MaterialValueIndex.RowString()}";
                                    ImGui.SetNextItemWidth(ImGui.CalcTextSize(t).X + ImGui.GetStyle().FramePadding.X * 2);
                                    ImGui.InputText("##material", ref t, 128, ImGuiInputTextFlags.ReadOnly);
                                    ImGui.TableNextColumn();
                                    ImGui.ColorButton("Diffuse", new Vector4(material.DiffuseR, material.DiffuseG, material.DiffuseB, 1));
                                    ImGui.SameLine();
                                    ImGui.ColorButton("Specular", new Vector4(material.SpecularR, material.SpecularG, material.SpecularB, 1));
                                    ImGui.SameLine();
                                    ImGui.ColorButton("Emissive", new Vector4(material.EmissiveR, material.EmissiveG, material.EmissiveB, 1));
                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{material.Gloss}");
                                    ImGui.TableNextColumn();
                                    ImGui.TextUnformatted($"{material.SpecularA * 100}%");
                                }

                                ImGui.EndTable();
                            }
                        }

                        ImGui.EndTooltip();
                    }
                }

                if (stains != null) {
                    ImGui.SameLine();
                    StainButton(stains.Stain, new Vector2(s.Y));
                    ImGui.SameLine();
                    StainButton(stains.Stain2, new Vector2(s.Y));
                }

                ImGui.EndGroup();
                
                ModListDisplay.Show(equipment);
            }
        }
    }
    
     private bool StainButton(byte stainId, Vector2 size, bool tooltip = true, bool isSelected = false) {
        var stain = DataManager.GetExcelSheet<Stain>()?.GetRow(stainId);
        return StainButton(stain, size, tooltip, isSelected);
    }

    private bool StainButton(Stain? stain, Vector2 size, bool tooltip = true, bool isSelected = false) {
        ImGui.Dummy(size);

        var drawOffset = size / 1.5f;
        var drawOffset2 = size / 2.25f;
        var pos = ImGui.GetItemRectMin();
        var center = ImGui.GetItemRectMin() + ImGui.GetItemRectSize() / 2;
        var dl = ImGui.GetWindowDrawList();
        var texture = TextureProvider.GetFromGame("ui/uld/ListColorChooser_hr1.tex").GetWrapOrEmpty();

        if (stain == null || stain.Value.RowId == 0) {
            dl.AddImage(texture.ImGuiHandle, center - drawOffset2, center + drawOffset2, new Vector2(0.8333333f, 0.3529412f), new Vector2(0.9444444f, 0.47058824f), 0x80FFFFFF);
            dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.27777f, 0.3529f), new Vector2(0.55555f, 0.64705f));
            if (ImGui.IsItemHovered()) dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.55555f, 0.3529f), new Vector2(0.83333f, 0.64705f));

            if (tooltip && ImGui.IsItemHovered()) ImGui.SetTooltip("No Dye");

            return ImGui.IsItemClicked();
        }

        var b = stain.Value.Color & 255;
        var g = (stain.Value.Color >> 8) & 255;
        var r = (stain.Value.Color >> 16) & 255;
        var stainVec4 = new Vector4(r / 255f, g / 255f, b / 255f, 1f);
        var stainColor = ImGui.GetColorU32(stainVec4);

        dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0, 0.3529f), new Vector2(0.27777f, 0.6470f), stainColor);
        if (stain.Value.Unknown1) {
            dl.PushClipRect(center - drawOffset2, center + drawOffset2);
            ImGui.ColorConvertRGBtoHSV(stainVec4.X, stainVec4.Y, stainVec4.Z, out var h, out var s, out var v);
            ImGui.ColorConvertHSVtoRGB(h, s, v - 0.5f, out var dR, out var dG, out var dB);
            ImGui.ColorConvertHSVtoRGB(h, s, v + 0.8f, out var bR, out var bG, out var bB);
            var dColor = ImGui.GetColorU32(new Vector4(dR, dG, dB, 1));
            var bColor = ImGui.GetColorU32(new Vector4(bR, bG, bB, 1));
            var tr = pos + size with { Y = 0 };
            var bl = pos + size with { X = 0 };
            var opacity = 0U;
            for (var x = 3; x < size.X; x++) {
                if (opacity < 0xF0_00_00_00U) opacity += 0x08_00_00_00U;
                dl.AddLine(tr + new Vector2(0, x), bl + new Vector2(x, 0), opacity | (0x00A0A0A0 & dColor), 2);
                dl.AddLine(tr - new Vector2(0, x), bl - new Vector2(x, 0), opacity | (0x00FFFFFF & bColor), 2);
            }

            dl.PopClipRect();
        }

        dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.27777f, 0.3529f), new Vector2(0.55555f, 0.64705f));
        if (isSelected || ImGui.IsItemHovered()) dl.AddImage(texture.ImGuiHandle, center - drawOffset, center + drawOffset, new Vector2(0.55555f, 0.3529f), new Vector2(0.83333f, 0.64705f));

        if (tooltip && ImGui.IsItemHovered()) ImGui.SetTooltip(stain.Value.Name.ExtractText());

        return ImGui.IsItemClicked();
    }
    
    
}
