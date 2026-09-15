using Ixen.Core;
using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Platform.Mac.NativeApi
{
    internal sealed class MacCursorImages
    {
        private sealed class Entry
        {
            internal byte[] Bytes;
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
                    return cached.Bytes;
                }

                _entries.Remove(key);
            }

            byte[] bytes = Encode(image.Bitmap);

            _entries[key] = new Entry { Bytes = bytes, Bitmap = image.Bitmap };

            return bytes;
        }

        private static byte[] Encode(SKBitmap bitmap)
        {
            if (bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return null;
            }

            using (SKData data = bitmap.Encode(SKEncodedImageFormat.Png, 100))
            {
                return data?.ToArray();
            }
        }

        internal void Clear() => _entries.Clear();
    }
}
