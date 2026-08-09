using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal static class FontCache
    {
        private static readonly Dictionary<(string family, float size), SKFont> _fonts = new();

        internal static SKFont Get(string family, float size)
        {
            (string, float) key = (family ?? string.Empty, size);

            if (_fonts.TryGetValue(key, out SKFont font))
            {
                return font;
            }

            SKTypeface typeface = string.IsNullOrWhiteSpace(family)
                ? SKTypeface.Default
                : SKTypeface.FromFamilyName(family) ?? SKTypeface.Default;

            font = new SKFont(typeface, size);
            _fonts[key] = font;

            return font;
        }
    }
}
