using Ixen.Core.Visual;

namespace Ixen.Core.Input
{
    public class DragEventArgs : PointerEventArgs
    {
        public float DeltaX { get; private set; }
        public float DeltaY { get; private set; }
        public float TotalX { get; private set; }
        public float TotalY { get; private set; }

        internal DragEventArgs(float x, float y, PointerButton button, VisualElement source,
            float deltaX, float deltaY, float totalX, float totalY,
            PointerKind kind = PointerKind.Mouse)
            : base(x, y, button, source, kind)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
            TotalX = totalX;
            TotalY = totalY;
        }
    }
}
