using SkiaSharp;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows.NativeApi
{
    internal sealed class NativeIcons : IDisposable
    {
        private const uint WM_SETICON = 0x0080;

        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        private const int SM_CXICON = 11;
        private const int SM_CYICON = 12;
        private const int SM_CXSMICON = 49;
        private const int SM_CYSMICON = 50;

        private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

        private IntPtr _big;
        private IntPtr _small;

        internal void Apply(IntPtr window, SKBitmap bitmap, uint dpi)
        {
            if (window == IntPtr.Zero || bitmap == null)
            {
                return;
            }

            Release();

            _big = Build(bitmap, SM_CXICON, SM_CYICON, dpi);
            _small = Build(bitmap, SM_CXSMICON, SM_CYSMICON, dpi);

            Send(window, ICON_BIG, _big);
            Send(window, ICON_SMALL, _small);
        }

        private static void Send(IntPtr window, int which, IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                SendMessage(window, WM_SETICON, (IntPtr)which, handle);
            }
        }

        private static IntPtr Build(SKBitmap bitmap, int widthMetric, int heightMetric, uint dpi)
        {
            int width = GetSystemMetricsForDpi(widthMetric, dpi);
            int height = GetSystemMetricsForDpi(heightMetric, dpi);

            if (width <= 0 || height <= 0)
            {
                return IntPtr.Zero;
            }

            using (SKBitmap sized = Fit(bitmap, width, height))
            {
                return NativeIconBuilder.Create(sized, true, 0, 0);
            }
        }

        private static SKBitmap Fit(SKBitmap source, int width, int height)
        {
            SKBitmap current = source;
            bool owned = false;

            while (current.Width >= width * 2 && current.Height >= height * 2)
            {
                SKBitmap half = current.Resize(new SKImageInfo(current.Width / 2, current.Height / 2), Sampling);

                if (half == null)
                {
                    break;
                }

                if (owned)
                {
                    current.Dispose();
                }

                current = half;
                owned = true;
            }

            SKBitmap sized = current.Resize(new SKImageInfo(width, height), Sampling);

            if (owned)
            {
                current.Dispose();
            }

            return sized;
        }

        private void Release()
        {
            NativeIconBuilder.Release(_big);
            NativeIconBuilder.Release(_small);

            _big = IntPtr.Zero;
            _small = IntPtr.Zero;
        }

        public void Dispose()
        {
            Release();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetricsForDpi(int index, uint dpi);
    }
}
