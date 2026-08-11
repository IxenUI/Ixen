using Ixen.Core.Visual;
using SkiaSharp;

namespace Ixen.Core.Rendering
{
    internal sealed class SkiaTextMeasurer : ITextMeasurer
    {
        internal static readonly SkiaTextMeasurer Default = new();

        public void MeasureText(string text, FontSpec font, out float width, out float height)
        {
            if (string.IsNullOrEmpty(text) || font.Size <= 0)
            {
                width = 0;
                height = 0;
                return;
            }

            SKFont skFont = FontCache.Get(font);

            width = skFont.MeasureText(text);
            height = skFont.Spacing;
        }

        public float GetLineHeight(FontSpec font)
        {
            if (font.Size <= 0)
            {
                return 0;
            }

            return FontCache.Get(font).Spacing;
        }
    }
}
