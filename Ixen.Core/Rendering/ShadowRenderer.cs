using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal static class ShadowRenderer
    {
        internal static void Render(VisualElement element, RendererContext context)
        {
            ShadowStyleDescriptor descriptor = element.StylesHandlers.BoxShadow.Descriptor;

            if (!descriptor.IsDeclared)
            {
                return;
            }

            List<Shadow> shadows = descriptor.Shadows;
            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;

            for (int index = shadows.Count - 1; index >= 0; index--)
            {
                Shadow shadow = shadows[index];
                float spread = shadow.Spread;

                context.DrawShadow(
                    element.X + shadow.OffsetX - spread,
                    element.Y + shadow.OffsetY - spread,
                    element.ActualWidth + spread * 2,
                    element.ActualHeight + spread * 2,
                    radius,
                    shadow.Blur,
                    new Color(shadow.Color));
            }
        }
    }
}
