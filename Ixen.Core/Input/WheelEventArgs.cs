using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Input
{
    public class WheelEventArgs : EventArgs
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public float DeltaX { get; private set; }
        public float DeltaY { get; private set; }
        public VisualElement Source { get; private set; }

        public bool Handled { get; set; }

        internal WheelEventArgs(float x, float y, float deltaX, float deltaY, VisualElement source)
        {
            X = x;
            Y = y;
            DeltaX = deltaX;
            DeltaY = deltaY;
            Source = source;
        }
    }
}
