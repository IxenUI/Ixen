using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Input
{
    public class PointerEventArgs : EventArgs
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public PointerButton Button { get; private set; }
        public VisualElement Source { get; private set; }

        public bool Handled { get; set; }

        internal PointerEventArgs(float x, float y, PointerButton button, VisualElement source)
        {
            X = x;
            Y = y;
            Button = button;
            Source = source;
        }
    }
}
