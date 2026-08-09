namespace Ixen.Core.Visual.Computers
{
    internal class ColumnLayoutComputer
    {
        internal void Compute(VisualElement element)
        {
            float x = element.X + element.PaddingLeft;
            float startY = element.Y + element.PaddingTop;
            float y = startY;
            float topW = 0;

            foreach (VisualElement child in element.Children)
            {
                child.SetPosition(x, y);
                y += child.BoxHeight;

                if (child.BoxWidth > topW)
                {
                    topW = child.BoxWidth;
                }
            }

            if (!element.IsHeightComputed)
            {
                element.Height = (y - startY) + element.VerticalPadding;
                element.IsHeightComputed = true;
            }

            if (!element.IsWidthComputed)
            {
                element.Width = topW + element.HorizontalPadding;
                element.IsWidthComputed = true;
            }
        }
    }
}
