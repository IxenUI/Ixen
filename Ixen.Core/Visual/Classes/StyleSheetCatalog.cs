using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    public static class StyleSheetCatalog
    {
        private static readonly object _gate = new object();
        private static readonly List<Entry> _entries = new List<Entry>();

        internal sealed class Entry
        {
            internal Entry(Type type, Func<StyleSheet> create, bool defaults)
            {
                Type = type;
                Create = create;
                Defaults = defaults;
            }

            internal Type Type { get; }
            internal Func<StyleSheet> Create { get; }
            internal bool Defaults { get; }
        }

        public static void Register(Type type, Func<StyleSheet> create, bool defaults)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (create == null)
            {
                throw new ArgumentNullException(nameof(create));
            }

            var entry = new Entry(type, create, defaults);

            lock (_gate)
            {
                for (int index = 0; index < _entries.Count; index++)
                {
                    if (_entries[index].Type == type)
                    {
                        return;
                    }
                }

                _entries.Add(entry);
            }

            StyleRegistry.SheetRegistered(entry);
        }

        internal static Entry[] Snapshot()
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }
}
