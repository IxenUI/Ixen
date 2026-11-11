using System;
using System.Collections;
using System.Collections.Generic;

namespace Ixen.Controls
{
    public class TreeMoveEventArgs : EventArgs
    {
        private readonly object[] _items;
        private readonly object[] _fromParents;
        private readonly IList[] _fromLists;
        private readonly int[] _fromIndices;

        internal TreeMoveEventArgs(object[] items, object[] fromParents, IList[] fromLists, int[] fromIndices,
            object toParent, IList toList, int toIndex)
        {
            _items = items;
            _fromParents = fromParents;
            _fromLists = fromLists;
            _fromIndices = fromIndices;

            ToParent = toParent;
            ToList = toList;
            ToIndex = toIndex;

            int arriving = 0;

            foreach (IList list in fromLists)
            {
                if (!ReferenceEquals(list, toList))
                {
                    arriving++;
                }
            }

            Total = toList.Count + arriving;
        }

        public IReadOnlyList<object> Items => _items;

        public int Count => _items.Length;

        public object Item => _items[0];

        public object FromParent => _fromParents[0];

        public IList FromList => _fromLists[0];

        public int FromIndex => _fromIndices[0];

        public IReadOnlyList<object> FromParents => _fromParents;

        public IReadOnlyList<IList> FromLists => _fromLists;

        public IReadOnlyList<int> FromIndices => _fromIndices;

        public object ToParent { get; }

        public IList ToList { get; }

        public int ToIndex { get; }

        public int Total { get; }

        public bool ChangesParent
        {
            get
            {
                foreach (IList list in _fromLists)
                {
                    if (!ReferenceEquals(list, ToList))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public void Apply()
        {
            for (int i = 0; i < _items.Length; i++)
            {
                IList list = _fromLists[i];

                for (int at = 0; at < list.Count; at++)
                {
                    if (ReferenceEquals(list[at], _items[i]))
                    {
                        list.RemoveAt(at);
                        break;
                    }
                }
            }

            for (int i = 0; i < _items.Length; i++)
            {
                ToList.Insert(ToIndex + i, _items[i]);
            }
        }

        public string Describe()
        {
            if (Count == 1)
            {
                return "Moved to position " + (ToIndex + 1) + " of " + Total;
            }

            return Count + " items moved to positions " + (ToIndex + 1) + " to " + (ToIndex + Count) + " of " + Total;
        }
    }
}
