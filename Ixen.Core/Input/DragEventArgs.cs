using Ixen.Core.Visual;

namespace Ixen.Core.Input
{
    public class DragEventArgs : PointerEventArgs
    {
        public float DeltaX { get; private set; }
        public float DeltaY { get; private set; }
        public float TotalX { get; private set; }
        public float TotalY { get; private set; }

        public VisualElement DragSource { get; private set; }
        public object Data { get; set; }
        public bool Accepted { get; set; } = true;

        internal DragEventArgs(float x, float y, PointerButton button, VisualElement source,
            float deltaX, float deltaY, float totalX, float totalY,
            PointerKind kind = PointerKind.Mouse)
            : base(x, y, button, source, kind)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
            TotalX = totalX;
            TotalY = totalY;
            DragSource = source;
        }

        internal DragEventArgs(float x, float y, PointerButton button, VisualElement target,
            VisualElement dragSource, object data, bool accepted, PointerKind kind)
            : base(x, y, button, target, kind)
        {
            DragSource = dragSource;
            Data = data;
            Accepted = accepted;
        }
    }
}
