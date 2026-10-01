using Ixen.Core.Rendering;
using SkiaSharp;
using System;
using System.Globalization;

namespace Ixen.View.Android
{
    public sealed class IxenFrameCapture : IDisposable
    {
        private readonly BitmapDifference _difference;

        internal IxenFrameCapture(SKBitmap host, SKBitmap library, int width, int height,
            float scale, float fontScale)
        {
            Host = BitmapDifference.Normalized(host);
            Library = BitmapDifference.Normalized(library);
            Width = width;
            Height = height;
            Scale = scale;
            FontScale = fontScale;

            _difference = BitmapDifference.Compare(Host, Library);
        }

        public SKBitmap Host { get; }

        public SKBitmap Library { get; }

        public int Width { get; }

        public int Height { get; }

        public float Scale { get; }

        public float FontScale { get; }

        public bool Matches => _difference != null && !_difference.Any;

        public string Difference
            => Host == null ? "the host produced no frame"
            : Library == null ? "the library produced no frame"
            : _difference == null ? string.Format(CultureInfo.InvariantCulture,
                    "the two frames cannot be compared, {0} x {1} against {2} x {3}",
                    Host.Width, Host.Height, Library.Width, Library.Height)
            : _difference.Describe();

        public string Describe()
        {
            return string.Format(CultureInfo.InvariantCulture,
                "{0} x {1} at scale {2} font scale {3} : {4}",
                Width, Height, Scale, FontScale, Difference);
        }

        public void Dispose()
        {
            Host?.Dispose();
            Library?.Dispose();
        }
    }
}
