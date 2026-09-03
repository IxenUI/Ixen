using Ixen.Core.Visual;
using SkiaSharp;
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
                string.IsNullOrWhiteSpace(spec.Family) ? null : spec.Family,
                spec.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal,
                SKFontStyleWidth.Normal,
                spec.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright)
                ?? SKTypeface.Default;

            font = Shape(new SKFont(typeface, spec.Size), smooth);
            _fonts[key] = font;

            return font;
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
