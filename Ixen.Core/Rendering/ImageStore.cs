using Ixen.Core.Visual;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Ixen.Core.Rendering
{
    internal class ImageStore : IImageMeasurer
    {
        private const long DEFAULT_BUDGET = 64 * 1024 * 1024;
        private const int MAX_DIVISOR = 8;

        private sealed class Entry
        {
            internal SKBitmap Bitmap;
            internal SKPaint Tile;
            internal long Bytes;
            internal long Stamp;
            internal int NaturalWidth;
            internal int NaturalHeight;
            internal int Divisor;
            internal bool Read;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        private readonly HashSet<string> _pending = new HashSet<string>();

        private IImageSource _source;
        private long _bytes;
        private long _clock;

        internal Action<Action> Poster { get; set; }
        internal Action Arrived { get; set; }

        internal long Budget { get; set; } = DEFAULT_BUDGET;

        internal long Bytes => _bytes;

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

        internal SKBitmap Get(string name) => Get(name, 0, 0);

        internal SKBitmap Get(string name, float width, float height)
        {
            Entry entry = Touch(name);

            if (entry == null)
            {
                return null;
            }

            Pixels(name, entry, width, height);

            return entry.Bitmap;
        }

        internal bool TryNatural(string name, out int width, out int height)
        {
            Entry entry = Touch(name);

            width = entry == null ? 0 : entry.NaturalWidth;
            height = entry == null ? 0 : entry.NaturalHeight;

            return width > 0 && height > 0;
        }

        private static int DivisorFor(Entry entry, float width, float height)
        {
            if (width <= 0 || height <= 0)
            {
                return 1;
            }

            for (int divisor = MAX_DIVISOR; divisor > 1; divisor /= 2)
            {
                if (entry.NaturalWidth / divisor >= width
                    && entry.NaturalHeight / divisor >= height)
                {
                    return divisor;
                }
            }

            return 1;
        }

        private void Pixels(string name, Entry entry, float width, float height)
        {
            if (entry.NaturalWidth <= 0 || _pending.Contains(name))
            {
                return;
            }

            int divisor = DivisorFor(entry, width, height);

            if (entry.Bitmap != null && entry.Divisor <= divisor)
            {
                return;
            }

            SKBitmap decoded = Load(name, divisor);

            if (decoded == null)
            {
                return;
            }

            _bytes -= entry.Bytes;

            entry.Tile?.Shader?.Dispose();
            entry.Tile?.Dispose();
            entry.Tile = null;
            entry.Bitmap?.Dispose();

            entry.Bitmap = decoded;
            entry.Divisor = divisor;
            entry.Bytes = decoded.ByteCount;

            _bytes += entry.Bytes;
        }

        private Entry Touch(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (_entries.TryGetValue(name, out Entry cached))
            {
                cached.Stamp = ++_clock;
                return cached;
            }

            if (_source is IAsyncImageSource asynchronous && Poster != null)
            {
                var waiting = new Entry
                {
                    Stamp = ++_clock,
                    Read = true
                };

                _entries[name] = waiting;
                _pending.Add(name);

                Start(asynchronous, name, waiting);

                return waiting;
            }

            var entry = new Entry
            {
                Stamp = ++_clock,
                Read = true
            };

            Natural(name, entry);

            _entries[name] = entry;

            return entry;
        }

        internal SKPaint GetTile(string name)
        {
            Entry entry = Touch(name);

            if (entry == null)
            {
                return null;
            }

            Pixels(name, entry, entry.NaturalWidth, entry.NaturalHeight);

            if (entry.Tile != null || entry.Bitmap == null)
            {
                return entry.Tile;
            }

            entry.Tile = new SKPaint
            {
                IsAntialias = false,
                Shader = entry.Bitmap.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat)
            };

            return entry.Tile;
        }

        public bool TryMeasure(string source, out float width, out float height)
        {
            bool known = TryNatural(source, out int natural, out int high);

            width = natural;
            height = high;

            return known;
        }

        internal void Trim()
        {
            while (_bytes > Budget)
            {
                string oldest = null;
                long stamp = long.MaxValue;

                foreach (KeyValuePair<string, Entry> candidate in _entries)
                {
                    if (candidate.Value.Bytes > 0 && candidate.Value.Stamp < stamp)
                    {
                        stamp = candidate.Value.Stamp;
                        oldest = candidate.Key;
                    }
                }

                if (oldest == null)
                {
                    return;
                }

                Evict(oldest);
            }
        }

        private void Evict(string name)
        {
            Entry entry = _entries[name];

            _entries.Remove(name);
            _bytes -= entry.Bytes;

            Release(entry);
        }

        private static void Release(Entry entry)
        {
            entry.Tile?.Shader?.Dispose();
            entry.Tile?.Dispose();
            entry.Bitmap?.Dispose();
        }

        private void Start(IAsyncImageSource source, string name, Entry waiting)
        {
            Task<Stream> opening;

            try
            {
                opening = source.OpenAsync(name);
            }
            catch
            {
                Settle(name, waiting, null);
                return;
            }

            if (opening == null)
            {
                Settle(name, waiting, null);
                return;
            }

            opening.ContinueWith(finished =>
            {
                SKBitmap decoded = Decode(finished);

                Poster(() => Settle(name, waiting, decoded));
            });
        }

        private static SKBitmap Decode(Task<Stream> finished)
        {
            if (finished.Status != TaskStatus.RanToCompletion || finished.Result == null)
            {
                return null;
            }

            try
            {
                using (Stream stream = finished.Result)
                {
                    return SKBitmap.Decode(stream);
                }
            }
            catch
            {
                return null;
            }
        }

        private void Settle(string name, Entry waiting, SKBitmap bitmap)
        {
            if (!_entries.TryGetValue(name, out Entry current) || current != waiting)
            {
                bitmap?.Dispose();
                return;
            }

            waiting.Bitmap = bitmap;
            waiting.Bytes = bitmap == null ? 0 : bitmap.ByteCount;
            waiting.Divisor = 1;
            waiting.NaturalWidth = bitmap == null ? 0 : bitmap.Width;
            waiting.NaturalHeight = bitmap == null ? 0 : bitmap.Height;

            _pending.Remove(name);

            _bytes += waiting.Bytes;

            Arrived?.Invoke();
        }

        internal void Clear()
        {
            foreach (KeyValuePair<string, Entry> entry in _entries)
            {
                Release(entry.Value);
            }

            _entries.Clear();
            _pending.Clear();
            _bytes = 0;
        }

        private SKBitmap Load(string name, int divisor)
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
                    if (stream == null)
                    {
                        return null;
                    }

                    if (divisor <= 1)
                    {
                        return SKBitmap.Decode(stream);
                    }

                    using (SKCodec codec = SKCodec.Create(stream))
                    {
                        if (codec == null)
                        {
                            return null;
                        }

                        SKSizeI wanted = codec.GetScaledDimensions(1f / divisor);
                        SKImageInfo info = codec.Info
                            .WithSize(wanted.Width, wanted.Height)
                            .WithColorType(SKImageInfo.PlatformColorType);

                        return SKBitmap.Decode(codec, info);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private void Natural(string name, Entry entry)
        {
            IImageSource source = _source;

            if (source == null)
            {
                return;
            }

            try
            {
                using (Stream stream = source.Open(name))
                {
                    if (stream == null)
                    {
                        return;
                    }

                    using (SKCodec codec = SKCodec.Create(stream))
                    {
                        if (codec == null)
                        {
                            return;
                        }

                        entry.NaturalWidth = codec.Info.Width;
                        entry.NaturalHeight = codec.Info.Height;
                    }
                }
            }
            catch
            {
            }
        }
    }
}
