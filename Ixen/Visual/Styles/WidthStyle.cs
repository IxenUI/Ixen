using Ixen.Rendering;

namespace Ixen.Visual.Styles
{
    public class WidthStyle : SizeStyle
    {
        public WidthStyle()
        {}

        public WidthStyle(string content)
            : base(content)
        {}

        public override void Compute(VisualElement element, float x, float y, float width, float height)
        {
            switch (Unit)
            {
                case SizeUnit.Pixels:
                    element.Width = Value;
                    break;
                case SizeUnit.Percents:
                    element.Width = (width / 100) * Value;
                    break;
            }
        }

        public override void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {}
    }
}
