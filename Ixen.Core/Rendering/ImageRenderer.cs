using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;

namespace Ixen.Core.Rendering
{
    internal class ImageRenderer
    {
        private readonly ImageStore _images;

        internal ImageRenderer(ImageStore images)
        {
            _images = images;
        }

        internal void Render(VisualElement element, RendererContext context)
        {
            if (_images == null || !(element is Image image) || string.IsNullOrEmpty(image.Source))
            {
                return;
            }

            SKBitmap bitmap = _images.Get(image.Source);

            if (bitmap == null)
            {
                return;
            }

            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;
            bool rounded = radius.HasRadius;

            if (rounded)
            {
                context.PushClip(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius);
            }

            context.DrawImage(bitmap,
                element.X + element.PaddingLeft + element.BorderInsideLeft,
                element.Y + element.PaddingTop + element.BorderInsideTop,
                element.ContentWidth,
                element.ContentHeight);

            if (rounded)
            {
                context.PopClip();
            }
        }
    }
}
