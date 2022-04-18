namespace Ixen.Visual.Styles
{
    public class WidthStyle : SizeStyle
    {
        public WidthStyle()
        {}

        public WidthStyle(string content)
            : base(content)
        {}

        public void Compute(VisualElement element, VisualElement container, DimensionalElement targetZone)
        {
            switch (Unit)
            {
                case SizeUnit.Pixels:
                    element.Width = Value;
                    break;
                case SizeUnit.Percents:
                    element.Width = (container.Width / 100) * Value;
                    break;
            }
        }
    }
}
