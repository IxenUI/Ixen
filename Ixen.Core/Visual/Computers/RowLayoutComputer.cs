namespace Ixen.Core.Visual.Computers
{
    internal class RowLayoutComputer
    {
        internal void Compute(VisualElement element)
        {
            float startX = element.X + element.PaddingLeft;
            float x = startX;
            float y = element.Y + element.PaddingTop;
            float topH = 0;

            foreach (VisualElement child in element.Children)
            {
                child.SetPosition(x, y);
                x += child.BoxWidth;

                if (child.BoxHeight > topH)
                {
                    topH = child.BoxHeight;
                }
            }

            if (!element.IsWidthComputed)
            {
                element.Width = (x - startX) + element.HorizontalPadding;
                element.IsWidthComputed = true;
            }

            if (!element.IsHeightComputed)
            {
                element.Height = topH + element.VerticalPadding;
                element.IsHeightComputed = true;
            }
        }
    }
}
