using SkiaSharp;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal sealed class TextBlobCache
    {
        private const long DEFAULT_BUDGET = 2 * 1024 * 1024;
        private const long BLOB_OVERHEAD = 500;
        private const long BYTES_A_CHARACTER = 10;

        private sealed class Entry
        {
            internal SKTextBlob Blob;
            internal long Bytes;
            internal long Stamp;
        }

        private readonly Dictionary<(string, SKFont, float), Entry> _entries
            = new Dictionary<(string, SKFont, float), Entry>();

        private long _bytes;
        private long _clock;

        internal long Budget { get; set; } = DEFAULT_BUDGET;

        internal long Bytes => _bytes;

        internal int Count => _entries.Count;

        internal static long Estimate(string text)
        {
            return BLOB_OVERHEAD + (text == null ? 0 : text.Length * BYTES_A_CHARACTER);
        }

        internal bool TryGet(string text, SKFont font, float spacing, out SKTextBlob blob)
        {
            if (_entries.TryGetValue((text, font, spacing), out Entry cached))
            {
                cached.Stamp = ++_clock;
                blob = cached.Blob;

                return true;
            }

            blob = null;

            return false;
        }

        internal void Add(string text, SKFont font, float spacing, SKTextBlob blob)
        {
            if (blob == null)
            {
                return;
            }

            var entry = new Entry
            {
                Blob = blob,
                Bytes = Estimate(text),
                Stamp = ++_clock
            };

            _entries[(text, font, spacing)] = entry;
            _bytes += entry.Bytes;

            Trim();
        }

        private void Trim()
        {
            while (_bytes > Budget && _entries.Count > 1)
            {
                (string, SKFont, float) oldest = default;
                long stamp = long.MaxValue;
                bool found = false;

                foreach (KeyValuePair<(string, SKFont, float), Entry> candidate in _entries)
                {
                    if (candidate.Value.Stamp < stamp)
                    {
                        stamp = candidate.Value.Stamp;
                        oldest = candidate.Key;
                        found = true;
                    }
                }

                if (!found)
                {
                    return;
                }

                Evict(oldest);
            }
        }

        private void Evict((string, SKFont, float) key)
        {
            if (!_entries.TryGetValue(key, out Entry entry))
            {
                return;
            }

            _entries.Remove(key);
            _bytes -= entry.Bytes;
            entry.Blob?.Dispose();
        }

        internal bool Holds(string text)
        {
            foreach (KeyValuePair<(string, SKFont, float), Entry> entry in _entries)
            {
                if (entry.Key.Item1 == text)
                {
                    return true;
                }
            }

            return false;
        }

        internal void Clear()
        {
            foreach (KeyValuePair<(string, SKFont, float), Entry> entry in _entries)
            {
                entry.Value.Blob?.Dispose();
            }

            _entries.Clear();
            _bytes = 0;
        }
    }
}
