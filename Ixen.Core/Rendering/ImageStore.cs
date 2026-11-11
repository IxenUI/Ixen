using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
            internal SvgDocument Vector;
            internal int RasterWidth;
            internal byte[] Encoded;
            internal volatile float WantedWidth;
            internal volatile float WantedHeight;
            internal int Needed = NOT_DRAWN;
            internal long WantedAt;
            internal int Attempt;
            internal bool Dormant;
            internal CancellationTokenSource Cancellation;
        }

        private const int NOT_DRAWN = int.MaxValue;

        private sealed class Arrival
        {
            internal byte[] Encoded;
            internal int Width;
            internal int Height;
            internal SvgDocument Vector;
            internal SKBitmap Bitmap;
            internal int Divisor;
        }

        private long _trimmedAt;

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        private readonly HashSet<string> _pending = new HashSet<string>();

        private IImageSource _source;
        private long _bytes;
        private long _clock;

        internal Action<Action> Poster { get; set; }
        internal Action Arrived { get; set; }

        internal long Budget { get; set; } = DEFAULT_BUDGET;

        internal long Bytes => _bytes;

        internal int Pending => _pending.Count;

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

        internal void Want(string name, float width, float height)
        {
            if (name != null && _pending.Contains(name) && _entries.TryGetValue(name, out Entry entry))
            {
                entry.WantedWidth = width;
                entry.WantedHeight = height;

                MarkWanted(name, entry);
            }
        }

        internal ImageStatus StatusOf(string name)
        {
            Entry entry = Touch(name);

            if (entry == null)
            {
                return ImageStatus.Failed;
            }

            if (_pending.Contains(name))
            {
                return ImageStatus.Loading;
            }

            return entry.NaturalWidth > 0 && entry.NaturalHeight > 0 ? ImageStatus.Ready : ImageStatus.Failed;
        }

        internal bool TryNatural(string name, out int width, out int height)
        {
            Entry entry = Touch(name);

            width = entry == null ? 0 : entry.NaturalWidth;
            height = entry == null ? 0 : entry.NaturalHeight;

            return width > 0 && height > 0;
        }

        private static int DivisorFor(Entry entry, float width, float height)
            => DivisorFor(entry.NaturalWidth, entry.NaturalHeight, width, height);

        private static int DivisorFor(int naturalWidth, int naturalHeight, float width, float height)
        {
            if (width <= 0 || height <= 0)
            {
                return 1;
            }

            for (int divisor = MAX_DIVISOR; divisor > 1; divisor /= 2)
            {
                if (naturalWidth / divisor >= width
                    && naturalHeight / divisor >= height)
                {
                    return divisor;
                }
            }

            return 1;
        }

        private static long EncodedBytes(Entry entry) => entry.Encoded == null ? 0 : entry.Encoded.Length;

        private void Pixels(string name, Entry entry, float width, float height)
        {
            if (_pending.Contains(name))
            {
                entry.WantedWidth = width;
                entry.WantedHeight = height;

                MarkWanted(name, entry);

                return;
            }

            if (entry.NaturalWidth <= 0)
            {
                return;
            }

            if (entry.Vector != null)
            {
                Rasterise(entry, width, height);

                return;
            }

            int divisor = DivisorFor(entry, width, height);

            if (divisor < entry.Needed)
            {
                entry.Needed = divisor;
            }

            if (entry.Bitmap != null && entry.Divisor <= divisor)
            {
                return;
            }

            SKBitmap decoded = entry.Encoded != null
                ? DecodeAt(entry.Encoded, divisor)
                : Load(name, divisor);

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
            entry.Bytes = EncodedBytes(entry) + decoded.ByteCount;

            _bytes += entry.Bytes;
        }

        private void Rasterise(Entry entry, float width, float height)
        {
            int wanted = width <= 0 ? entry.NaturalWidth : (int)Math.Ceiling(width);

            if (wanted < 1)
            {
                wanted = 1;
            }

            if (wanted > SvgRasteriser.MAX_SIDE)
            {
                wanted = SvgRasteriser.MAX_SIDE;
            }

            if (entry.Bitmap != null && entry.RasterWidth >= wanted)
            {
                return;
            }

            int tall = (int)Math.Ceiling(wanted * (double)entry.NaturalHeight / entry.NaturalWidth);

            SKBitmap drawn = SvgRasteriser.Render(entry.Vector, wanted, tall < 1 ? 1 : tall);

            if (drawn == null)
            {
                return;
            }

            _bytes -= entry.Bytes;

            entry.Tile?.Shader?.Dispose();
            entry.Tile?.Dispose();
            entry.Tile = null;
            entry.Bitmap?.Dispose();

            entry.Bitmap = drawn;
            entry.RasterWidth = wanted;
            entry.Bytes = drawn.ByteCount;

            _bytes += entry.Bytes;
        }

        internal void Keep(string name)
        {
            if (_entries.TryGetValue(name, out Entry cached))
            {
                cached.Stamp = ++_clock;
            }
        }

        internal void Wanted(string name)
        {
            if (_pending.Contains(name) && _entries.TryGetValue(name, out Entry waiting))
            {
                MarkWanted(name, waiting);
            }
        }

        private void MarkWanted(string name, Entry entry)
        {
            entry.WantedAt = ++_clock;

            if (entry.Dormant && _source is IAsyncImageSource asynchronous)
            {
                entry.Dormant = false;
                Start(asynchronous, name, entry);
            }
        }

        private void PutToSleep(long shown)
        {
            foreach (KeyValuePair<string, Entry> candidate in _entries)
            {
                Entry entry = candidate.Value;

                if (entry.Dormant || entry.WantedAt > shown || !_pending.Contains(candidate.Key))
                {
                    continue;
                }

                entry.Dormant = true;
                entry.Cancellation?.Cancel();
            }
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
            long shown = _trimmedAt;

            _trimmedAt = _clock;

            if (_pending.Count > 0)
            {
                PutToSleep(shown);
            }

            for (int pass = 0; pass < 3 && _bytes > Budget; pass++)
            {
                while (_bytes > Budget)
                {
                    string chosen = pass == 1 ? Oversized() : Oldest(shown, pass == 0);

                    if (chosen == null)
                    {
                        break;
                    }

                    Evict(chosen, pass < 2);
                }
            }

            foreach (KeyValuePair<string, Entry> entry in _entries)
            {
                entry.Value.Needed = NOT_DRAWN;
            }
        }

        private string Oversized()
        {
            string largest = null;
            long bytes = 0;
            bool rereadable = !(_source is IAsyncImageSource);

            foreach (KeyValuePair<string, Entry> candidate in _entries)
            {
                Entry entry = candidate.Value;

                if (entry.Bitmap == null || entry.Vector != null || entry.Needed == NOT_DRAWN
                    || entry.Divisor >= entry.Needed || (entry.Encoded == null && !rereadable))
                {
                    continue;
                }

                if (entry.Bitmap.ByteCount > bytes)
                {
                    bytes = entry.Bitmap.ByteCount;
                    largest = candidate.Key;
                }
            }

            return largest;
        }

        private string Oldest(long shown, bool pixelsOnly)
        {
            string oldest = null;
            long stamp = long.MaxValue;

            foreach (KeyValuePair<string, Entry> candidate in _entries)
            {
                Entry entry = candidate.Value;
                bool evictable = pixelsOnly ? entry.Encoded != null && entry.Bitmap != null : entry.Bytes > 0;

                if (evictable && entry.Stamp <= shown && entry.Stamp < stamp)
                {
                    stamp = entry.Stamp;
                    oldest = candidate.Key;
                }
            }

            return oldest;
        }

        private void Evict(string name, bool pixelsOnly)
        {
            Entry entry = _entries[name];

            if (pixelsOnly)
            {
                _bytes -= entry.Bitmap.ByteCount;
                entry.Bytes -= entry.Bitmap.ByteCount;

                entry.Tile?.Shader?.Dispose();
                entry.Tile?.Dispose();
                entry.Tile = null;
                entry.Bitmap.Dispose();
                entry.Bitmap = null;
                entry.Divisor = 0;

                return;
            }

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
            int attempt = ++waiting.Attempt;
            var cancellation = new CancellationTokenSource();

            waiting.Cancellation = cancellation;

            try
            {
                opening = source.OpenAsync(name, cancellation.Token);
            }
            catch
            {
                Settle(name, waiting, attempt, null);
                return;
            }

            if (opening == null)
            {
                Settle(name, waiting, attempt, null);
                return;
            }

            opening.ContinueWith(finished =>
            {
                Arrival arrival = Read(finished, waiting.WantedWidth, waiting.WantedHeight);

                Poster(() => Settle(name, waiting, attempt, arrival));
            });
        }

        private static Arrival Read(Task<Stream> finished, float wantedWidth, float wantedHeight)
        {
            if (finished.IsFaulted)
            {
                GC.KeepAlive(finished.Exception);
            }

            if (finished.Status != TaskStatus.RanToCompletion || finished.Result == null)
            {
                return null;
            }

            try
            {
                byte[] encoded;

                using (Stream stream = finished.Result)
                using (var buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    encoded = buffer.ToArray();
                }

                using (var header = new MemoryStream(encoded))
                using (SKCodec codec = SKCodec.Create(header))
                {
                    if (codec != null)
                    {
                        var arrival = new Arrival
                        {
                            Encoded = encoded,
                            Width = codec.Info.Width,
                            Height = codec.Info.Height
                        };

                        if (wantedWidth > 0 && wantedHeight > 0)
                        {
                            arrival.Divisor = DivisorFor(arrival.Width, arrival.Height, wantedWidth, wantedHeight);
                            arrival.Bitmap = DecodeAt(encoded, arrival.Divisor);
                        }

                        return arrival;
                    }
                }

                using (var vector = new MemoryStream(encoded))
                {
                    SvgDocument document = SvgDocument.Parse(vector);

                    return document == null
                        ? null
                        : new Arrival
                        {
                            Vector = document,
                            Width = (int)Math.Ceiling(document.Width),
                            Height = (int)Math.Ceiling(document.Height)
                        };
                }
            }
            catch
            {
                return null;
            }
        }

        private void Settle(string name, Entry waiting, int attempt, Arrival arrival)
        {
            bool stale = arrival == null && (attempt != waiting.Attempt || waiting.Dormant);

            if (!_entries.TryGetValue(name, out Entry current) || current != waiting || !_pending.Contains(name) || stale)
            {
                arrival?.Bitmap?.Dispose();
                return;
            }

            if (attempt != waiting.Attempt)
            {
                waiting.Cancellation?.Cancel();
            }

            waiting.Dormant = false;
            waiting.Cancellation = null;
            _pending.Remove(name);

            if (arrival != null)
            {
                waiting.Encoded = arrival.Encoded;
                waiting.Vector = arrival.Vector;
                waiting.NaturalWidth = arrival.Width;
                waiting.NaturalHeight = arrival.Height;
                waiting.Bitmap = arrival.Bitmap;
                waiting.Divisor = arrival.Bitmap == null ? 0 : arrival.Divisor;
                waiting.Bytes = (arrival.Encoded == null ? 0 : arrival.Encoded.Length)
                    + (arrival.Bitmap == null ? 0 : arrival.Bitmap.ByteCount);

                _bytes += waiting.Bytes;
            }

            Arrived?.Invoke();
        }

        private static SKBitmap DecodeAt(byte[] encoded, int divisor)
        {
            using (var stream = new MemoryStream(encoded))
            {
                return DecodeAt(stream, divisor);
            }
        }

        private static SKBitmap DecodeAt(Stream stream, int divisor)
        {
            try
            {
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

                    SKSizeI natural = codec.Info.Size;
                    SKSizeI wanted = codec.GetScaledDimensions(1f / divisor);
                    SKImageInfo info = codec.Info
                        .WithSize(wanted.Width, wanted.Height)
                        .WithColorType(SKImageInfo.PlatformColorType);

                    SKBitmap decoded = SKBitmap.Decode(codec, info);
                    int width = natural.Width / divisor;
                    int height = natural.Height / divisor;

                    if (decoded == null || decoded.Width <= width || width < 1 || height < 1)
                    {
                        return decoded;
                    }

                    using (decoded)
                    {
                        return decoded.Resize(decoded.Info.WithSize(width, height),
                            new SKSamplingOptions(SKCubicResampler.Mitchell));
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        internal void Clear()
        {
            foreach (KeyValuePair<string, Entry> entry in _entries)
            {
                entry.Value.Cancellation?.Cancel();
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
                    return stream == null ? null : DecodeAt(stream, divisor);
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
                        if (codec != null)
                        {
                            entry.NaturalWidth = codec.Info.Width;
                            entry.NaturalHeight = codec.Info.Height;

                            return;
                        }
                    }
                }

                Vector(source, name, entry);
            }
            catch
            {
            }
        }

        private static void Vector(IImageSource source, string name, Entry entry)
        {
            using (Stream stream = source.Open(name))
            {
                SvgDocument document = SvgDocument.Parse(stream);

                if (document == null)
                {
                    return;
                }

                entry.Vector = document;
                entry.NaturalWidth = (int)Math.Ceiling(document.Width);
                entry.NaturalHeight = (int)Math.Ceiling(document.Height);
            }
        }
    }
}
