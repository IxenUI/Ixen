using SkiaSharp;
using System;

namespace Ixen.Core.Visual
{
    public class DimensionalElement
    {
        private float _x;
        private float _y;
        private float _width;
        private float _height;

        internal virtual float X
        {
            get => _x;
            set => _x = value;
        }

        internal virtual float Y
        {
            get => _y;
            set => _y = value;
        }

        internal float Width
        {
            get => _width;
            set => _width = value < 0 ? 0 : value;
        }

        internal float Height
        {
            get => _height;
            set => _height = value < 0 ? 0 : value;
        }

        internal virtual float ActualWidth
            => Width;

        internal virtual float ActualHeight
            => Height;

        internal virtual void SetSize(float width, float height)
        {
            Width = width;
            Height = height;
        }

        internal virtual void SetPosition(float x, float y)
        {
            X = x;
            Y = y;
        }

        internal bool IsIntersect(DimensionalElement element)
        {
            if (X < element.X + element.ActualWidth
                && X + ActualWidth > element.X
                && Y < element.Y + element.ActualHeight)
            {
                return Y + ActualHeight > element.Y;
            }

            return false;
        }

        internal bool IsIntersectInclusive(DimensionalElement element)
        {
            if (X <= element.X + element.ActualWidth
                && X + ActualWidth >= element.X
                && Y <= element.Y + element.ActualHeight)
            {
                return Y + ActualHeight >= element.Y;
            }

            return false;
        }

        internal DimensionalElement Intersect(DimensionalElement element)
        {
            if (!IsIntersectInclusive(element))
            {
                return new DimensionalElement();
            }

            var res = new DimensionalElement();
            res.X = Math.Max(X, element.X);
            res.Y = Math.Max(Y, element.Y);
            res.Width = Math.Min(X + ActualWidth, element.X + element.ActualWidth) - res.X;
            res.Height = Math.Min(Y + ActualHeight, element.Y + element.ActualHeight) - res.Y;

            return res;
        }
    }
}
