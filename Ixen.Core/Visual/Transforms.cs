using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    internal static class Transforms
    {
        internal static Matrix2D Of(VisualElement element)
        {
            TransformStyleDescriptor transform = element.HasAnimations
                ? element.AnimatedTransform() ?? element.StylesHandlers.Transform.Descriptor
                : element.StylesHandlers.Transform.Descriptor;

            if (transform == null || !transform.IsDeclared)
            {
                return Matrix2D.Identity;
            }

            float width = element.ActualWidth;
            float height = element.ActualHeight;

            Matrix2D matrix = Matrix2D.Identity;
            List<TransformOperation> operations = transform.Operations;

            for (int index = 0; index < operations.Count; index++)
            {
                matrix = Matrix2D.Concat(matrix, Of(operations[index], width, height));
            }

            TransformOriginStyleDescriptor origin = element.StylesHandlers.TransformOrigin.Descriptor;

            float originX = element.X + Resolve(origin.XUnit, origin.X, width);
            float originY = element.Y + Resolve(origin.YUnit, origin.Y, height);

            return Matrix2D.Concat(
                Matrix2D.Concat(Matrix2D.Translation(originX, originY), matrix),
                Matrix2D.Translation(-originX, -originY));
        }

        private static Matrix2D Of(TransformOperation operation, float width, float height)
        {
            switch (operation.Kind)
            {
                case TransformKind.Translate:
                    return Matrix2D.Translation(
                        Resolve(operation.XUnit, operation.X, width),
                        Resolve(operation.YUnit, operation.Y, height));

                case TransformKind.Scale:
                    return Matrix2D.Scaling(operation.X, operation.Y);

                case TransformKind.Rotate:
                    return Matrix2D.Rotation(operation.X);

                case TransformKind.Skew:
                    return Matrix2D.Skewing(operation.X, operation.Y);

                default:
                    return Matrix2D.Identity;
            }
        }

        private static float Resolve(SizeUnit unit, float value, float extent)
            => unit == SizeUnit.Percents ? extent * value / 100f : value;
    }
}
