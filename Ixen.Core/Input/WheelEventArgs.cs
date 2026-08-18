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
        public KeyModifiers Modifiers { get; private set; }
        public VisualElement Source { get; private set; }

        public bool Handled { get; set; }

        public bool HasModifier(KeyModifiers modifier) => (Modifiers & modifier) == modifier;

        internal WheelEventArgs(float x, float y, float deltaX, float deltaY,
            KeyModifiers modifiers, VisualElement source)
        {
            X = x;
            Y = y;
            DeltaX = deltaX;
            DeltaY = deltaY;
            Modifiers = modifiers;
            Source = source;
        }
    }
}
