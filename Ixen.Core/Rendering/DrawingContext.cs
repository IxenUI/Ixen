using Ixen.Core.Visual;
using SkiaSharp;
using System;

namespace Ixen.Core.Rendering
{
    public sealed class DrawingContext
    {
        private readonly SKPaint _fill = new SKPaint { IsStroke = false, IsAntialias = true };

        private readonly SKPaint _stroke = new SKPaint
        {
            IsStroke = true,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        private SKPoint[] _points = new SKPoint[64];

        private RendererContext _renderer;
        private SKCanvas _canvas;
        private int _saved;

        internal DrawingContext()
        {
        }

        public float Width { get; private set; }

        public float Height { get; private set; }

        public Color Foreground { get; private set; }

        public Color ColorOf(string colorOrToken) => _renderer.ColorOf(colorOrToken);

        internal SKCanvas Canvas => _canvas;

        internal void Begin(RendererContext renderer, DrawingElement element)
        {
            _renderer = renderer;
            _canvas = renderer.SKCanvas;

            Width = element.ContentWidth;
            Height = element.ContentHeight;
            Foreground = element.StylesHandlers.Color.Brush.Color;

            _saved = _canvas.Save();

            _canvas.ClipRect(new SKRect(
                element.X + element.BorderInsideLeft,
                element.Y + element.BorderInsideTop,
                element.X + element.ActualWidth - element.BorderInsideRight,
                element.Y + element.ActualHeight - element.BorderInsideBottom));
            _canvas.Translate(element.ContentX, element.ContentY);
        }

        internal void End()
        {
            _canvas.RestoreToCount(_saved);
            _canvas = null;
            _renderer = null;
        }

        private SKPaint Fill(Color color)
        {
            _fill.Shader = null;
            _fill.Color = color.SKColor;

            return _fill;
        }

        private SKPaint Fill(DrawingGradient gradient)
        {
            _fill.Color = SKColors.White;
            _fill.Shader = gradient.ShaderFor(Width, Height);

            return _fill;
        }

        private SKPaint Stroke(Color color, float thickness)
        {
            _stroke.Color = color.SKColor;
            _stroke.StrokeWidth = thickness;

            return _stroke;
        }

        public void FillRectangle(float x, float y, float width, float height, Color color)
            => _canvas.DrawRect(x, y, width, height, Fill(color));

        public void FillRectangle(float x, float y, float width, float height, DrawingGradient gradient)
            => _canvas.DrawRect(x, y, width, height, Fill(gradient));

        public void FillRoundRectangle(float x, float y, float width, float height, float radius, Color color)
            => _canvas.DrawRoundRect(x, y, width, height, radius, radius, Fill(color));

        public void FillRoundRectangle(float x, float y, float width, float height, float radius,
            DrawingGradient gradient)
            => _canvas.DrawRoundRect(x, y, width, height, radius, radius, Fill(gradient));

        public void FillCircle(float centerX, float centerY, float radius, Color color)
            => _canvas.DrawCircle(centerX, centerY, radius, Fill(color));

        public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1)
            => _canvas.DrawLine(x1, y1, x2, y2, Stroke(color, thickness));

        public void DrawCircle(float centerX, float centerY, float radius, Color color, float thickness = 1)
            => _canvas.DrawCircle(centerX, centerY, radius, Stroke(color, thickness));

        public void DrawArc(float centerX, float centerY, float radius, float startDegrees, float sweepDegrees,
            Color color, float thickness = 1)
        {
            var bounds = new SKRect(centerX - radius, centerY - radius, centerX + radius, centerY + radius);

            _canvas.DrawArc(bounds, startDegrees - 90, sweepDegrees, false, Stroke(color, thickness));
        }

        public void DrawPolyline(float[] points, int count, Color color, float thickness = 1)
        {
            if (points == null || count < 2)
            {
                return;
            }

            if (count * 2 > points.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count),
                    $"{count} points need {count * 2} numbers, and the array holds {points.Length}.");
            }

            if (_points.Length < count)
            {
                _points = new SKPoint[Math.Max(count, _points.Length * 2)];
            }

            for (int index = 0; index < count; index++)
            {
                _points[index] = new SKPoint(points[index * 2], points[index * 2 + 1]);
            }

            if (_points.Length == count)
            {
                _canvas.DrawPoints(SKPointMode.Polygon, _points, Stroke(color, thickness));
                return;
            }

            for (int index = 1; index < count; index++)
            {
                _canvas.DrawLine(_points[index - 1], _points[index], Stroke(color, thickness));
            }
        }
    }
}
