using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    internal static class HitTester
    {
        internal static VisualElement HitTest(VisualElement element, float x, float y)
        {
            if (element == null || element.IsVoidOrInvalid)
            {
                return null;
            }

            if (x < element.X
                || y < element.Y
                || x >= element.X + element.ActualWidth
                || y >= element.Y + element.ActualHeight)
            {
                return null;
            }

            if (element.StylesHandlers != null)
            {
                CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;

                if (radius != null && radius.HasRadius && !IsInsideRoundedShape(element, radius, x, y))
                {
                    return null;
                }
            }

            for (int i = element.Children.Count - 1; i >= 0; i--)
            {
                VisualElement hit = HitTest(element.Children[i], x, y);

                if (hit != null)
                {
                    return hit;
                }
            }

            return element;
        }

        private static bool IsInsideRoundedShape(VisualElement element, CornerRadiusStyleDescriptor radius,
            float x, float y)
        {
            float left = element.X;
            float top = element.Y;
            float right = left + element.ActualWidth;
            float bottom = top + element.ActualHeight;

            return IsInsideCorner(radius.TopLeft, left + radius.TopLeft - x, top + radius.TopLeft - y)
                && IsInsideCorner(radius.TopRight, x - (right - radius.TopRight), top + radius.TopRight - y)
                && IsInsideCorner(radius.BottomRight, x - (right - radius.BottomRight), y - (bottom - radius.BottomRight))
                && IsInsideCorner(radius.BottomLeft, left + radius.BottomLeft - x, y - (bottom - radius.BottomLeft));
        }

        private static bool IsInsideCorner(float radius, float dx, float dy)
        {
            if (radius <= 0 || dx <= 0 || dy <= 0)
            {
                return true;
            }

            dx /= radius;
            dy /= radius;

            return dx * dx + dy * dy <= 1;
        }
    }
}
