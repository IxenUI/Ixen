using System;
using System.Collections;

namespace Ixen.Controls
{
    public class TreeMoveEventArgs : EventArgs
    {
        public TreeMoveEventArgs(object item, object fromParent, IList fromList, int fromIndex,
            object toParent, IList toList, int toIndex)
        {
            Item = item;
            FromParent = fromParent;
            FromList = fromList;
            FromIndex = fromIndex;
            ToParent = toParent;
            ToList = toList;
            ToIndex = toIndex;
            Total = toList.Count + (ChangesParent ? 1 : 0);
        }

        public int Total { get; }

        public object Item { get; }

        public object FromParent { get; }

        public IList FromList { get; }

        public int FromIndex { get; }

        public object ToParent { get; }

        public IList ToList { get; }

        public int ToIndex { get; }

        public bool ChangesParent => !ReferenceEquals(FromList, ToList);

        public void Apply()
        {
            FromList.RemoveAt(FromIndex);
            ToList.Insert(ToIndex, Item);
        }

        public string Describe()
            => "Moved to position " + (ToIndex + 1) + " of " + Total;
    }
}
