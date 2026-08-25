using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Rendering
{
    internal static class ShadowRenderer
    {
        internal static void Render(VisualElement element, RendererContext context)
        {
            ShadowStyleDescriptor shadow = element.StylesHandlers.BoxShadow.Descriptor;

            if (!shadow.IsDeclared)
            {
                return;
            }

            float spread = shadow.Spread;

            context.DrawShadow(
                element.X + shadow.OffsetX - spread,
                element.Y + shadow.OffsetY - spread,
                element.ActualWidth + spread * 2,
                element.ActualHeight + spread * 2,
                element.StylesHandlers.CornerRadius.Descriptor,
                shadow.Blur,
                new Color(shadow.Color));
        }
    }
}
