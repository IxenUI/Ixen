using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    public class VisualElementStylesDescriptors
    {
        public BackgroundStyleDescriptor Background { get; set; }
        public BorderStyleDescriptor Border { get; set; }
        public ColumnTemplateStyleDescriptor ColumnTemplate { get; set; }
        public HeightStyleDescriptor Height { get; set; } = new();
        public LayoutStyleDescriptor Layout { get; set; } = new();
        public MarginStyleDescriptor Margin { get; set; } = new();
        public MaskStyleDescriptor Mask { get; set; } = new();
        public PaddingStyleDescriptor Padding { get; set; } = new();
        public RowTemplateStyleDescriptor RowTemplate { get; set; }
        public WidthStyleDescriptor Width { get; set; } = new();
    }
}
