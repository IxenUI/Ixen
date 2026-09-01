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

            float top = target.Y + target.PaddingTop + target.BorderInsideTop;
            float above = element.Y - top;
            float below = element.Y + element.ActualHeight - (top + target.ContentHeight);

            float offsetY = above < 0 ? above : below > 0 ? below : 0;

            if (offsetY == 0)
            {
                return false;
            }

            target.ScrollBy(0, offsetY);

            return true;
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
