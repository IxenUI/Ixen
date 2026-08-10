using Ixen.Core.Visual;

namespace Ixen.Core.Rendering
{
    internal class VisualRenderer
    {
        private TextRenderer _textRenderer = new();

        internal void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {
            if (element.StylesHandlers == null || element.Clip.IsVoidOrInvalid)
            {
                return;
            }

            RenderElement(element, context);

            if (element.Children.Count == 0)
            {
                return;
            }

            context.PushClip(element.X, element.Y, element.ActualWidth, element.ActualHeight,
                element.StylesHandlers.CornerRadius.Descriptor);

            foreach (VisualElement child in element.Children)
            {
                Render(child, context, viewPort);
            }

            context.PopClip();
        }

        private void RenderElement(VisualElement element, RendererContext context)
        {
            VisualElementStylesHandlers styles = element.StylesHandlers;

            styles.Background?.Render(element, context);
            styles.Border?.Render(element, context);
            _textRenderer.Render(element, context);
        }
    }
}
