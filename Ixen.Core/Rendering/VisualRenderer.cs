using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
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
            if (element.StylesHandlers == null || element.Clip.IsVoidOrInvalid
                || element.IsHidden)
            {
                return;
            }

            bool transformed = element.HasTransform;

            if (transformed)
            {
                context.PushTransform(Transforms.Of(element));
            }

            OpacityStyleDescriptor opacity = element.StylesHandlers.Opacity.Descriptor;
            bool layered = opacity.IsTransparent;

            if (layered)
            {
                context.PushOpacity(opacity.Value);
            }

            bool backdrop = element.HasBackdropFilter;

            if (backdrop)
            {
                context.PushBackdrop(element.StylesHandlers.BackdropFilter.Chain,
                    element.X, element.Y, element.ActualWidth, element.ActualHeight,
                    element.StylesHandlers.CornerRadius.Descriptor);
            }

            bool filtered = element.HasFilter;

            if (filtered)
            {
                FilterChain chain = element.StylesHandlers.Filter.Chain;
                float grow = chain.Margin;

                context.PushFilter(chain,
                    element.X - element.BorderOutsideLeft - grow,
                    element.Y - element.BorderOutsideTop - grow,
                    element.ActualWidth + element.BorderOutsideLeft + element.BorderOutsideRight + grow * 2,
                    element.ActualHeight + element.BorderOutsideTop + element.BorderOutsideBottom + grow * 2);
            }

            RenderElement(element, context);

            if (element.Children.Count == 0 && !element.HasChrome)
            {
                if (filtered)
                {
                    context.PopClip();
                }

                if (backdrop)
                {
                    context.PopClip();
                    context.PopClip();
                }

                if (layered)
                {
                    context.PopClip();
                }

                if (transformed)
                {
                    context.PopClip();
                }

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

            if (filtered)
            {
                context.PopClip();
            }

            if (backdrop)
            {
                context.PopClip();
                context.PopClip();
            }

            if (layered)
            {
                context.PopClip();
            }

            if (transformed)
            {
                context.PopClip();
            }
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
