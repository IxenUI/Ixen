using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;
using System;

namespace Ixen.Core.Rendering
{
    internal sealed class GradientShader : IDisposable
    {
        private readonly Gradient _gradient;
        private readonly SKColor[] _colors;
        private readonly float[] _offsets;

        private SKShader _shader;
        private float _width = -1;
        private float _height = -1;

        internal GradientShader(Gradient gradient)
        {
            _gradient = gradient;
            _colors = new SKColor[gradient.Stops.Count];
            _offsets = new float[gradient.Stops.Count];

            for (int i = 0; i < gradient.Stops.Count; i++)
            {
                GradientStop stop = gradient.Stops[i];

                _colors[i] = new Color(stop.Color).SKColor;
                _offsets[i] = stop.HasOffset
                    ? stop.Offset
                    : (gradient.Stops.Count == 1 ? 0 : (float)i / (gradient.Stops.Count - 1));
            }
        }

        internal SKShader For(float width, float height)
        {
            if (_shader != null && width == _width && height == _height)
            {
                return _shader;
            }

            _shader?.Dispose();

            _width = width;
            _height = height;
            _shader = Build(width, height);

            return _shader;
        }

        private SKShader Build(float width, float height)
        {
            if (_gradient.Kind == GradientKind.Radial)
            {
                return SKShader.CreateRadialGradient(
                    new SKPoint(width / 2, height / 2),
                    Math.Max(width, height) / 2,
                    _colors,
                    _offsets,
                    SKShaderTileMode.Clamp);
            }

            double radians = (_gradient.Angle - 90) * Math.PI / 180;
            float dx = (float)Math.Cos(radians);
            float dy = (float)Math.Sin(radians);

            float halfWidth = width / 2;
            float halfHeight = height / 2;
            float extent = Math.Abs(dx) * halfWidth + Math.Abs(dy) * halfHeight;

            var start = new SKPoint(halfWidth - dx * extent, halfHeight - dy * extent);
            var end = new SKPoint(halfWidth + dx * extent, halfHeight + dy * extent);

            return SKShader.CreateLinearGradient(start, end, _colors, _offsets, SKShaderTileMode.Clamp);
        }

        public void Dispose()
        {
            _shader?.Dispose();
            _shader = null;
        }
    }
}
