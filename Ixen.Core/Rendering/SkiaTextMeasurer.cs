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

            SKFont skFont = FontCache.Get(font, text);

            width = skFont.MeasureText(text) + font.Advance(text);

            height = GetLineHeight(font);
        }

        public void MeasureCharacters(string text, FontSpec font, float[] advances)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (font.Size <= 0)
            {
                Array.Clear(advances, 0, text.Length);
                return;
            }

            SKFont skFont = FontCache.Get(font, text);
            float spacing = font.LetterSpacing;

            if (skFont.CountGlyphs(text) == text.Length)
            {
                skFont.GetGlyphWidths(text, new Span<float>(advances, 0, text.Length),
                    Span<SKRect>.Empty, null);

                if (spacing != 0)
                {
                    for (int index = 0; index < text.Length; index++)
                    {
                        advances[index] += spacing;
                    }
                }

                return;
            }

            for (int index = 0; index < text.Length; index++)
            {
                int length = char.IsHighSurrogate(text[index])
                    && index + 1 < text.Length
                    && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;

                advances[index] = skFont.MeasureText(text.AsSpan(index, length)) + spacing;

                if (length == 2)
                {
                    advances[index + 1] = 0;
                    index++;
                }
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
