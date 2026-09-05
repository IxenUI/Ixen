using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum FilterKind
    {
        Blur,
        Grayscale,
        Sepia,
        Saturate,
        Invert,
        Brightness,
        Contrast,
        HueRotate,
        Opacity,
        DropShadow
    }

    public class FilterOperation
    {
        public FilterKind Kind { get; set; }
        public float Value { get; set; }
        public Shadow Shadow { get; set; }

        internal FilterOperation Copy()
            => new FilterOperation
            {
                Kind = Kind,
                Value = Value,
                Shadow = Shadow?.Copy()
            };

        internal bool SameAs(FilterOperation other)
            => Kind == other.Kind
                && Value == other.Value
                && (Shadow == null
                    ? other.Shadow == null
                    : Shadow.SameAs(other.Shadow));

        internal string ToSource()
            => $"new {nameof(FilterOperation)} {{ "
                + $"{nameof(Kind)} = {nameof(FilterKind)}.{Kind}, "
                + $"{nameof(Value)} = {Value.ToString("R", CultureInfo.InvariantCulture)}f"
                + (Shadow == null ? string.Empty : $", {nameof(Shadow)} = {Shadow.ToSource()}")
                + " }";
    }

    public class FilterStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.FILTER;

        public List<FilterOperation> Operations { get; set; } = new List<FilterOperation>();

        internal int Count => Operations == null ? 0 : Operations.Count;

        internal bool IsDeclared => Count > 0;

        internal void Set(FilterStyleDescriptor other)
        {
            Operations.Clear();

            for (int index = 0; index < other.Count; index++)
            {
                Operations.Add(other.Operations[index].Copy());
            }
        }

        internal FilterStyleDescriptor Snapshot()
        {
            var copy = new FilterStyleDescriptor();

            for (int index = 0; index < Count; index++)
            {
                copy.Operations.Add(Operations[index].Copy());
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
                if (!Operations[index].SameAs(other.Operations[index]))
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
