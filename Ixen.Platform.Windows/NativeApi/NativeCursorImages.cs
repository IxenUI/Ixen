using Ixen.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ixen.Platform.Windows.NativeApi
{
    internal sealed class NativeCursorImages : IDisposable
    {
        private sealed class Entry
        {
            internal IntPtr Handle;
            internal SKBitmap Bitmap;
        }

        private readonly Dictionary<string, Entry> _entries = new();

        internal IntPtr Get(CursorImage image)
        {
            if (image?.Bitmap == null)
            {
                return IntPtr.Zero;
            }

            string key = image.Name + "|" + image.HotspotX + "|" + image.HotspotY;

            if (_entries.TryGetValue(key, out Entry cached))
            {
                if (cached.Bitmap == image.Bitmap)
                {
                    return cached.Handle;
                }

                NativeIconBuilder.Release(cached.Handle);
                _entries.Remove(key);
            }

            IntPtr handle = NativeIconBuilder.Create(image.Bitmap, false, image.HotspotX, image.HotspotY);

            _entries[key] = new Entry { Handle = handle, Bitmap = image.Bitmap };

            return handle;
        }

        public void Dispose()
        {
            foreach (Entry entry in _entries.Values)
            {
                NativeIconBuilder.Release(entry.Handle);
            }

            _entries.Clear();
        }
    }
}
