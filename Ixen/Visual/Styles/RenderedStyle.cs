using Ixen.Rendering;

namespace Ixen.Visual.Styles
{
    public abstract class RenderedStyle : Style
    {
        public RenderedStyle() : base()
        {}

        public RenderedStyle(string content) : base(content)
        {}

        public abstract void Render(VisualElement element, RendererContext context);
    }
}
