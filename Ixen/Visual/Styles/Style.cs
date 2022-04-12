using Ixen.Rendering;

namespace Ixen.Visual.Styles
{
    public abstract class Style
    {
        protected string _content;

        public Style()
        {}

        public Style(string content)
        {
            _content = content;
            Parse();
        }

        public abstract void Parse();
        public abstract void Compute(VisualElement element, float x, float y, float width, float height);
        public abstract void Render(VisualElement element, RendererContext context, ViewPort viewPort);
    }
}
