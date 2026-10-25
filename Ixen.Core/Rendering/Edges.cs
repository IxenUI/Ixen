namespace Ixen.Core.Rendering
{
    internal struct Edges
    {
        internal float Top;
        internal float Right;
        internal float Bottom;
        internal float Left;

        internal Edges(float top, float right, float bottom, float left)
        {
            Top = top;
            Right = right;
            Bottom = bottom;
            Left = left;
        }

        internal static Edges All(float value) => new Edges(value, value, value, value);

        internal Edges Negated => new Edges(-Top, -Right, -Bottom, -Left);

        internal bool IsZero => Top == 0 && Right == 0 && Bottom == 0 && Left == 0;
    }
}
