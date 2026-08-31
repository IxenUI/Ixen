using Ixen.Core.Visual;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal static class FontCache
    {
        private const char MAX_ASCII = (char)0x7F;

        private static readonly Dictionary<(string family, float size, bool bold, bool italic), SKFont> _fonts = new();

        private static readonly Dictionary<(string family, bool bold, bool italic, int codepoint), SKTypeface> _faces = new();

        private static readonly Dictionary<(SKTypeface face, float size), SKFont> _fallbacks = new();

        private static readonly Dictionary<(SKTypeface face, int codepoint), bool> _covers = new();

        internal static SKFont Get(FontSpec spec)
        {
            (string, float, bool, bool) key = (spec.Family ?? string.Empty, spec.Size, spec.Bold, spec.Italic);

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

            font = new SKFont(typeface, spec.Size);
            _fonts[key] = font;

            return font;
        }

        internal static SKFont Get(FontSpec spec, string text)
        {
            SKFont font = Get(spec);

            if (!MayBeUncovered(text))
            {
                return font;
            }

            int missing = FirstUncovered(font.Typeface, text);

            if (missing < 0)
            {
                return font;
            }

            return Fallback(spec, missing) ?? font;
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

        private static int FirstUncovered(SKTypeface typeface, string text)
        {
            if (typeface == null)
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

                if (!Covers(typeface, codepoint))
                {
                    return codepoint;
                }
            }

            return -1;
        }

        private static bool Covers(SKTypeface typeface, int codepoint)
        {
            (SKTypeface, int) key = (typeface, codepoint);

            if (_covers.TryGetValue(key, out bool covered))
            {
                return covered;
            }

            covered = typeface.ContainsGlyph(codepoint);
            _covers[key] = covered;

            return covered;
        }

        private static SKFont Fallback(FontSpec spec, int codepoint)
        {
            SKTypeface face = FallbackFace(spec, codepoint);

            if (face == null)
            {
                return null;
            }

            (SKTypeface, float) key = (face, spec.Size);

            if (_fallbacks.TryGetValue(key, out SKFont font))
            {
                return font;
            }

            font = new SKFont(face, spec.Size);
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
