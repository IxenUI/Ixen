using Ixen.Core.Visual;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal class VisualRenderer
    {
        private TextRenderer _textRenderer = new();
        private readonly ImageRenderer _imageRenderer;

        internal VisualRenderer(ImageStore images = null)
        {
            _imageRenderer = new ImageRenderer(images);
        }

        internal void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {
            RenderTree(element, context, viewPort);

            if (!element.HasOverlays)
            {
                return;
            }

            List<VisualElement> overlays = element.Overlays;

            for (int index = 0; index < overlays.Count; index++)
            {
                RenderTree(overlays[index], context, viewPort);
            }
        }

        private void RenderTree(VisualElement element, RendererContext context, ViewPort viewPort)
        {
            if (element.StylesHandlers == null || element.Clip.IsVoidOrInvalid)
            {
                return;
            }

            RenderElement(element, context);

            if (element.Children.Count == 0 && !element.HasChrome)
            {
                return;
            }

            if (element.IsOverlay)
            {
                context.PushClip(element.Clip.X, element.Clip.Y,
                    element.Clip.ActualWidth, element.Clip.ActualHeight, null);
            }
            else
            {
                context.PushClip(element.X, element.Y, element.ActualWidth, element.ActualHeight,
                    element.StylesHandlers.CornerRadius.Descriptor);
            }

            foreach (VisualElement child in element.Children)
            {
                if (!child.IsOverlay)
                {
                    RenderTree(child, context, viewPort);
                }
            }

            if (element.HasChrome)
            {
                foreach (VisualElement chrome in element.Chrome)
                {
                    RenderTree(chrome, context, viewPort);
                }
            }

            context.PopClip();
        }

        private void RenderElement(VisualElement element, RendererContext context)
        {
            VisualElementStylesHandlers styles = element.StylesHandlers;

            ShadowRenderer.Render(element, context);
            styles.Background?.Render(element, context);
            _imageRenderer.Render(element, context);
            styles.Border?.Render(element, context);
            _textRenderer.Render(element, context);
        }
    }
}
