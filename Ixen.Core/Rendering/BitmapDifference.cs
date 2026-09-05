using SkiaSharp;
using System;
using System.Globalization;

namespace Ixen.Core.Rendering
{
    internal sealed class BitmapDifference
    {
        private const byte DIMMED = 235;

        internal int Pixels { get; private set; }
        internal int Total { get; private set; }
        internal int MaxChannelDelta { get; private set; }
        internal int Left { get; private set; }
        internal int Top { get; private set; }
        internal int Right { get; private set; }
        internal int Bottom { get; private set; }

        internal bool Any => Pixels > 0;

        internal int Width => Any ? Right - Left + 1 : 0;

        internal int Height => Any ? Bottom - Top + 1 : 0;

        internal float Share => Total == 0 ? 0f : (float)Pixels / Total;

        private BitmapDifference()
        {
            Left = int.MaxValue;
            Top = int.MaxValue;
            Right = -1;
            Bottom = -1;
        }

        internal static bool Comparable(SKBitmap one, SKBitmap other)
        {
            return one != null && other != null
                && one.Width == other.Width
                && one.Height == other.Height
                && one.ColorType == other.ColorType
                && one.BytesPerPixel == other.BytesPerPixel
                && one.RowBytes == other.RowBytes;
        }

        internal static SKBitmap Normalized(SKBitmap bitmap)
        {
            if (bitmap == null)
            {
                return null;
            }

            return bitmap.Copy(SKColorType.Bgra8888);
        }

        internal static BitmapDifference Compare(SKBitmap one, SKBitmap other)
        {
            if (!Comparable(one, other))
            {
                return null;
            }

            byte[] left = one.Bytes;
            byte[] right = other.Bytes;
            int step = one.BytesPerPixel;
            int stride = one.RowBytes;

            var difference = new BitmapDifference
            {
                Total = one.Width * one.Height
            };

            for (int y = 0; y < one.Height; y++)
            {
                int row = y * stride;

                for (int x = 0; x < one.Width; x++)
                {
                    int delta = DeltaAt(left, right, row + x * step, step);

                    if (delta == 0)
                    {
                        continue;
                    }

                    difference.Pixels++;

                    if (delta > difference.MaxChannelDelta)
                    {
                        difference.MaxChannelDelta = delta;
                    }

                    if (x < difference.Left) { difference.Left = x; }
                    if (x > difference.Right) { difference.Right = x; }
                    if (y < difference.Top) { difference.Top = y; }
                    if (y > difference.Bottom) { difference.Bottom = y; }
                }
            }

            if (!difference.Any)
            {
                difference.Left = 0;
                difference.Top = 0;
            }

            return difference;
        }

        internal static SKBitmap Highlight(SKBitmap one, SKBitmap other)
        {
            if (!Comparable(one, other))
            {
                return null;
            }

            byte[] left = one.Bytes;
            byte[] right = other.Bytes;
            int step = one.BytesPerPixel;
            int stride = one.RowBytes;

            var picture = new SKBitmap(one.Width, one.Height, one.ColorType, SKAlphaType.Premul);

            for (int y = 0; y < one.Height; y++)
            {
                int row = y * stride;

                for (int x = 0; x < one.Width; x++)
                {
                    int at = row + x * step;

                    picture.SetPixel(x, y, DeltaAt(left, right, at, step) == 0
                        ? Faded(right, at)
                        : new SKColor(0xFF, 0x00, 0xFF));
                }
            }

            return picture;
        }

        private static int DeltaAt(byte[] left, byte[] right, int at, int step)
        {
            int delta = 0;

            for (int channel = 0; channel < step; channel++)
            {
                int gap = Math.Abs(left[at + channel] - right[at + channel]);

                if (gap > delta)
                {
                    delta = gap;
                }
            }

            return delta;
        }

        private static SKColor Faded(byte[] pixels, int at)
        {
            byte grey = (byte)((pixels[at] + pixels[at + 1] + pixels[at + 2]) / 3);
            byte lifted = (byte)(DIMMED - (DIMMED - grey) / 5);

            return new SKColor(lifted, lifted, lifted);
        }

        internal string Describe()
        {
            if (!Any)
            {
                return "identical";
            }

            string share = (Share * 100f).ToString("0.00", CultureInfo.InvariantCulture);

            return $"{Pixels} of {Total} pixels ({share}%) differ, "
                + $"largest channel gap {MaxChannelDelta} of 255, "
                + $"inside {Width}x{Height} at {Left},{Top}";
        }
    }
}
