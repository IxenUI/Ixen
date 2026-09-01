using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;
using System;

namespace Ixen.Core.Rendering
{
    public sealed class RendererContext
    {
        private readonly SKRoundRect _roundRect = new();
        private readonly SKPathBuilder _insetBuilder = new();
        private readonly SKPoint[] _quad = new SKPoint[4];
        private readonly SKPaint _quadPaint = new SKPaint { IsStroke = false, IsAntialias = true };
        private readonly SKPoint[] _radii = new SKPoint[4];

        private int _clipDepth;
        private bool _filtered;

        internal SKCanvas SKCanvas { get; private set; }

        private readonly SKPaint _imagePaint = new SKPaint { IsAntialias = true };

        private readonly SKPaint _bandPaint = new SKPaint { IsStroke = false, IsAntialias = false };
        private readonly SKPaint _shadowPaint = new SKPaint { IsStroke = false, IsAntialias = true };
        private float _shadowBlur = -1;
        private readonly SKPaint _textShadowPaint = new SKPaint { IsStroke = false, IsAntialias = true };
        private float _textShadowBlur = -1;
        private readonly SKPaint _gradientPaint = new SKPaint { IsStroke = false, IsAntialias = true };
        private readonly SKPaint _layerPaint = new SKPaint { Color = SKColors.Black };

        private readonly SKSamplingOptions _sampling =
            new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);

        internal void BeginFrame(SKCanvas canvas, float scale)
        {
            SKCanvas = canvas;
            _clipDepth = 0;
            _filtered = false;

            if (scale <= 0 || scale == 1)
            {
                return;
            }

            SKCanvas.Save();
            _clipDepth++;
            SKCanvas.Scale(scale);
        }

        internal void EndFrame()
        {
            while (_clipDepth > 0)
            {
                SKCanvas.Restore();
                _clipDepth--;
            }

            if (!_filtered)
            {
                return;
            }

            _filtered = false;
            SKGraphics.PurgeResourceCache();
        }

        public void Clear(Color color)
        {
            SKCanvas.Clear(color.SKColor);
        }

        internal void PushClip(float x, float y, float width, float height, CornerRadiusStyleDescriptor radius)
        {
            SKCanvas.Save();
            _clipDepth++;

            if (radius != null && radius.HasRadius)
            {
                SKCanvas.ClipRoundRect(BuildRoundRect(x, y, width, height, radius), SKClipOperation.Intersect, true);
                return;
            }

            SKCanvas.ClipRect(new SKRect(x, y, x + width, y + height), SKClipOperation.Intersect, false);
        }

        internal void PushTransform(Matrix2D matrix)
        {
            SKCanvas.Save();
            _clipDepth++;

            var skia = new SKMatrix(matrix.ScaleX, matrix.SkewX, matrix.TransX,
                matrix.SkewY, matrix.ScaleY, matrix.TransY, 0, 0, 1);

            SKCanvas.Concat(in skia);
        }

        internal void PushFilter(FilterChain chain, float x, float y, float width, float height)
        {
            SKCanvas.SaveLayer(new SKRect(x, y, x + width, y + height), chain.Paint);
            _clipDepth++;
            _filtered = true;
        }

        internal void PushBackdrop(FilterChain chain, float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius)
        {
            PushClip(x, y, width, height, radius);

            var rect = new SKRect(x, y, x + width, y + height);
            var rec = new SKCanvasSaveLayerRec
            {
                Bounds = rect,
                Backdrop = chain.Paint?.ImageFilter
            };

            SKCanvas.SaveLayer(in rec);
            _clipDepth++;
            _filtered = true;
        }

        internal void PushOpacity(float opacity)
        {
            _layerPaint.Color = _layerPaint.Color.WithAlpha((byte)(opacity * 255));

            SKCanvas.SaveLayer(_layerPaint);
            _clipDepth++;
        }

        internal void PopClip()
        {
            if (_clipDepth == 0)
            {
                return;
            }

            SKCanvas.Restore();
            _clipDepth--;
        }

        public void DrawInnerRectangle(float x, float y, float width, float height, Pen pen)
        {
            pen.Antialisasing = false;

            SKCanvas.DrawRect
            (
                x + pen.Width / 2,
                y + pen.Width / 2,
                width - pen.Width,
                height - pen.Width,
                pen.SKPaint
            );
        }

        public void DrawOuterRectangle(float x, float y, float width, float height, Pen pen)
        {
            pen.Antialisasing = false;

            SKCanvas.DrawRect
            (
                x - pen.Width / 2,
                y - pen.Width / 2,
                width + pen.Width,
                height + pen.Width,
                pen.SKPaint
            );
        }

        public void DrawRectangle(float x, float y, float width, float height, Pen pen)
        {
            pen.Antialisasing = false;
            SKCanvas.DrawRect(x, y, width, height, pen.SKPaint);
        }

        internal void DrawImage(SKBitmap bitmap, float x, float y, float width, float height)
        {
            if (bitmap == null || width <= 0 || height <= 0)
            {
                return;
            }

            SKCanvas.DrawBitmap(bitmap, new SKRect(x, y, x + width, y + height), _sampling, _imagePaint);
        }

