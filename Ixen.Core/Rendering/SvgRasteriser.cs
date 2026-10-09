using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
using SkiaSharp;
using System;

namespace Ixen.Core.Rendering
{
    internal static class SvgRasteriser
    {
        internal const int MAX_SIDE = 4096;

        internal static SKBitmap Render(SvgDocument document, int width, int height)
        {
            if (document == null || width <= 0 || height <= 0
                || document.Width <= 0 || document.Height <= 0)
            {
                return null;
            }

            if (width > MAX_SIDE)
            {
                width = MAX_SIDE;
            }

            if (height > MAX_SIDE)
            {
                height = MAX_SIDE;
            }

            var bitmap = new SKBitmap(width, height, SKImageInfo.PlatformColorType,
                SKAlphaType.Premul);

            using (var canvas = new SKCanvas(bitmap))
            using (var fill = new SKPaint { IsStroke = false, IsAntialias = true })
            using (var stroke = new SKPaint
            {
                IsStroke = true,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Butt,
                StrokeJoin = SKStrokeJoin.Miter
            })
            {
                canvas.Clear(SKColors.Transparent);
                canvas.Scale(width / document.Width, height / document.Height);
                canvas.Translate(-document.Left, -document.Top);

                foreach (SvgShape shape in document.Shapes)
                {
                    Paint(canvas, shape, fill, stroke);
                }
            }

            return bitmap;
        }

        private static void Paint(SKCanvas canvas, SvgShape shape, SKPaint fill, SKPaint stroke)
        {
            SKPath path = RendererContext.NativePath(shape.Geometry);

            if (path == null)
            {
                return;
            }

            canvas.Save();

            if (!shape.Transform.IsIdentity)
            {
                Matrix2D matrix = shape.Transform;
                var skia = new SKMatrix(matrix.ScaleX, matrix.SkewX, matrix.TransX,
                    matrix.SkewY, matrix.ScaleY, matrix.TransY, 0, 0, 1);

                canvas.Concat(in skia);
            }

            if (Tint(shape.Fill, shape.FillAlpha, out SKColor filled))
            {
                fill.Color = filled;
                canvas.DrawPath(path, fill);
            }

            if (shape.StrokeWidth > 0 && Tint(shape.Stroke, shape.StrokeAlpha, out SKColor stroked))
            {
                stroke.Color = stroked;
                stroke.StrokeWidth = shape.StrokeWidth;
                canvas.DrawPath(path, stroke);
            }

            canvas.Restore();
        }

        private static bool Tint(string paint, float alpha, out SKColor color)
        {
            color = SKColors.Transparent;

            if (string.IsNullOrEmpty(paint) || alpha <= 0)
            {
                return false;
            }

            if (!Color.TryParse(paint, out Color named))
            {
                return false;
            }

            SKColor parsed = named.SKColor;
            byte scaled = (byte)Math.Round(parsed.Alpha * (alpha > 1 ? 1 : alpha));

            if (scaled == 0)
            {
                return false;
            }

            color = parsed.WithAlpha(scaled);

            return true;
        }
    }
}
