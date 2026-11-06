using Ixen.Core.Visual;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal sealed class SkiaTextMeasurer : ITextMeasurer
    {
        internal static readonly SkiaTextMeasurer Default = new();

        private readonly List<FontRun> _runs = new List<FontRun>();

        public void MeasureText(string text, FontSpec font, out float width, out float height)
        {
            if (string.IsNullOrEmpty(text) || font.Size <= 0)
            {
                width = 0;
                height = 0;
                return;
            }

            lock (_runs)
            {
                width = FontCache.Width(font, text, false, _runs) + font.Advance(text);
            }

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

            lock (_runs)
            {
                if (FontCache.Split(font, text, false, _runs))
                {
                    foreach (FontRun run in _runs)
                    {
                        Advances(text, run.Start, run.Length, run.Font, advances);
                    }
                }
                else
                {
                    Advances(text, 0, text.Length, FontCache.Get(font, text), advances);
                }
            }

            float spacing = font.LetterSpacing;

            if (spacing == 0)
            {
                return;
            }

            for (int index = 0; index < text.Length; index++)
            {
                if (index > 0 && char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1]))
                {
                    continue;
                }

                advances[index] += spacing;
            }
        }

        private static void Advances(string text, int start, int length, SKFont font, float[] advances)
        {
            ReadOnlySpan<char> span = text.AsSpan(start, length);

            if (font.CountGlyphs(span) == length)
            {
                font.GetGlyphWidths(span, new Span<float>(advances, start, length), Span<SKRect>.Empty, null);

                return;
            }

            for (int index = start; index < start + length; index++)
            {
                int size = char.IsHighSurrogate(text[index])
                    && index + 1 < start + length
                    && char.IsLowSurrogate(text[index + 1]) ? 2 : 1;

                advances[index] = font.MeasureText(text.AsSpan(index, size));

                if (size == 2)
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
