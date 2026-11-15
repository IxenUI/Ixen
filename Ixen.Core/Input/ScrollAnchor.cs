using Ixen.Core.Visual;
using System;

namespace Ixen.Core.Input
{
    internal struct ScrollAnchor
    {
        private const float SLACK = 0.01f;

        internal VisualElement Scroller;
        internal VisualElement Anchor;
        internal float Offset;
        internal float ScrollY;

        internal static bool Wants(VisualElement element)
            => element.Scrollable && element.ScrollAnchoring && element.ScrollY > 0
                && !element.IsRestoringScroll && !element.IsScrollingSmoothly;

        internal static ScrollAnchor Take(VisualElement scroller)
        {
            VisualElement anchor = Choose(scroller);

            return new ScrollAnchor
            {
                Scroller = scroller,
                Anchor = anchor,
                Offset = anchor == null ? 0 : anchor.Y - ViewportTop(scroller),
                ScrollY = scroller.ScrollY
            };
        }

        internal float Drift()
        {
            if (Anchor == null || Scroller.ScrollY != ScrollY || !Wants(Scroller) || Anchor.IsHiddenInTree
                || !Within(Anchor, Scroller))
            {
                return 0;
            }

            float drift = Anchor.Y - ViewportTop(Scroller) - Offset;

            return Math.Abs(drift) < SLACK ? 0 : drift;
        }

        private static float ViewportTop(VisualElement scroller)
            => scroller.ContentY + scroller.ScrollY;

        private static VisualElement Choose(VisualElement scroller)
        {
            float top = ViewportTop(scroller);
            VisualElement container = scroller;
            VisualElement chosen = null;

            while (container != null)
            {
                VisualElement next = null;

                foreach (VisualElement child in container.Children)
                {
                    if (child.IsOverlay || child.IsHiddenInTree || child.ActualHeight <= 0)
                    {
                        continue;
                    }

                    if (child.Y + child.ActualHeight > top)
                    {
                        next = child;
                        break;
                    }
                }

                if (next == null)
                {
                    break;
                }

                chosen = next;
                container = next.Y < top && !next.Scrollable ? next : null;
            }

            return chosen;
        }

        private static bool Within(VisualElement element, VisualElement scroller)
        {
            for (VisualElement current = element.Parent; current != null; current = current.Parent)
            {
                if (current == scroller)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
