using Ixen.Core.Visual;
using SkiaSharp;

namespace Ixen.Core.Rendering
{
    internal sealed class SkiaTextMeasurer : ITextMeasurer
    {
        internal static readonly SkiaTextMeasurer Default = new();

        public void MeasureText(string text, string fontFamily, float fontSize, out float width, out float height)
        {
            if (string.IsNullOrEmpty(text) || fontSize <= 0)
            {
                width = 0;
                height = 0;
                return;
            }

            SKFont font = FontCache.Get(fontFamily, fontSize);

            width = font.MeasureText(text);
            height = font.Spacing;
        }
    }
}
