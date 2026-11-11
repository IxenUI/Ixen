using System;

namespace Ixen.Controls
{
    public class DataGridItemEventArgs : EventArgs
    {
        public object Item { get; }

        public int Index { get; }

        public DataGridItemEventArgs(object item, int index)
        {
            Item = item;
            Index = index;
        }
    }
}
