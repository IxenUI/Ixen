using Ixen.Core.Visual;

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

                if (Contains(element))
                {
                    contained = true;

                    return null;
                }
            }

            return null;
        }

        private static bool Contains(VisualElement element)
            => element.StylesHandlers != null
                && element.StylesHandlers.Overscroll.Descriptor.Contains;

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

        private static bool CanScroll(VisualElement element, float offsetX, float offsetY)
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
