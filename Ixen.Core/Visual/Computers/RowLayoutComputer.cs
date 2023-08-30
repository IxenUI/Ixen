namespace Ixen.Core.Visual.Computers
{
    internal class RowLayoutComputer
    {
        internal void Compute(VisualElement element)
        {
            float x = element.X;
            float y = element.Y;
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
                element.Width = x;
                element.IsWidthComputed = true;
            }

            if (!element.IsHeightComputed)
            {
                element.Height = topH;
                element.IsHeightComputed = true;
            }
        }
    }
}
