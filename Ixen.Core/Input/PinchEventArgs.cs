using Ixen.Core.Visual;

namespace Ixen.Core.Input
{
    public class PinchEventArgs : PointerEventArgs
    {
        public float Scale { get; private set; }
        public float Rotation { get; private set; }
        public float DeltaX { get; private set; }
        public float DeltaY { get; private set; }
        public float TotalX { get; private set; }
        public float TotalY { get; private set; }
        public int PointerCount { get; private set; }

        internal PinchEventArgs(float x, float y, VisualElement source, float scale, float rotation,
            float deltaX, float deltaY, float totalX, float totalY, int pointerCount,
            PointerKind kind = PointerKind.Touch)
            : base(x, y, PointerButton.None, source, kind)
        {
            Scale = scale;
            Rotation = rotation;
            DeltaX = deltaX;
            DeltaY = deltaY;
            TotalX = totalX;
            TotalY = totalY;
            PointerCount = pointerCount;
        }
    }
}
