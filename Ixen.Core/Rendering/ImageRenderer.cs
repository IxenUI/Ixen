using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;
using System;

namespace Ixen.Core.Rendering
{
    internal class ImageRenderer
    {
        private const float EPSILON = 0.01f;

        private readonly ImageStore _images;

        internal ImageRenderer(ImageStore images)
        {
            _images = images;
        }

        internal void Render(VisualElement element, RendererContext context)
        {
            if (_images == null)
            {
                return;
            }

            RenderBackground(element, context);
            RenderContent(element, context);
        }

        private void RenderBackground(VisualElement element, RendererContext context)
        {
            BackgroundStyleDescriptor background = element.StylesHandlers.Background.Descriptor;

            if (background == null || string.IsNullOrEmpty(background.ImageUrl))
            {
                return;
            }

            SKBitmap bitmap = _images.Get(background.ImageUrl);

            if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return;
            }

            float width = element.ActualWidth;
            float height = element.ActualHeight;

            if (width <= 0 || height <= 0)
            {
                return;
            }

            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;
            bool rounded = radius.HasRadius;

            if (rounded)
            {
                context.PushClip(element.X, element.Y, width, height, radius);
            }

            if (background.RepeatX || background.RepeatY)
            {
                context.TileImage(_images.GetTile(background.ImageUrl), element.X, element.Y,
                    background.RepeatX ? width : Math.Min(width, bitmap.Width),
                    background.RepeatY ? height : Math.Min(height, bitmap.Height));
            }
            else
            {
                float drawWidth = bitmap.Width;
                float drawHeight = bitmap.Height;
                float x = element.X;
                float y = element.Y;

                if (background.IsScaled)
                {
                    Resolve(background.Fit, bitmap.Width, bitmap.Height, width, height,
                        out drawWidth, out drawHeight);

                    x += (width - drawWidth) / 2f;
                    y += (height - drawHeight) / 2f;
                }

                bool overflows = drawWidth > width + EPSILON || drawHeight > height + EPSILON;

                if (overflows)
                {
                    context.PushClip(element.X, element.Y, width, height, null);
                }

                context.DrawImage(bitmap, x, y, drawWidth, drawHeight);

                if (overflows)
                {
                    context.PopClip();
                }
            }

            if (rounded)
            {
                context.PopClip();
            }
        }

        private void RenderContent(VisualElement element, RendererContext context)
        {
            if (!(element is Image image) || string.IsNullOrEmpty(image.Source))
            {
                return;
            }

            SKBitmap bitmap = _images.Get(image.Source);

            if (bitmap == null || bitmap.Width <= 0 || bitmap.Height <= 0)
            {
                return;
            }

            float boxWidth = element.ContentWidth;
            float boxHeight = element.ContentHeight;

            if (boxWidth <= 0 || boxHeight <= 0)
            {
                return;
            }

            float boxX = element.X + element.PaddingLeft + element.BorderInsideLeft;
            float boxY = element.Y + element.PaddingTop + element.BorderInsideTop;

            Resolve(element.StylesHandlers.ObjectFit.Descriptor.Value, bitmap.Width, bitmap.Height,
                boxWidth, boxHeight, out float width, out float height);

            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;

            bool rounded = radius.HasRadius;
            bool overflows = width > boxWidth + EPSILON || height > boxHeight + EPSILON;

            if (rounded)
            {
                context.PushClip(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius);
            }

            if (overflows)
            {
                context.PushClip(boxX, boxY, boxWidth, boxHeight, null);
            }

            context.DrawImage(bitmap,
                boxX + (boxWidth - width) / 2f,
                boxY + (boxHeight - height) / 2f,
                width,
                height);

            if (overflows)
            {
                context.PopClip();
            }

            if (rounded)
            {
                context.PopClip();
            }
        }

        private static void Resolve(ObjectFit fit, float naturalWidth, float naturalHeight,
            float boxWidth, float boxHeight, out float width, out float height)
        {
            switch (fit)
            {
                case ObjectFit.Contain:
                    Scale(Math.Min(boxWidth / naturalWidth, boxHeight / naturalHeight),
                        naturalWidth, naturalHeight, out width, out height);
                    return;

                case ObjectFit.Cover:
                    Scale(Math.Max(boxWidth / naturalWidth, boxHeight / naturalHeight),
                        naturalWidth, naturalHeight, out width, out height);
                    return;

                case ObjectFit.None:
                    width = naturalWidth;
                    height = naturalHeight;
                    return;

                case ObjectFit.ScaleDown:
                    Scale(Math.Min(1f, Math.Min(boxWidth / naturalWidth, boxHeight / naturalHeight)),
                        naturalWidth, naturalHeight, out width, out height);
                    return;

                default:
                    width = boxWidth;
                    height = boxHeight;
                    return;
            }
        }

        private static void Scale(float factor, float naturalWidth, float naturalHeight,
            out float width, out float height)
        {
            width = naturalWidth * factor;
            height = naturalHeight * factor;
        }
    }
}
