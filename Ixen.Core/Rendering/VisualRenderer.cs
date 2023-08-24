using Ixen.Core.Visual;
using SkiaSharp;
using System;

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

        private DimensionalElement ComputeElementClip(VisualElement element)
        {
            DimensionalElement res = element;

            while (element.Parent != null)
            {
                element = element.Parent;
                res = res.Intersect(element);
            }

            return res;
        }

        private void RenderElement(VisualElement element, RendererContext context)
        {
            VisualElementStylesHandlers styles = element.StylesHandlers;
            if (styles == null)
            {
                return;
            }

            var clip = ComputeElementClip(element);
            context.SetClip(clip.X, clip.Y, clip.Width, clip.Height);

            styles.Background?.Render(element, context);
            styles.Border?.Render(element, context);
        }
    }
}
