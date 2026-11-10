namespace Ixen.Core.Visual
{
    internal static class Marquee
    {
        internal const int PAUSE_TICKS = 90;
        internal const float STEP = 0.5f;
        internal const float GAP = 40;

        internal static bool Overflows(VisualElement element)
            => element.TextLines != null
                && element.TextLines.Count == 1
                && element.TextLayout != null
                && element.TextLayout.Width > element.TextSlotWidth + 0.5f;

        internal static bool Advance(VisualElement element)
        {
            element.MarqueeTicks++;

            float travel = (element.MarqueeTicks - PAUSE_TICKS) * STEP;

            if (travel <= 0)
            {
                return false;
            }

            if (travel >= element.TextLayout.Width + GAP)
            {
                Rest(element);

                return true;
            }

            element.MarqueeOffset = travel;

            return true;
        }

        internal static bool Rest(VisualElement element)
        {
            bool moved = element.MarqueeOffset != 0;

            element.MarqueeTicks = 0;
            element.MarqueeOffset = 0;

            return moved;
        }
    }
}
