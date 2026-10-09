using System;

namespace Ixen.Core.Visual.Shapes
{
    internal enum PathVerb
    {
        Move,
        Line,
        Quad,
        Cubic,
        Close
    }

    internal sealed class PathGeometry
    {
        internal PathVerb[] Verbs { get; }

        internal float[] Points { get; }

        internal bool EvenOdd { get; }

        internal float Left { get; }

        internal float Top { get; }

        internal float Width { get; }

        internal float Height { get; }

        internal object Native { get; set; }

        internal PathGeometry(PathVerb[] verbs, float[] points, bool evenOdd,
            float left, float top, float right, float bottom)
        {
            Verbs = verbs;
            Points = points;
            EvenOdd = evenOdd;
            Left = left;
            Top = top;
            Width = Math.Max(0f, right - left);
            Height = Math.Max(0f, bottom - top);
        }

        internal bool IsEmpty => Verbs.Length == 0;
    }
}
