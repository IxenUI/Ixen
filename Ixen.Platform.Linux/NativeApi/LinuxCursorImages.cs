using Ixen.Core;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Platform.Linux.NativeApi
{
    internal sealed class LinuxCursorImages
    {
        private sealed class Entry
        {
            internal byte[] Pixels;
            internal SKBitmap Bitmap;
        }

        private readonly Dictionary<string, Entry> _entries = new();

        internal byte[] Get(CursorImage image)
        {
            if (image?.Bitmap == null)
            {
                return null;
            }

            string key = image.Name + "|" + image.HotspotX + "|" + image.HotspotY;

            if (_entries.TryGetValue(key, out Entry cached))
            {
                if (cached.Bitmap == image.Bitmap)
                {
                    return cached.Pixels;
                }

                _entries.Remove(key);
            }

            byte[] pixels = Premultiplied(image.Bitmap);

            _entries[key] = new Entry { Pixels = pixels, Bitmap = image.Bitmap };

            return pixels;
        }

        internal static byte[] Premultiplied(SKBitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            if (width <= 0 || height <= 0)
            {
                return null;
            }

            var pixels = new byte[width * height * 4];
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SKColor colour = bitmap.GetPixel(x, y);
                    byte alpha = colour.Alpha;

                    pixels[index++] = Scale(colour.Blue, alpha);
                    pixels[index++] = Scale(colour.Green, alpha);
                    pixels[index++] = Scale(colour.Red, alpha);
                    pixels[index++] = alpha;
                }
            }

            return pixels;
        }

        private static byte Scale(byte channel, byte alpha)
        {
            return (byte)((channel * alpha + 127) / 255);
        }

        internal void Clear() => _entries.Clear();
    }
}
