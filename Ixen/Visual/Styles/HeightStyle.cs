using Ixen.Rendering;

namespace Ixen.Visual.Styles
{
    public class HeightStyle : SizeStyle
    {
        public HeightStyle()
        {}

        public HeightStyle(string content)
            : base(content)
        {}

        public override void Compute(VisualElement element, float x, float y, float width, float height)
        {
            switch (Unit)
            {
                case SizeUnit.Pixels:
                    element.Height = Value;
                    break;
                case SizeUnit.Percents:
                    element.Height = (height / 100) * Value;
                    break;
            }
        }

        public override void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {}
    }
}
