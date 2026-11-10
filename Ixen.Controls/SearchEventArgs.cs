using System;

namespace Ixen.Controls
{
    public class SearchEventArgs : EventArgs
    {
        public SearchEventArgs(string text, int index)
        {
            Text = text;
            Index = index;
        }

        public string Text { get; }

        public int Index { get; }
    }
}
