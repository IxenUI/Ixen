using Ixen.Core.Visual.Styles.Handlers;

namespace Ixen.Core.Visual
{
    public class VisualElementStylesHandlers
    {
        public BackgroundStyleHandler Background { get; set; }
        public BorderStyleHandler Border { get; set; }
        public HeightStyleHandler Height { get; set; } = new();
        public LayoutStyleHandler Layout { get; set; } = new();
        public MarginStyleHandler Margin { get; set; } = new();
        public MaskStyleHandler Mask { get; set; } = new();
        public PaddingStyleHandler Padding { get; set; } = new();
        public WidthStyleHandler Width { get; set; } = new();
    }
}
