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

        private sealed class Entry
        {
            internal SKBitmap Bitmap;
            internal SKPaint Tile;
            internal long Bytes;
            internal long Stamp;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

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

        internal SKBitmap Get(string name)
        {
            Entry entry = Touch(name);

            return entry?.Bitmap;
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
                    Stamp = ++_clock
                };

                _entries[name] = waiting;

                Start(asynchronous, name, waiting);

                return waiting;
            }

            SKBitmap bitmap = Load(name);

            var entry = new Entry
            {
                Bitmap = bitmap,
                Bytes = bitmap == null ? 0 : bitmap.ByteCount,
                Stamp = ++_clock
            };

            _entries[name] = entry;
            _bytes += entry.Bytes;

            return entry;
        }

        internal SKPaint GetTile(string name)
        {
            Entry entry = Touch(name);

            if (entry == null)
            {
                return null;
            }

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
            _bytes = 0;
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
