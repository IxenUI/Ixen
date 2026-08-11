using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    public class VisualElementStylesDescriptors
    {
        public BackgroundStyleDescriptor Background { get; set; } = new();
        public BorderStyleDescriptor Border { get; set; } = new();
        public ColorStyleDescriptor Color { get; set; } = new();
        public ColumnTemplateStyleDescriptor ColumnTemplate { get; set; } = new();
        public CornerRadiusStyleDescriptor CornerRadius { get; set; } = new();
        public FontFamilyStyleDescriptor FontFamily { get; set; } = new();
        public FontSizeStyleDescriptor FontSize { get; set; } = new();
        public HeightStyleDescriptor Height { get; set; } = new();
        public LayoutStyleDescriptor Layout { get; set; } = new();
        public MarginStyleDescriptor Margin { get; set; } = new();
        public PaddingStyleDescriptor Padding { get; set; } = new();
        public RowTemplateStyleDescriptor RowTemplate { get; set; } = new();
        public TextAlignStyleDescriptor TextAlign { get; set; } = new();
        public TextVAlignStyleDescriptor TextVAlign { get; set; } = new();
        public TextWrapStyleDescriptor TextWrap { get; set; } = new();
        public WidthStyleDescriptor Width { get; set; } = new();
    }
}
