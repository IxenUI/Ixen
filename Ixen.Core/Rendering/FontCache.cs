using Ixen.Core.Visual;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal static class FontCache
    {
        private const char MAX_ASCII = (char)0x7F;

        private static readonly Dictionary<(string family, float size, bool bold, bool italic, bool smooth), SKFont> _fonts = new();

        private static readonly Dictionary<(string family, bool bold, bool italic, int codepoint), SKTypeface> _faces = new();

        private static readonly Dictionary<(SKTypeface face, float size, bool smooth), SKFont> _fallbacks = new();

        private static readonly Dictionary<(SKFont font, int codepoint), bool> _covers = new();

        private static string _defaultFamily;

        internal static SKFont Get(FontSpec spec) => Get(spec, false);

        internal static SKFont Get(FontSpec spec, bool smooth)
        {
            (string, float, bool, bool, bool) key = (spec.Family ?? string.Empty, spec.Size,
                spec.Bold, spec.Italic, smooth);

            if (_fonts.TryGetValue(key, out SKFont font))
            {
                return font;
            }

            SKTypeface typeface = SKTypeface.FromFamilyName(
                FamilyOf(spec),
                spec.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                spec.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright)
                ?? SKTypeface.Default;

            font = Shape(new SKFont(typeface, spec.Size), smooth);
            _fonts[key] = font;

            return font;
        }

        private static string FamilyOf(FontSpec spec)
        {
            if (!string.IsNullOrWhiteSpace(spec.Family))
            {
                return spec.Family;
            }

            if (_defaultFamily == null)
            {
                SKTypeface face = SKTypeface.Default;

                _defaultFamily = face == null || string.IsNullOrWhiteSpace(face.FamilyName)
                    ? string.Empty
                    : face.FamilyName;
            }

            return _defaultFamily.Length == 0 ? null : _defaultFamily;
        }

        private static SKFont Shape(SKFont font, bool smooth)
        {
            if (smooth)
            {
                font.Hinting = SKFontHinting.None;
                font.Subpixel = true;
            }

            return font;
        }

        internal static SKFont Get(FontSpec spec, string text) => Get(spec, text, false);

        internal static SKFont Get(FontSpec spec, string text, bool smooth)
        {
            SKFont font = Get(spec, smooth);

            if (!MayBeUncovered(text))
            {
                return font;
            }

            int missing = FirstUncovered(font, text);

            if (missing < 0)
            {
                return font;
            }

            return Fallback(spec, missing, smooth) ?? font;
        }

        internal static bool Split(FontSpec spec, string text, bool smooth, List<FontRun> runs)
        {
            runs.Clear();

            if (!MayBeUncovered(text))
            {
                return false;
            }

            SKFont primary = Get(spec, smooth);

            if (FirstUncovered(primary, text) < 0)
            {
                return false;
            }

            SKFont current = null;
            int start = 0;

            for (int index = 0; index < text.Length;)
            {
                int length = CodepointAt(text, index, out int codepoint);
                SKFont font = FontFor(spec, smooth, primary, current, codepoint);

                if (font != current)
                {
                    if (current != null)
                    {
                        runs.Add(new FontRun(start, index - start, current));
                    }

                    current = font;
                    start = index;
                }

                index += length;
            }

            runs.Add(new FontRun(start, text.Length - start, current));

            return runs.Count > 1;
        }

        private static SKFont FontFor(FontSpec spec, bool smooth, SKFont primary, SKFont current, int codepoint)
        {
            if (current != null && IsSticky(codepoint))
            {
                return current;
            }

            if (codepoint <= MAX_ASCII || Covers(primary, codepoint))
            {
                return current != null && current != primary && IsNeutral(codepoint) && Covers(current, codepoint)
                    ? current
                    : primary;
            }

            if (current != null && current != primary && Covers(current, codepoint))
            {
                return current;
            }

            return Fallback(spec, codepoint, smooth) ?? primary;
        }

        private static int CodepointAt(string text, int index, out int codepoint)
        {
            char c = text[index];

            if (char.IsHighSurrogate(c) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]))
            {
                codepoint = char.ConvertToUtf32(c, text[index + 1]);

                return 2;
            }

            codepoint = c;

            return 1;
        }

        private static bool IsSticky(int codepoint)
        {
            if (codepoint == 0x200D || (codepoint >= 0xFE00 && codepoint <= 0xFE0F)
                || (codepoint >= 0xE0100 && codepoint <= 0xE01EF) || (codepoint >= 0x1F3FB && codepoint <= 0x1F3FF))
            {
                return true;
            }

            if (codepoint > 0xFFFF)
            {
                return false;
            }

            System.Globalization.UnicodeCategory category = char.GetUnicodeCategory((char)codepoint);

            return category == System.Globalization.UnicodeCategory.NonSpacingMark
                || category == System.Globalization.UnicodeCategory.SpacingCombiningMark
                || category == System.Globalization.UnicodeCategory.EnclosingMark;
        }

        private static bool IsNeutral(int codepoint)
        {
            if (codepoint > 0xFFFF)
            {
                return false;
            }

            char c = (char)codepoint;

            return char.IsWhiteSpace(c) || char.IsPunctuation(c) || char.IsSymbol(c) || char.IsDigit(c);
        }

        internal static float Width(FontSpec spec, string text, bool smooth, List<FontRun> runs)
        {
            if (!Split(spec, text, smooth, runs))
            {
                return Get(spec, text, smooth).MeasureText(text);
            }

            float width = 0;

            foreach (FontRun run in runs)
            {
                width += run.Font.MeasureText(text.AsSpan(run.Start, run.Length));
            }

            return width;
        }

        private static bool MayBeUncovered(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char c in text)
            {
                if (c > MAX_ASCII)
                {
                    return true;
                }
            }

            return false;
        }

        private static int FirstUncovered(SKFont font, string text)
        {
            if (font?.Typeface == null)
            {
                return -1;
            }

            for (int index = 0; index < text.Length; index++)
            {
                char c = text[index];

                if (c <= MAX_ASCII)
                {
                    continue;
                }

                int codepoint = c;

                if (char.IsHighSurrogate(c) && index + 1 < text.Length
                    && char.IsLowSurrogate(text[index + 1]))
                {
                    codepoint = char.ConvertToUtf32(c, text[index + 1]);
                    index++;
                }

                if (!Covers(font, codepoint))
                {
                    return codepoint;
                }
            }

            return -1;
        }

        private static bool Covers(SKFont font, int codepoint)
        {
            (SKFont, int) key = (font, codepoint);

            if (_covers.TryGetValue(key, out bool covered))
            {
                return covered;
            }

            covered = font.ContainsGlyph(codepoint);
            _covers[key] = covered;

            return covered;
        }

        private static SKFont Fallback(FontSpec spec, int codepoint, bool smooth)
        {
            SKTypeface face = FallbackFace(spec, codepoint);

            if (face == null)
            {
                return null;
            }

            (SKTypeface, float, bool) key = (face, spec.Size, smooth);

            if (_fallbacks.TryGetValue(key, out SKFont font))
            {
                return font;
            }

            font = Shape(new SKFont(face, spec.Size), smooth);
            _fallbacks[key] = font;

            return font;
        }

        private static SKTypeface FallbackFace(FontSpec spec, int codepoint)
        {
            (string, bool, bool, int) key = (spec.Family ?? string.Empty, spec.Bold, spec.Italic, codepoint);

            if (_faces.TryGetValue(key, out SKTypeface face))
            {
                return face;
            }

            face = SKFontManager.Default.MatchCharacter(
                string.IsNullOrWhiteSpace(spec.Family) ? null : spec.Family,
                spec.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                spec.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright,
                null,
                codepoint);

            _faces[key] = face;

            return face;
        }
    }
}
