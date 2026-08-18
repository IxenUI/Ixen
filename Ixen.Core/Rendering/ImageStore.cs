using Ixen.Core.Visual;
using SkiaSharp;
using System.Collections.Generic;
using System.IO;

namespace Ixen.Core.Rendering
{
    internal class ImageStore : IImageMeasurer
    {
        private readonly Dictionary<string, SKBitmap> _bitmaps = new Dictionary<string, SKBitmap>();

        private IImageSource _source;

        internal IImageSource Source
        {
            get => _source;
            set
            {
                if (_source == value)
                {
                    return;
                }

                _source = value;
                Clear();
            }
        }

        internal SKBitmap Get(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (_bitmaps.TryGetValue(name, out SKBitmap cached))
            {
                return cached;
            }

            SKBitmap bitmap = Load(name);
            _bitmaps[name] = bitmap;

            return bitmap;
        }

        public bool TryMeasure(string source, out float width, out float height)
        {
            SKBitmap bitmap = Get(source);

            if (bitmap == null)
            {
                width = 0;
                height = 0;

                return false;
            }

            width = bitmap.Width;
            height = bitmap.Height;

            return true;
        }

        internal void Clear()
        {
            foreach (KeyValuePair<string, SKBitmap> entry in _bitmaps)
            {
                entry.Value?.Dispose();
            }

            _bitmaps.Clear();
        }

        private SKBitmap Load(string name)
        {
            IImageSource source = _source;

            if (source == null)
            {
                return null;
            }

            try
            {
                using (Stream stream = source.Open(name))
                {
                    return stream == null ? null : SKBitmap.Decode(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
