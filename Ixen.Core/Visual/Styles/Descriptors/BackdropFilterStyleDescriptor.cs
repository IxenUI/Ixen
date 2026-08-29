using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BackdropFilterStyleDescriptor : FilterStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BACKDROP_FILTER;

        internal override string ToSource()
        {
            var sb = new StringBuilder();

            sb.Append($"new {nameof(BackdropFilterStyleDescriptor)} {{ ");
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
