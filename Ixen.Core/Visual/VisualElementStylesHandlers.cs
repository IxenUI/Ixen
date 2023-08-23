using Ixen.Core.Visual.Styles.Handlers;

namespace Ixen.Core.Visual
{
    internal class VisualElementStylesHandlers
    {
        public BackgroundStyleHandler Background { get; set; } = new();
        public BorderStyleHandler Border { get; set; } = new();
        public ColumnTemplateStyleHandler ColumnTemplate { get; set; } = new();
        public HeightStyleHandler Height { get; set; } = new();
        public LayoutStyleHandler Layout { get; set; } = new();
        public MarginStyleHandler Margin { get; set; } = new();
        public PaddingStyleHandler Padding { get; set; } = new();
        public RowTemplateStyleHandler RowTemplate { get; set; } = new();
        public WidthStyleHandler Width { get; set; } = new();
    }
}
