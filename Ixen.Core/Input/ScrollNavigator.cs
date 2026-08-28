using Ixen.Core.Visual;

namespace Ixen.Core.Input
{
    internal static class ScrollNavigator
    {
        internal const float STEP = 48f;

        internal static bool Scroll(VisualElement from, float offsetX, float offsetY)
        {
            for (VisualElement element = from; element != null; element = element.Parent)
            {
                if (element.Scrollable && CanScroll(element, offsetX, offsetY))
                {
                    element.ScrollBy(offsetX, offsetY);
                    return true;
                }
            }

            return false;
        }

        internal static VisualElement Find(VisualElement from, float offsetX, float offsetY)
        {
            for (VisualElement element = from; element != null; element = element.Parent)
            {
                if (element.Scrollable && CanScroll(element, offsetX, offsetY))
                {
                    return element;
                }
            }

            return null;
        }

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
