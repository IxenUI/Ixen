using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Input
{
    public class TextInputEventArgs : EventArgs
    {
        public string Text { get; private set; }
        public VisualElement Source { get; private set; }

        public bool Handled { get; set; }

        internal TextInputEventArgs(string text, VisualElement source)
        {
            Text = text;
            Source = source;
        }
    }
}
