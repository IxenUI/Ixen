using Ixen.Core.Visual;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal static class FontCache
    {
        private static readonly Dictionary<(string family, float size, bool bold, bool italic), SKFont> _fonts = new();

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
    }
}