        internal void TileImage(SKPaint tile, float x, float y, float width, float height)
        {
            if (tile == null || width <= 0 || height <= 0)
            {
                return;
            }

            SKCanvas.Save();
            SKCanvas.Translate(x, y);
            SKCanvas.DrawRect(new SKRect(0, 0, width, height), tile);
            SKCanvas.Restore();
        }

        internal float GetLineHeight(FontSpec fontSpec)
            => fontSpec.LineHeight > 0 ? fontSpec.LineHeight : FontCache.Get(fontSpec).Spacing;

        private static float Baseline(float top, FontSpec fontSpec, SKFont font)
        {
            float leading = fontSpec.LineHeight > 0 ? (fontSpec.LineHeight - font.Spacing) / 2 : 0;

            return top + leading - font.Metrics.Ascent;
        }

        internal float MeasureTextWidth(string text, FontSpec fontSpec)
            => FontCache.Get(fontSpec, text).MeasureText(text) + fontSpec.Advance(text);

        private static float LastGap(FontSpec fontSpec) => fontSpec.LetterSpacing;

        private void DrawSpaced(string text, float x, float baseline, FontSpec fontSpec,
            SKFont font, SKPaint paint)
        {
            for (int index = 0; index < text.Length; index++)
            {
                string glyph = text[index].ToString();

                SKCanvas.DrawText(glyph, x, baseline, SKTextAlign.Left, font, paint);
                x += font.MeasureText(glyph) + fontSpec.LetterSpacing;
            }
        }

        internal void DrawText(string text, float x, float top, FontSpec fontSpec, Brush brush)
        {
            SKFont font = FontCache.Get(fontSpec, text);

            brush.Antialisasing = true;

            float baseline = Baseline(top, fontSpec, font);

            if (fontSpec.LetterSpacing != 0)
            {
                DrawSpaced(text, x, baseline, fontSpec, font, brush.SKPaint);
                return;
            }

            SKCanvas.DrawText(text, x, baseline, SKTextAlign.Left, font, brush.SKPaint);
        }

        internal void DrawTextShadow(string text, float x, float top, FontSpec fontSpec,
            ShadowStyleDescriptor shadow)
        {
            if (shadow == null || !shadow.IsDeclared)
            {
                return;
            }

            SKFont font = FontCache.Get(fontSpec, text);
            System.Collections.Generic.List<Shadow> shadows = shadow.Shadows;

            for (int index = shadows.Count - 1; index >= 0; index--)
            {
                DrawOneTextShadow(text, x, top, fontSpec, font, shadows[index]);
            }
        }

        private void DrawOneTextShadow(string text, float x, float top, FontSpec fontSpec,
            SKFont font, Shadow shadow)
        {
            if (shadow.Blur != _textShadowBlur)
            {
                _textShadowPaint.MaskFilter = shadow.Blur > 0
                    ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, shadow.Blur / 2)
                    : null;

                _textShadowBlur = shadow.Blur;
            }

            _textShadowPaint.Color = new Color(shadow.Color).SKColor;

            float shadowBaseline = Baseline(top + shadow.OffsetY, fontSpec, font);

            if (fontSpec.LetterSpacing != 0)
            {
                DrawSpaced(text, x + shadow.OffsetX, shadowBaseline, fontSpec, font, _textShadowPaint);
                return;
            }

