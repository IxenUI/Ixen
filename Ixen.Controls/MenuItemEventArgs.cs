using System;

namespace Ixen.Controls
{
    public class MenuItemEventArgs : EventArgs
    {
        public MenuItem Item { get; }

        public MenuItemEventArgs(MenuItem item)
        {
            Item = item;
        }
    }
}
