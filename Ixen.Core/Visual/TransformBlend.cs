using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    internal class TransformBlend
    {
        private TransformStyleDescriptor _result;
        private readonly TransformOperation _fromNeutral = new TransformOperation();
        private readonly TransformOperation _toNeutral = new TransformOperation();

        internal bool Produced(TransformStyleDescriptor descriptor)
            => ReferenceEquals(descriptor, _result);

        internal TransformStyleDescriptor Of(TransformStyleDescriptor from,
            TransformStyleDescriptor to, float eased)
        {
            List<TransformOperation> into = Buffer(
                Math.Max(from == null ? 0 : from.Count, to == null ? 0 : to.Count));

            for (int index = 0; index < into.Count; index++)
            {
                TransformOperation shape = index < (to == null ? 0 : to.Count)
                    ? to.Operations[index]
                    : from.Operations[index];

                TransformOperation start = At(from, index, shape, _fromNeutral);
                TransformOperation end = At(to, index, shape, _toNeutral);
                TransformOperation target = into[index];

                target.Kind = shape.Kind;

                target.XUnit = UnitOf(start.XUnit, start.X, end.XUnit, end.X);
                target.X = start.X + (end.X - start.X) * eased;

                target.YUnit = UnitOf(start.YUnit, start.Y, end.YUnit, end.Y);
                target.Y = start.Y + (end.Y - start.Y) * eased;
            }

            return _result;
        }

        internal TransformStyleDescriptor CopyOf(TransformStyleDescriptor source)
        {
            List<TransformOperation> into = Buffer(source == null ? 0 : source.Count);

            for (int index = 0; index < into.Count; index++)
            {
                TransformOperation from = source.Operations[index];
                TransformOperation target = into[index];

                target.Kind = from.Kind;
                target.XUnit = from.XUnit;
                target.X = from.X;
                target.YUnit = from.YUnit;
                target.Y = from.Y;
            }

            return _result;
        }

        private List<TransformOperation> Buffer(int count)
        {
            if (_result == null)
            {
                _result = new TransformStyleDescriptor();
            }

            List<TransformOperation> operations = _result.Operations;

            while (operations.Count > count)
            {
                operations.RemoveAt(operations.Count - 1);
            }

            while (operations.Count < count)
            {
                operations.Add(new TransformOperation());
            }

            return operations;
        }

        private static SizeUnit UnitOf(SizeUnit fromUnit, float fromValue,
            SizeUnit toUnit, float toValue)
        {
            if (fromUnit == toUnit || toValue != 0)
            {
                return toUnit;
            }

            return fromValue != 0 ? fromUnit : toUnit;
        }

        private static TransformOperation At(TransformStyleDescriptor source, int index,
            TransformOperation shape, TransformOperation neutral)
        {
            if (source != null && index < source.Count)
            {
                return source.Operations[index];
            }

            float value = TransformOperation.IdentityValue(shape.Kind);

            neutral.Kind = shape.Kind;
            neutral.XUnit = shape.XUnit;
            neutral.X = value;
            neutral.YUnit = shape.YUnit;
            neutral.Y = value;

            return neutral;
        }
    }
}
