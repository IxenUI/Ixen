using Ixen.Core.Rendering;

namespace Ixen.Core.Visual.Styles
{
    public class BackgroundStyle : RenderedStyle
    {
        public Color Color { get; set; } = Color.Transparent;
        public string ImageUrl { get; set; }
        public bool RepeatX { get; set; }
        public bool RepeatY { get; set; }

        public BackgroundStyle()
        {}

        public BackgroundStyle(string content)
            : base(content)
        {}

        public override void Parse()
        {}

        public override void Render(VisualElement element, RendererContext context)
        {
            context.FillRectangle(element.X, element.Y, element.Width, element.Height, new Brush(Color));
        }
    }
}
