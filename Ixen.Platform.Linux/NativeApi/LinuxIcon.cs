using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Platform.Linux.NativeApi
{
    internal static class LinuxIcon
    {
        internal static readonly int[] SIZES = { 16, 32, 48, 64, 128 };

        private static readonly SKSamplingOptions Sampling = new(SKCubicResampler.Mitchell);

        internal static int[] Cardinals(SKBitmap source)
        {
            if (source == null || source.Width <= 0 || source.Height <= 0)
            {
                return null;
            }

            var data = new List<int>();

            foreach (int size in SIZES)
            {
                if (size > source.Width && size > SIZES[0])
                {
                    continue;
                }

                using (SKBitmap sized = Fit(source, size))
                {
                    if (sized == null)
                    {
                        continue;
                    }

                    data.Add(size);
                    data.Add(size);

                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            SKColor colour = sized.GetPixel(x, y);

                            data.Add((colour.Alpha << 24) | (colour.Red << 16) | (colour.Green << 8) | colour.Blue);
                        }
                    }
                }
            }

            return data.Count == 0 ? null : data.ToArray();
        }

        private static SKBitmap Fit(SKBitmap source, int size)
        {
            SKBitmap current = source;
            bool owned = false;

            while (current.Width >= size * 2 && current.Height >= size * 2)
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

            SKBitmap sized = current.Resize(new SKImageInfo(size, size), Sampling);

            if (owned)
            {
                current.Dispose();
            }

            return sized;
        }
    }
}
