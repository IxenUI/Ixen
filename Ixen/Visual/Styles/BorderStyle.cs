using Ixen.Core;
using Ixen.Rendering;

namespace Ixen.Visual.Styles
{
    public enum BorderType
    {
        Outer,
        Inner,
        Center
    }

    public class BorderStyle : Style
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
        {}

        public override void Compute(VisualElement element, float x, float y, float width, float height)
        {}

        public override void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {
            switch (Type)
            {
                case Visual.Styles.BorderType.Center:
                    context.DrawRectangle(element.X, element.Y, element.Width, element.Height, new Pen(Color, Thickness));
                    break;
                case Visual.Styles.BorderType.Inner:
                    context.DrawInnerRectangle(element.X, element.Y, element.Width, element.Height, new Pen(Color, Thickness));
                    break;
                case Visual.Styles.BorderType.Outer:
                    context.DrawOuterRectangle(element.X, element.Y, element.Width, element.Height, new Pen(Color, Thickness));
                    break;
            }
        }
    }
}
