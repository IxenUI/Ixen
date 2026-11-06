using System;

namespace Ixen.Controls
{
    public class ListDropEventArgs : EventArgs
    {
        public ListDropEventArgs(object data, int index)
        {
            Data = data;
            Index = index;
        }

        public object Data { get; }

        public int Index { get; }
    }
}
