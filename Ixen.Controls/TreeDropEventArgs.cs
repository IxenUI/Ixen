using System;

namespace Ixen.Controls
{
    public class TreeDropEventArgs : EventArgs
    {
        public TreeDropEventArgs(object data, object item)
        {
            Data = data;
            Item = item;
        }

        public object Data { get; }

        public object Item { get; }
    }
}
