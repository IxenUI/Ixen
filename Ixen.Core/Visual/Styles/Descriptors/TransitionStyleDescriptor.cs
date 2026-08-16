using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class TransitionStyleDescriptor : StyleDescriptor
    {
        public const string ALL = "all";

        internal override string Identifier => StyleIdentifier.TRANSITION;

        public Dictionary<string, int> Durations { get; set; } = new Dictionary<string, int>();

        public int DurationOf(string property)
        {
            if (Durations.TryGetValue(property, out int duration))
            {
                return duration;
            }

            return Durations.TryGetValue(ALL, out int all) ? all : 0;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
        {
            var sb = new StringBuilder();

            sb.Append($"new {nameof(TransitionStyleDescriptor)} {{ {nameof(Durations)} = new global::System.Collections.Generic.Dictionary<string, int> {{ ");

            bool first = true;

            foreach (KeyValuePair<string, int> entry in Durations)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append($"{{ {SourceOf(entry.Key)}, {entry.Value} }}");
                first = false;
            }

            return sb.Append(" } }").ToString();
        }
    }
}
