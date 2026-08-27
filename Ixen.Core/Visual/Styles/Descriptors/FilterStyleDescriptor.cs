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
