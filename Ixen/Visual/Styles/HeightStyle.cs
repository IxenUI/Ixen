namespace Ixen.Visual.Styles
{
    public class HeightStyle : SizeStyle
    {
        public HeightStyle()
        {}

        public HeightStyle(string content)
            : base(content)
        {}

        public void Compute(VisualElement element, VisualElement container)
        {
            switch (Unit)
            {
                case SizeUnit.Pixels:
                    element.Height = Value;
                    break;
                case SizeUnit.Percents:
                    element.Height = (container.Height / 100) * Value;
                    break;
            }
        }
    }
}
