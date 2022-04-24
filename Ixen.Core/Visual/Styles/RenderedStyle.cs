using Ixen.Core.Rendering;

namespace Ixen.Core.Visual.Styles
{
    public abstract class RenderedStyle : Style
    {
        public RenderedStyle() : base()
        {}

        public RenderedStyle(string content) : base(content)
        {}

        internal abstract void Render(VisualElement element, RendererContext context);
    }
}
