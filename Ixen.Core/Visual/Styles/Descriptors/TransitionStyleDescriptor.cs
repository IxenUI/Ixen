using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public struct TransitionSpec
    {
        public int Duration { get; set; }
        public int Delay { get; set; }
        public EasingKind Easing { get; set; }
    }

    public class TransitionStyleDescriptor : StyleDescriptor
    {
        public const string ALL = "all";

        internal override string Identifier => StyleIdentifier.TRANSITION;

        public Dictionary<string, TransitionSpec> Specs { get; set; } = new Dictionary<string, TransitionSpec>();

        public TransitionSpec SpecOf(string property)
        {
            if (Specs.TryGetValue(property, out TransitionSpec spec))
            {
                return spec;
            }

            return Specs.TryGetValue(ALL, out TransitionSpec all) ? all : default;
        }

        public int DurationOf(string property) => SpecOf(property).Duration;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
        {
            var sb = new StringBuilder();

            sb.Append($"new {nameof(TransitionStyleDescriptor)} {{ {nameof(Specs)} = new global::System.Collections.Generic.Dictionary<string, global::Ixen.Core.Visual.Styles.Descriptors.{nameof(TransitionSpec)}> {{ ");

            bool first = true;

            foreach (KeyValuePair<string, TransitionSpec> entry in Specs)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append($"{{ {SourceOf(entry.Key)}, new global::Ixen.Core.Visual.Styles.Descriptors.{nameof(TransitionSpec)} {{ {nameof(TransitionSpec.Duration)} = {entry.Value.Duration}, {nameof(TransitionSpec.Delay)} = {entry.Value.Delay}, {nameof(TransitionSpec.Easing)} = global::Ixen.Core.Visual.Styles.{nameof(EasingKind)}.{entry.Value.Easing} }} }}");
                first = false;
            }

            return sb.Append(" } }").ToString();
        }
    }
}
