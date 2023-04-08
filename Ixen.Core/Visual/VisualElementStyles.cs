using Ixen.Core.Visual.Styles;

namespace Ixen.Core.Visual
{
    public class VisualElementStyles
    {
        public BackgroundStyle Background { get; set; }
        public BorderStyle Border { get; set; }
        public SizeStyle Height { get; set; } = new();
        public LayoutStyle Layout { get; set; } = new();
        public MarginStyle Margin { get; set; } = new();
        public MaskStyle Mask { get; set; } = new();
        public MarginStyle Padding { get; set; } = new();
        public SizeStyle Width { get; set; } = new();

        public void ApplyStyle (Style style)
        {
            switch (style.Identifier)
            {
                case StyleIdentifier.Background:
                    Background = (BackgroundStyle)style;
                    break;

                case StyleIdentifier.Border:
                    Border = (BorderStyle)style;
                    break;

                case StyleIdentifier.Height:
                    Height = (HeightStyle)style;
                    break;

                case StyleIdentifier.Layout:
                    Layout = (LayoutStyle)style;
                    break;

                case StyleIdentifier.Margin:
                    Margin = (MarginStyle)style;
                    break;

                case StyleIdentifier.Mask:
                    Mask = (MaskStyle)style;
                    break;

                case StyleIdentifier.Padding:
                    Padding = (PaddingStyle)style;
                    break;

                case StyleIdentifier.Width:
                    Width = (WidthStyle)style;
                    break;

                default:
                    break;
            }
        }
    }
}
