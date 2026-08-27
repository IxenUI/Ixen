using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;
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

                switch (operation.Kind)
                {
                    case FilterKind.Blur:
                        filter = SKImageFilter.CreateBlur(operation.Value, operation.Value, filter);
                        break;
                }
            }

            return filter == null ? null : new SKPaint { ImageFilter = filter };
        }
    }
}
