using Ixen.Core.Visual.Styles.Handlers;

namespace Ixen.Core.Visual
{
    internal class VisualElementStylesHandlers
    {
        public BackgroundStyleHandler Background { get; set; }
        public BorderStyleHandler Border { get; set; }
        public ColumnTemplateStyleHandler ColumnTemplate { get; set; }
        public HeightStyleHandler Height { get; set; }
        public LayoutStyleHandler Layout { get; set; }
        public MarginStyleHandler Margin { get; set; }
        public MaskStyleHandler Mask { get; set; }
        public PaddingStyleHandler Padding { get; set; }
        public RowTemplateStyleHandler RowTemplate { get; set; }
        public WidthStyleHandler Width { get; set; }
    }
}
