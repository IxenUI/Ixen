using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Input
{
    internal static class ScrollNavigator
    {
        internal const float STEP = 48f;

        internal static VisualElement Find(VisualElement from, float offsetX, float offsetY)
            => Find(from, offsetX, offsetY, out _);

        internal static VisualElement Find(VisualElement from, float offsetX, float offsetY,
            out bool contained)
        {
            contained = false;

            for (VisualElement element = from; element != null; element = element.Parent)
            {
                if (!element.Scrollable)
                {
                    continue;
                }

                if (CanScroll(element, offsetX, offsetY))
                {
                    return element;
                }

                if (Contains(element, Horizontal(offsetX, offsetY)))
                {
                    contained = true;

                    return null;
                }
            }

            return null;
        }

        internal static bool Horizontal(float offsetX, float offsetY)
            => Math.Abs(offsetX) > Math.Abs(offsetY);

        internal static VisualElement Bouncer(VisualElement from, float offsetX, float offsetY)
        {
            bool horizontal = Horizontal(offsetX, offsetY);

            for (VisualElement element = from; element != null; element = element.Parent)
            {
                if (!element.Scrollable)
                {
                    continue;
                }

                if (CanBounce(element, horizontal))
                {
                    return element;
                }

                if (Contains(element, horizontal))
                {
                    return null;
                }
            }

            return null;
        }

        internal static bool CanBounce(VisualElement element, bool horizontal)
            => Bounces(element, horizontal) && Overflows(element, horizontal);

        private static bool Overflows(VisualElement element, bool horizontal)
            => horizontal ? element.MaxScrollX > 0f : element.MaxScrollY > 0f;

        private static bool Bounces(VisualElement element, bool horizontal)
            => element.StylesHandlers == null
                || element.StylesHandlers.Overscroll.Descriptor.Bounces(horizontal);

        private static bool Contains(VisualElement element, bool horizontal)
            => element.StylesHandlers != null
                && element.StylesHandlers.Overscroll.Descriptor.Contains(horizontal);

        internal static VisualElement FindDefault(VisualElement root, float offsetX, float offsetY)
        {
            if (root == null)
            {
                return null;
            }

            if (root.Scrollable && CanScroll(root, offsetX, offsetY))
            {
                return root;
            }

            foreach (VisualElement child in root.Children)
            {
                VisualElement found = FindDefault(child, offsetX, offsetY);

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        internal static bool IntoView(VisualElement element)
        {
            VisualElement target = Scrollable(element?.Parent);

            if (target == null)
            {
                return false;
            }

            float left = target.X + target.PaddingLeft + target.BorderInsideLeft;
            float top = target.Y + target.PaddingTop + target.BorderInsideTop;

            float offsetX = Nearest(element.X - left,
                element.X + element.ActualWidth - (left + target.ContentWidth));
            float offsetY = Nearest(element.Y - top,
                element.Y + element.ActualHeight - (top + target.ContentHeight));

            if (offsetX == 0 && offsetY == 0)
            {
                return false;
            }

            target.RequestScroll(offsetX, offsetY);

            return true;
        }

        private static float Nearest(float before, float after)
        {
            if (before < 0)
            {
                return before;
            }

            if (after > 0)
            {
                return after < before ? after : before;
            }

            return 0;
        }

        private static float Within(float value, float max)
        {
            if (value > max)
            {
                value = max;
            }

            return value < 0 ? 0 : value;
        }

        internal static bool Reveal(VisualElement element, bool alignToTop)
        {
            float x = element.X;
            float y = element.Y;
            float width = element.ActualWidth;
            float height = element.ActualHeight;
            bool moved = false;

            for (VisualElement target = Scrollable(element.Parent); target != null; target = Scrollable(target.Parent))
            {
                float left = target.X + target.PaddingLeft + target.BorderInsideLeft;
                float top = target.Y + target.PaddingTop + target.BorderInsideTop;
                float above = y - top;

                float wantedX = Nearest(x - left, x + width - (left + target.ContentWidth));
                float wantedY = alignToTop ? above : Nearest(above, y + height - (top + target.ContentHeight));

                float nextX = Within(target.ScrollX + wantedX, target.MaxScrollX);
                float nextY = Within(target.ScrollY + wantedY, target.MaxScrollY);

                float appliedX = nextX - target.ScrollX;
                float appliedY = nextY - target.ScrollY;

                if (appliedX == 0 && appliedY == 0)
                {
                    continue;
                }

                if (target.ScrollsSmoothly)
                {
                    target.ScrollTo(nextX, nextY);
                }
                else
                {
                    target.ScrollX = nextX;
                    target.ScrollY = nextY;
                    moved = true;
                }

                x -= appliedX;
                y -= appliedY;
            }

            return moved;
        }

        private static VisualElement Scrollable(VisualElement from)
        {
            for (VisualElement element = from; element != null; element = element.Parent)
            {
                if (element.Scrollable)
                {
                    return element;
                }
            }

            return null;
        }

        internal static bool CanScroll(VisualElement element, float offsetX, float offsetY)
            => CanScrollAxis(element.ScrollX, element.MaxScrollX, offsetX)
                || CanScrollAxis(element.ScrollY, element.MaxScrollY, offsetY);

        private static bool CanScrollAxis(float offset, float max, float delta)
        {
            if (delta < 0)
            {
                return offset > 0;
            }

            return delta > 0 && offset < max;
        }
    }
}
