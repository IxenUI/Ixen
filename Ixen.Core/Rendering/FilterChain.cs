using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal sealed class FilterChain
    {
        private readonly FilterStyleDescriptor _descriptor;

        private SKPaint _paint;
        private bool _built;

        internal FilterChain(FilterStyleDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        private const float SIGMAS = 3f;

        internal float Margin
        {
            get
            {
                float total = 0;
                List<FilterOperation> operations = _descriptor.Operations;

                for (int index = 0; index < operations.Count; index++)
                {
                    if (operations[index].Kind == FilterKind.Blur)
                    {
                        total += operations[index].Value;
                    }
                }

                return total * SIGMAS;
            }
        }

        internal SKPaint Paint
        {
            get
            {
                if (_built)
                {
                    return _paint;
                }

                _built = true;
                _paint = Build();

                return _paint;
            }
        }

        private SKPaint Build()
        {
            List<FilterOperation> operations = _descriptor.Operations;
            SKImageFilter filter = null;

            for (int index = 0; index < operations.Count; index++)
            {
                FilterOperation operation = operations[index];

                if (operation.Kind == FilterKind.Blur)
                {
                    filter = SKImageFilter.CreateBlur(operation.Value, operation.Value, filter);
                    continue;
                }

                float[] matrix = Matrix(operation);

                if (matrix == null)
                {
                    continue;
                }

                filter = SKImageFilter.CreateColorFilter(
                    SKColorFilter.CreateColorMatrix(matrix), filter);
            }

            return filter == null ? null : new SKPaint { ImageFilter = filter };
        }

        private const float LUMA_R = 0.213f;
        private const float LUMA_G = 0.715f;
        private const float LUMA_B = 0.072f;

        private static float[] Matrix(FilterOperation operation)
        {
            float value = operation.Value;

            switch (operation.Kind)
            {
                case FilterKind.Grayscale:
                    return Saturation(1 - value);

                case FilterKind.Saturate:
                    return Saturation(value);

                case FilterKind.Sepia:
                    return Sepia(value);

                case FilterKind.HueRotate:
                    return HueRotate(value);

                case FilterKind.Invert:
                    return Scaled(1 - 2 * value, value);

                case FilterKind.Brightness:
                    return Scaled(value, 0);

                case FilterKind.Contrast:
                    return Scaled(value, (1 - value) / 2);

                case FilterKind.Opacity:
                    return new float[]
                    {
                        1, 0, 0, 0, 0,
                        0, 1, 0, 0, 0,
                        0, 0, 1, 0, 0,
                        0, 0, 0, value, 0
                    };

                default:
                    return null;
            }
        }

        private static float[] Scaled(float scale, float offset)
            => new float[]
            {
                scale, 0, 0, 0, offset,
                0, scale, 0, 0, offset,
                0, 0, scale, 0, offset,
                0, 0, 0, 1, 0
            };

        private static float[] Saturation(float value)
            => new float[]
            {
                LUMA_R + (1 - LUMA_R) * value, LUMA_G - LUMA_G * value, LUMA_B - LUMA_B * value, 0, 0,
                LUMA_R - LUMA_R * value, LUMA_G + (1 - LUMA_G) * value, LUMA_B - LUMA_B * value, 0, 0,
                LUMA_R - LUMA_R * value, LUMA_G - LUMA_G * value, LUMA_B + (1 - LUMA_B) * value, 0, 0,
                0, 0, 0, 1, 0
            };

        private static float[] Sepia(float value)
        {
            float rest = 1 - value;

            return new float[]
            {
                0.393f + 0.607f * rest, 0.769f - 0.769f * rest, 0.189f - 0.189f * rest, 0, 0,
                0.349f - 0.349f * rest, 0.686f + 0.314f * rest, 0.168f - 0.168f * rest, 0, 0,
                0.272f - 0.272f * rest, 0.534f - 0.534f * rest, 0.131f + 0.869f * rest, 0, 0,
                0, 0, 0, 1, 0
            };
        }

        private static float[] HueRotate(float degrees)
        {
            double radians = degrees * Math.PI / 180;
            float cos = (float)Math.Cos(radians);
            float sin = (float)Math.Sin(radians);

            return new float[]
            {
                LUMA_R + cos * 0.787f - sin * 0.213f,
                LUMA_G - cos * 0.715f - sin * 0.715f,
                LUMA_B - cos * 0.072f + sin * 0.928f, 0, 0,

                LUMA_R - cos * 0.213f + sin * 0.143f,
                LUMA_G + cos * 0.285f + sin * 0.140f,
                LUMA_B - cos * 0.072f - sin * 0.283f, 0, 0,

                LUMA_R - cos * 0.213f - sin * 0.787f,
                LUMA_G - cos * 0.715f + sin * 0.715f,
                LUMA_B + cos * 0.928f + sin * 0.072f, 0, 0,

                0, 0, 0, 1, 0
            };
        }
    }
}