            SKCanvas.DrawText(text, x + shadow.OffsetX, shadowBaseline,
                SKTextAlign.Left, font, _textShadowPaint);
        }

        internal void DrawTextDecoration(string text, float x, float top, FontSpec fontSpec,
            TextDecorationStyleDescriptor decoration, Brush brush)
        {
            if (decoration == null || decoration.Value == TextDecorations.None
                || string.IsNullOrEmpty(text))
            {
                return;
            }

            SKFont font = FontCache.Get(fontSpec, text);
            SKFontMetrics metrics = font.Metrics;
            float width = MeasureTextWidth(text, fontSpec) - LastGap(fontSpec);

            if (width <= 0)
            {
                return;
            }

            float baseline = Baseline(top, fontSpec, font);
            float thickness = metrics.UnderlineThickness ?? font.Size / 14f;

            if (thickness < 1)
            {
                thickness = 1;
            }

            if (decoration.Has(TextDecorations.Underline))
            {
                float offset = metrics.UnderlinePosition ?? font.Size / 9f;
                Line(x, baseline + offset, width, thickness, brush);
            }

            if (decoration.Has(TextDecorations.LineThrough))
            {
                float offset = metrics.StrikeoutPosition ?? metrics.Ascent / 2.6f;
                Line(x, baseline + offset, width, thickness, brush);
            }

            if (decoration.Has(TextDecorations.Overline))
            {
                Line(x, baseline + metrics.Ascent, width, thickness, brush);
            }
        }

        private void Line(float x, float y, float width, float thickness, Brush brush)
        {
            _bandPaint.Color = brush.Color.SKColor;
            SKCanvas.DrawRect(x, y, width, thickness, _bandPaint);
        }

        internal void FillRoundRectangle(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, Brush brush)
        {
            brush.Antialisasing = true;
            SKCanvas.DrawRoundRect(BuildRoundRect(x, y, width, height, radius), brush.SKPaint);
        }

        internal void DrawRoundRectangle(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, Pen pen, BorderType type)
        {
            float offset = 0;

            if (type == BorderType.Inner)
            {
                offset = pen.Width / 2;
            }
            else if (type == BorderType.Outer)
            {
                offset = -pen.Width / 2;
            }

            pen.Antialisasing = true;
            SKCanvas.DrawRoundRect(
                BuildRoundRect(x + offset, y + offset, width - offset * 2, height - offset * 2, radius),
                pen.SKPaint);
        }

        internal void FillGradient(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, GradientShader gradient)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _gradientPaint.Shader = gradient.For(width, height);

            SKCanvas.Save();
            SKCanvas.Translate(x, y);

            if (radius != null && radius.HasRadius)
            {
                SKCanvas.DrawRoundRect(BuildRoundRect(0, 0, width, height, radius), _gradientPaint);
            }
            else
            {
                SKCanvas.DrawRect(0, 0, width, height, _gradientPaint);
            }

            SKCanvas.Restore();
            _gradientPaint.Shader = null;
        }

        internal void DrawShadow(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, float blur, Color color)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            SetShadowBlur(blur);

            _shadowPaint.Color = color.SKColor;

            if (radius != null && radius.HasRadius)
            {
                SKCanvas.DrawRoundRect(BuildRoundRect(x, y, width, height, radius), _shadowPaint);
                return;
            }

            SKCanvas.DrawRect(x, y, width, height, _shadowPaint);
        }

        internal void FillQuad(float x1, float y1, float x2, float y2, float x3, float y3,
            float x4, float y4, Color color)
        {
            _quadPaint.Color = color.SKColor;

            _quad[0] = new SKPoint(x1, y1);
            _quad[1] = new SKPoint(x2, y2);
            _quad[2] = new SKPoint(x3, y3);
            _quad[3] = new SKPoint(x4, y4);

            _insetBuilder.Reset();
            _insetBuilder.FillType = SKPathFillType.Winding;
            _insetBuilder.AddPoly(_quad, true);

            using (SKPath path = _insetBuilder.Detach())
            {
                SKCanvas.DrawPath(path, _quadPaint);
            }
        }

        private void SetShadowBlur(float blur)
        {
            if (blur == _shadowBlur)
            {
                return;
            }

            _shadowPaint.MaskFilter = blur > 0
                ? SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur / 2)
                : null;

            _shadowBlur = blur;
        }

        internal void DrawInsetShadow(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, float offsetX, float offsetY, float blur,
            float spread, Color color)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            SetShadowBlur(blur);

            _shadowPaint.Color = color.SKColor;

            PushClip(x, y, width, height, radius);

            float reach = blur * 2 + spread + Math.Abs(offsetX) + Math.Abs(offsetY) + 1;

            _insetBuilder.Reset();
            _insetBuilder.FillType = SKPathFillType.EvenOdd;
            _insetBuilder.AddRect(new SKRect(x - reach, y - reach,
                x + width + reach, y + height + reach));

            float innerX = x + offsetX + spread;
            float innerY = y + offsetY + spread;
            float innerWidth = width - spread * 2;
            float innerHeight = height - spread * 2;

            if (innerWidth > 0 && innerHeight > 0)
            {
                if (radius != null && radius.HasRadius)
                {
                    _insetBuilder.AddRoundRect(
                        BuildRoundRect(innerX, innerY, innerWidth, innerHeight, radius));
                }
                else
                {
                    _insetBuilder.AddRect(new SKRect(innerX, innerY,
                        innerX + innerWidth, innerY + innerHeight));
                }
            }

            using (SKPath path = _insetBuilder.Detach())
            {
                SKCanvas.DrawPath(path, _shadowPaint);
            }

            PopClip();
        }

        private SKRoundRect BuildRoundRect(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius)
        {
            _radii[0] = new SKPoint(radius.TopLeft, radius.TopLeft);
            _radii[1] = new SKPoint(radius.TopRight, radius.TopRight);
            _radii[2] = new SKPoint(radius.BottomRight, radius.BottomRight);
            _radii[3] = new SKPoint(radius.BottomLeft, radius.BottomLeft);

            _roundRect.SetRectRadii(new SKRect(x, y, x + width, y + height), _radii);

            return _roundRect;
        }

        public void FillRectangle(float x, float y, float width, float height, Brush brush)
        {
            brush.Antialisasing = false;
            SKCanvas.DrawRect(x, y, width, height, brush.SKPaint);
        }

        internal void FillRectangle(float x, float y, float width, float height, Color color)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _bandPaint.Color = color.SKColor;
            SKCanvas.DrawRect(x, y, width, height, _bandPaint);
        }
    }
}
