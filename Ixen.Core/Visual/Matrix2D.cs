using System;

namespace Ixen.Core.Visual
{
    internal readonly struct Matrix2D
    {
        internal static readonly Matrix2D Identity = new Matrix2D(1, 0, 0, 0, 1, 0);

        internal readonly float ScaleX;
        internal readonly float SkewX;
        internal readonly float TransX;
        internal readonly float SkewY;
        internal readonly float ScaleY;
        internal readonly float TransY;

        internal Matrix2D(float scaleX, float skewX, float transX, float skewY, float scaleY, float transY)
        {
            ScaleX = scaleX;
            SkewX = skewX;
            TransX = transX;
            SkewY = skewY;
            ScaleY = scaleY;
            TransY = transY;
        }

        internal bool IsIdentity
            => ScaleX == 1 && SkewX == 0 && TransX == 0
            && SkewY == 0 && ScaleY == 1 && TransY == 0;

        internal static Matrix2D Translation(float x, float y)
            => new Matrix2D(1, 0, x, 0, 1, y);

        internal static Matrix2D Scaling(float x, float y)
            => new Matrix2D(x, 0, 0, 0, y, 0);

        internal static Matrix2D Rotation(float degrees)
        {
            double radians = degrees * Math.PI / 180;
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new Matrix2D(cos, -sin, 0, sin, cos, 0);
        }

        internal static Matrix2D Skewing(float xDegrees, float yDegrees)
        {
            float x = (float)Math.Tan(xDegrees * Math.PI / 180);
            float y = (float)Math.Tan(yDegrees * Math.PI / 180);

            return new Matrix2D(1, x, 0, y, 1, 0);
        }

        internal static Matrix2D Concat(Matrix2D first, Matrix2D second)
            => new Matrix2D
            (
                first.ScaleX * second.ScaleX + first.SkewX * second.SkewY,
                first.ScaleX * second.SkewX + first.SkewX * second.ScaleY,
                first.ScaleX * second.TransX + first.SkewX * second.TransY + first.TransX,
                first.SkewY * second.ScaleX + first.ScaleY * second.SkewY,
                first.SkewY * second.SkewX + first.ScaleY * second.ScaleY,
                first.SkewY * second.TransX + first.ScaleY * second.TransY + first.TransY
            );

        internal void Map(float x, float y, out float mappedX, out float mappedY)
        {
            mappedX = ScaleX * x + SkewX * y + TransX;
            mappedY = SkewY * x + ScaleY * y + TransY;
        }

        internal bool TryInvert(out Matrix2D inverted)
        {
            float determinant = ScaleX * ScaleY - SkewX * SkewY;

            if (determinant == 0 || float.IsNaN(determinant) || float.IsInfinity(determinant))
            {
                inverted = Identity;
                return false;
            }

            float scaleX = ScaleY / determinant;
            float skewX = -SkewX / determinant;
            float skewY = -SkewY / determinant;
            float scaleY = ScaleX / determinant;

            inverted = new Matrix2D
            (
                scaleX,
                skewX,
                -(scaleX * TransX + skewX * TransY),
                skewY,
                scaleY,
                -(skewY * TransX + scaleY * TransY)
            );

            return true;
        }
    }
}
