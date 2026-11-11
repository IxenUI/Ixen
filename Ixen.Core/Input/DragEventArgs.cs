using Ixen.Core.Visual;

namespace Ixen.Core.Input
{
    public class DragEventArgs : PointerEventArgs
    {
        public const string GHOST = "DragGhost";
        public const string GHOST_BADGE = "DragGhostBadge";

        public float DeltaX { get; private set; }
        public float DeltaY { get; private set; }
        public float TotalX { get; private set; }
        public float TotalY { get; private set; }

        public VisualElement DragSource { get; private set; }
        public object Data { get; set; }
        public bool Accepted { get; set; } = true;

        public VisualElement Ghost { get; set; }
        public float GhostOffsetX { get; set; }
        public float GhostOffsetY { get; set; }
        public string GhostBadge { get; set; }

        internal VisualElement GhostSource { get; private set; }

        public void GhostOf(VisualElement element)
        {
            GhostSource = element;
            Ghost = null;

            if (element == null)
            {
                return;
            }

            GhostOffsetX = X - TotalX - element.X;
            GhostOffsetY = Y - TotalY - element.Y;
        }

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
