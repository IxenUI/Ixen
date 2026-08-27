using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum TransformKind
    {
        Translate,
        Scale,
        Rotate,
        Skew
    }

    public class TransformOperation
    {
        public TransformKind Kind { get; set; }
        public SizeUnit XUnit { get; set; } = SizeUnit.Pixels;
        public float X { get; set; }
        public SizeUnit YUnit { get; set; } = SizeUnit.Pixels;
        public float Y { get; set; }

        internal static float IdentityValue(TransformKind kind)
            => kind == TransformKind.Scale ? 1f : 0f;

        internal string ToSource()
        {
            var sb = new StringBuilder();

            sb.Append($"new {nameof(TransformOperation)} {{ ");
            sb.Append($"{nameof(Kind)} = {nameof(TransformKind)}.{Kind}, ");
            sb.Append($"{nameof(XUnit)} = {nameof(SizeUnit)}.{XUnit}, ");
            sb.Append($"{nameof(X)} = {X.ToString("R", CultureInfo.InvariantCulture)}f, ");
            sb.Append($"{nameof(YUnit)} = {nameof(SizeUnit)}.{YUnit}, ");
            sb.Append($"{nameof(Y)} = {Y.ToString("R", CultureInfo.InvariantCulture)}f ");
            sb.Append("}");

            return sb.ToString();
        }
    }

    public class TransformStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TRANSFORM;

        public List<TransformOperation> Operations { get; set; } = new List<TransformOperation>();

        internal bool IsDeclared => Operations != null && Operations.Count > 0;

        internal int Count => Operations == null ? 0 : Operations.Count;

        internal bool Matches(TransformStyleDescriptor other)
        {
            if (other == null)
            {
                return Count == 0;
            }

            if (Count != other.Count)
            {
                return false;
            }

            for (int index = 0; index < Count; index++)
            {
                TransformOperation mine = Operations[index];
                TransformOperation theirs = other.Operations[index];

                if (mine.Kind != theirs.Kind
                    || mine.XUnit != theirs.XUnit || mine.X != theirs.X
                    || mine.YUnit != theirs.YUnit || mine.Y != theirs.Y)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool Compatible(TransformStyleDescriptor from, TransformStyleDescriptor to)
        {
            int fromCount = from == null ? 0 : from.Count;
            int toCount = to == null ? 0 : to.Count;

            if (fromCount == 0 || toCount == 0)
            {
                return true;
            }

            if (fromCount != toCount)
            {
                return false;
            }

            for (int index = 0; index < fromCount; index++)
            {
                TransformOperation first = from.Operations[index];
                TransformOperation second = to.Operations[index];

                if (first.Kind != second.Kind)
                {
                    return false;
                }

                if (first.Kind != TransformKind.Translate)
                {
                    continue;
                }

                if (!UnitsAgree(first.XUnit, first.X, second.XUnit, second.X)
                    || !UnitsAgree(first.YUnit, first.Y, second.YUnit, second.Y))
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool UnitsAgree(SizeUnit fromUnit, float fromValue,
            SizeUnit toUnit, float toValue)
            => fromUnit == toUnit || fromValue == 0 || toValue == 0;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
        {
            var sb = new StringBuilder();

            sb.Append($"new {nameof(TransformStyleDescriptor)} {{ ");
            sb.Append($"{nameof(Operations)} = new global::System.Collections.Generic.List<{nameof(TransformOperation)}> {{ ");

            for (int index = 0; index < Operations.Count; index++)
            {
                if (index > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(Operations[index].ToSource());
            }

            sb.Append(" } }");

            return sb.ToString();
        }
    }
}
