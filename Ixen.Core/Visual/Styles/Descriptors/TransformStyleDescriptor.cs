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
