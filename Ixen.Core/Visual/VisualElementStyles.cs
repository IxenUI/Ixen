using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles;

namespace Ixen.Core.Visual
{
    public class VisualElementStyles
    {
        public LayoutStyle Layout { get; set; } = new();
        public MaskStyle Mask { get; set; } = new();
        public SizeStyle Width { get; set; } = new();
        public SizeStyle Height { get; set; } = new();
        public MarginStyle Margin { get; set; } = new();
        public MarginStyle Padding { get; set; } = new();

        public BackgroundStyle Background { get; set; }
        public BorderStyle Border { get; set; }
    }
}
