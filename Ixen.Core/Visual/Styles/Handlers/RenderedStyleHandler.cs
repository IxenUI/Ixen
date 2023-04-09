using Ixen.Core.Rendering;

namespace Ixen.Core.Visual.Styles.Handlers
{
    public abstract class RenderedStyleHandler : StyleHandler
    {
        public RenderedStyleHandler() : base()
        { }

        internal abstract void Render(VisualElement element, RendererContext context);
    }
}
