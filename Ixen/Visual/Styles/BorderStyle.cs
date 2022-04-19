using Ixen.Core;
using Ixen.Rendering;

namespace Ixen.Visual.Styles
{
    public class BorderStyle : RenderedStyle
    {
        public Color Color { get; set; } = Color.Transparent;
        public float Thickness { get; set; } = 1;
        public BorderType Type { get; set; } = BorderType.Outer;

        public BorderStyle()
        {}

        public BorderStyle(string content)
            : base(content)
        {}

        public override void Parse()
        {
            throw new System.NotImplementedException();
        }

        public override void Render(VisualElement element, RendererContext context)
        {
            switch (Type)
            {
                case BorderType.Center:
                    context.DrawRectangle(element.X, element.Y, element.Width, element.Height, new Pen(Color, Thickness));
                    break;
                case BorderType.Inner:
                    context.DrawInnerRectangle(element.X, element.Y, element.Width, element.Height, new Pen(Color, Thickness));
                    break;
                case BorderType.Outer:
                    context.DrawOuterRectangle(element.X, element.Y, element.Width, element.Height, new Pen(Color, Thickness));
                    break;
            }
        }
    }
}
