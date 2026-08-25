using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    internal static class HitTester
    {
        internal static VisualElement HitTest(VisualElement root, float x, float y)
        {
            if (root != null && root.HasOverlays)
            {
                System.Collections.Generic.List<VisualElement> overlays = root.Overlays;

                for (int index = overlays.Count - 1; index >= 0; index--)
                {
                    VisualElement hit = TestOverlay(overlays[index], x, y);

                    if (hit != null)
                    {
                        return hit;
                    }
                }
            }

            return Test(root, x, y);
        }

        private static VisualElement TestOverlay(VisualElement layer, float x, float y)
        {
            if (layer == null)
            {
                return null;
            }

            float localX = x;
            float localY = y;

            if (layer.HasTransform)
            {
                if (!Transforms.Of(layer).TryInvert(out Matrix2D inverse))
                {
                    return null;
                }

                inverse.Map(x, y, out localX, out localY);
            }

            if (layer.HasChrome)
            {
                for (int i = layer.Chrome.Count - 1; i >= 0; i--)
                {
                    VisualElement chrome = Test(layer.Chrome[i], localX, localY);

                    if (chrome != null)
                    {
                        return chrome;
                    }
                }
            }

            for (int i = layer.Children.Count - 1; i >= 0; i--)
            {
                VisualElement hit = Test(layer.Children[i], localX, localY);

                if (hit != null)
                {
                    return hit;
                }
            }

            return Test(layer, x, y);
        }

        private static VisualElement Test(VisualElement element, float x, float y)
        {
            if (element == null || element.IsVoidOrInvalid)
            {
                return null;
            }

            if (element.HasTransform)
            {
                if (!Transforms.Of(element).TryInvert(out Matrix2D inverse))
                {
                    return null;
                }

                inverse.Map(x, y, out x, out y);
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

            if (element.HasChrome)
            {
                for (int i = element.Chrome.Count - 1; i >= 0; i--)
                {
                    VisualElement chrome = Test(element.Chrome[i], x, y);

                    if (chrome != null)
                    {
                        return chrome;
                    }
                }
            }

            for (int i = element.Children.Count - 1; i >= 0; i--)
            {
                if (element.Children[i].IsOverlay)
                {
                    continue;
                }

                VisualElement hit = Test(element.Children[i], x, y);

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
