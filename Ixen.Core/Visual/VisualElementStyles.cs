using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Handlers;

namespace Ixen.Core.Visual
{
    public class VisualElementStyles
    {
        public BackgroundStyleHandler Background { get; set; }
        public BorderStyleHandler Border { get; set; }
        public HeightStyleHandler Height { get; set; } = new();
        public LayoutStyleHandler Layout { get; set; } = new();
        public MarginStyleHandler Margin { get; set; } = new();
        public MaskStyleHandler Mask { get; set; } = new();
        public PaddingStyleHandler Padding { get; set; } = new();
        public WidthStyleHandler Width { get; set; } = new();

        public void ApplyStyle (StyleDescriptor style)
        {
            switch (style.Identifier)
            {
                case StyleIdentifier.Background:
                    Background = new BackgroundStyleHandler((BackgroundStyleDescriptor)style);
                    break;

                case StyleIdentifier.Border:
                    Border = new BorderStyleHandler((BorderStyleDescriptor)style);
                    break;

                case StyleIdentifier.Height:
                    Height = new HeightStyleHandler((HeightStyleDescriptor)style);
                    break;

                case StyleIdentifier.Layout:
                    Layout = new LayoutStyleHandler((LayoutStyleDescriptor)style);
                    break;

                case StyleIdentifier.Margin:
                    Margin = new MarginStyleHandler((MarginStyleDescriptor)style);
                    break;

                case StyleIdentifier.Mask:
                    Mask = new MaskStyleHandler((MaskStyleDescriptor)style);
                    break;

                case StyleIdentifier.Padding:
                    Padding = new PaddingStyleHandler((PaddingStyleDescriptor)style);
                    break;

                case StyleIdentifier.Width:
                    Width = new WidthStyleHandler((WidthStyleDescriptor)style);
                    break;

                default:
                    break;
            }
        }
    }
}
