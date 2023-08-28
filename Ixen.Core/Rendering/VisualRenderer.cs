using Ixen.Core.Visual;

namespace Ixen.Core.Rendering
{
    internal class VisualRenderer
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
            if (styles == null || element.Clip.IsVoidOrInvalid)
            {
                return;
            }

            context.SetClip(element.Clip.X, element.Clip.Y, element.Clip.Width, element.Clip.Height);

            styles.Background?.Render(element, context);
            styles.Border?.Render(element, context);
        }
    }
}
