using Ixen.Core;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows.NativeApi
{
    internal sealed class NativeCursorImages : IDisposable
    {
        private const int BI_RGB = 0;
        private const uint DIB_RGB_COLORS = 0;

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

                Release(cached.Handle);
                _entries.Remove(key);
            }

            IntPtr handle = Create(image.Bitmap, image.HotspotX, image.HotspotY);

            _entries[key] = new Entry { Handle = handle, Bitmap = image.Bitmap };

            return handle;
        }

        private static IntPtr Create(SKBitmap bitmap, int hotspotX, int hotspotY)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;

            if (width <= 0 || height <= 0)
            {
                return IntPtr.Zero;
            }

            var header = new BITMAPINFOHEADER
            {
                biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER)),
                biWidth = width,
                biHeight = -height,
                biPlanes = 1,
                biBitCount = 32,
                biCompression = BI_RGB
            };

            IntPtr colour = CreateDIBSection(IntPtr.Zero, ref header, DIB_RGB_COLORS,
                out IntPtr bits, IntPtr.Zero, 0);

            if (colour == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }

            byte[] pixels = Straight(bitmap);

            Marshal.Copy(pixels, 0, bits, pixels.Length);

            int maskStride = ((width + 31) / 32) * 4;
            var empty = new byte[maskStride * height];

            IntPtr mask = CreateBitmap(width, height, 1, 1, empty);

            if (mask == IntPtr.Zero)
            {
                DeleteObject(colour);

                return IntPtr.Zero;
            }

            var info = new ICONINFO
            {
                fIcon = false,
                xHotspot = hotspotX,
                yHotspot = hotspotY,
                hbmMask = mask,
                hbmColor = colour
            };

            IntPtr cursor = CreateIconIndirect(ref info);

            DeleteObject(mask);
            DeleteObject(colour);

            return cursor;
        }

        private static byte[] Straight(SKBitmap bitmap)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            var pixels = new byte[width * height * 4];
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    SKColor colour = bitmap.GetPixel(x, y);

                    pixels[index++] = colour.Blue;
                    pixels[index++] = colour.Green;
                    pixels[index++] = colour.Red;
                    pixels[index++] = colour.Alpha;
                }
            }

            return pixels;
        }

        private static void Release(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                DestroyIcon(handle);
            }
        }

        public void Dispose()
        {
            foreach (Entry entry in _entries.Values)
            {
                Release(entry.Handle);
            }

            _entries.Clear();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public int biSize;
            public int biWidth;
            public int biHeight;
            public short biPlanes;
            public short biBitCount;
            public int biCompression;
            public int biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public int biClrUsed;
            public int biClrImportant;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDIBSection(IntPtr dc, ref BITMAPINFOHEADER header,
            uint usage, out IntPtr bits, IntPtr section, uint offset);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateBitmap(int width, int height, uint planes,
            uint bitCount, byte[] bits);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconIndirect(ref ICONINFO info);

        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr handle);
    }
}
