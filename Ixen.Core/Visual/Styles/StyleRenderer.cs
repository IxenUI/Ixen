using Ixen.Core.Rendering;

namespace Ixen.Core.Visual.Styles
{
    internal class StyleRenderer
    {
        internal void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {
            RenderElement(element, context);

            foreach (VisualElement child in element.Children)
            {
                Render(child, context, viewPort);
            }
        }

        private void RenderElement(VisualElement element, RendererContext context)
        {
            VisualElementStylesHandlers styles = element.StylesHandlers;
            if (styles == null || !element.Renderable)
            {
                return;
            }

            styles.Background?.Render(element, context);
            styles.Border?.Render(element, context);
        }
    }
}
