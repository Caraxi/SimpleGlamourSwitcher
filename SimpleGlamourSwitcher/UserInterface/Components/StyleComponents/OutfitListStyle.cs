namespace SimpleGlamourSwitcher.UserInterface.Components.StyleComponents;

public record OutfitListStyle : StyleProvider<OutfitListStyle> {
    public PolaroidStyle Polaroid = new();
    
    public Colour OutfitColour = PolaroidStyle.Default.FrameColour; // full outfit, not default
    public Colour DefaultOutfitColour = 0xFF8EBF86; // full outfit, set as default
    public Colour MinorOutfitColour = 0xFFBA9DC2;
    public Colour MinorDefaultColour = 0xFF946A90;
}
