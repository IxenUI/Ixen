using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum FilterKind
    {
        Blur
    }

    public class FilterOperation
    {
        public FilterKind Kind { get; set; }
        public float Value { get; set; }

        internal string ToSource()
            => $"new {nameof(FilterOperation)} {{ "
                + $"{nameof(Kind)} = {nameof(FilterKind)}.{Kind}, "
                + $"{nameof(Value)} = {Value.ToString("R", CultureInfo.InvariantCulture)}f }}";
    }

    public class FilterStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.FILTER;

        public List<FilterOperation> Operations { get; set; } = new List<FilterOperation>();

        internal int Count => Operations == null ? 0 : Operations.Count;

        internal bool IsDeclared => Count > 0;

        internal FilterStyleDescriptor Snapshot()
        {
            var copy = new FilterStyleDescriptor();

            for (int index = 0; index < Count; index++)
            {
                FilterOperation operation = Operations[index];

                copy.Operations.Add(new FilterOperation
                {
                    Kind = operation.Kind,
                    Value = operation.Value
                });
            }

            return copy;
        }

        internal bool SameAs(FilterStyleDescriptor other)
        {
            if (other == null || Count != other.Count)
            {
                return false;
            }

            for (int index = 0; index < Count; index++)
            {
                if (Operations[index].Kind != other.Operations[index].Kind
                    || Operations[index].Value != other.Operations[index].Value)
                {
                    return false;
                }
            }

            return true;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
        {
            var sb = new StringBuilder();

            sb.Append($"new {nameof(FilterStyleDescriptor)} {{ ");
            sb.Append($"{nameof(Operations)} = new global::System.Collections.Generic.List<{nameof(FilterOperation)}> {{ ");

            for (int index = 0; index < Count; index++)
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
