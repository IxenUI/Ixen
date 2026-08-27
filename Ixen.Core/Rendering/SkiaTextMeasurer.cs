using Ixen.Core.Visual;
using SkiaSharp;
using System;

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

            width = skFont.MeasureText(text) + font.Advance(text);

            height = GetLineHeight(font);
        }

        public void MeasureCharacters(string text, FontSpec font, float[] advances)
        {
            if (string.IsNullOrEmpty(text) || font.Size <= 0)
            {
                return;
            }

            SKFont skFont = FontCache.Get(font);
            float spacing = font.LetterSpacing;

            for (int index = 0; index < text.Length; index++)
            {
                advances[index] = skFont.MeasureText(text.AsSpan(index, 1)) + spacing;
            }
        }

        public float GetLineHeight(FontSpec font)
        {
            if (font.Size <= 0)
            {
                return 0;
            }

            return font.LineHeight > 0 ? font.LineHeight : FontCache.Get(font).Spacing;
        }
    }
}
