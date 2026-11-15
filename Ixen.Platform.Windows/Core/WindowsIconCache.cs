using Ixen.Core;
using Ixen.Core.Rendering;
using Ixen.Core.Visual.Shapes;
using Ixen.Platform.Windows.NativeApi;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal sealed class WindowsIconCache : IDisposable
    {
        private const int SM_CXSMICON = 49;

        private readonly Func<IImageSource> _images;
        private readonly Dictionary<string, IntPtr> _icons = new Dictionary<string, IntPtr>(StringComparer.Ordinal);

        internal WindowsIconCache(Func<IImageSource> images)
        {
            _images = images;
        }

        internal static int SmallSize(IntPtr windowPtr)
            => GetSystemMetricsForDpi(SM_CXSMICON, WindowApi.GetWindowDpi(windowPtr));

        internal IntPtr Get(string name, int size)
        {
            if (string.IsNullOrWhiteSpace(name) || size <= 0)
            {
                return IntPtr.Zero;
            }

            string key = size + "|" + name;

            if (_icons.TryGetValue(key, out IntPtr cached))
            {
                return cached;
            }

            IntPtr handle = IntPtr.Zero;

            using (SKBitmap bitmap = Load(name, size))
            {
                if (bitmap != null)
                {
                    handle = NativeIconBuilder.Create(bitmap, true, 0, 0);
                }
            }

            _icons[key] = handle;

            return handle;
        }

        private SKBitmap Load(string name, int size)
        {
            byte[] bytes;

            using (Stream stream = _images()?.Open(name))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    bytes = memory.ToArray();
                }
            }

            using (SKData data = SKData.CreateCopy(bytes))
            using (SKCodec codec = SKCodec.Create(data))
            {
                if (codec != null)
                {
                    using (SKBitmap decoded = SKBitmap.Decode(codec))
                    {
                        return decoded == null ? null : NativeIcons.Fit(decoded, size, size);
                    }
                }
            }

            using (var memory = new MemoryStream(bytes))
            {
                return SvgRasteriser.Render(SvgDocument.Parse(memory), size, size);
            }
        }

        public void Dispose()
        {
            foreach (IntPtr icon in _icons.Values)
            {
                NativeIconBuilder.Release(icon);
            }

            _icons.Clear();
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    }
}
