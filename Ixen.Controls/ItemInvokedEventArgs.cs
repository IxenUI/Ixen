using System;

namespace Ixen.Controls
{
    public class ItemInvokedEventArgs : EventArgs
    {
        public object Item { get; }

        public int Index { get; }

        public ItemInvokedEventArgs(object item, int index)
        {
            Item = item;
            Index = index;
        }
    }
}
