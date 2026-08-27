using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum GradientKind
    {
        Linear,
        Radial
    }

    public class GradientStop
    {
        public string Color { get; set; }
        public float Offset { get; set; } = UNSET_OFFSET;

        internal const float UNSET_OFFSET = -1f;

        internal bool HasOffset => Offset >= 0;
    }

    public class Gradient
    {
        public GradientKind Kind { get; set; }
        public float Angle { get; set; } = 180f;
        public List<GradientStop> Stops { get; set; } = new List<GradientStop>();

        internal Gradient Snapshot()
        {
            var copy = new Gradient { Kind = Kind, Angle = Angle };

            foreach (GradientStop stop in Stops)
            {
                copy.Stops.Add(new GradientStop { Color = stop.Color, Offset = stop.Offset });
            }

            return copy;
        }

        internal bool SameAs(Gradient other)
        {
            if (other == null || Kind != other.Kind || Angle != other.Angle
                || Stops.Count != other.Stops.Count)
            {
                return false;
            }

            for (int index = 0; index < Stops.Count; index++)
            {
                GradientStop mine = Stops[index];
                GradientStop theirs = other.Stops[index];

                if (mine.Color != theirs.Color || mine.Offset != theirs.Offset)
                {
                    return false;
                }
            }

            return true;
        }

        internal string ToSource()
        {
            var sb = new StringBuilder();

            sb.Append($"new {nameof(Gradient)} {{ ");
            sb.Append($"{nameof(Kind)} = {nameof(GradientKind)}.{Kind}, ");
            sb.Append($"{nameof(Angle)} = {Angle.ToString("R", CultureInfo.InvariantCulture)}f, ");
            sb.Append($"{nameof(Stops)} = new global::System.Collections.Generic.List<{nameof(GradientStop)}> {{ ");

            for (int i = 0; i < Stops.Count; i++)
            {
                GradientStop stop = Stops[i];

                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append($"new {nameof(GradientStop)} {{ ");
                sb.Append($"{nameof(GradientStop.Color)} = \"{stop.Color}\", ");
                sb.Append($"{nameof(GradientStop.Offset)} = {stop.Offset.ToString("R", CultureInfo.InvariantCulture)}f ");
                sb.Append("}");
            }

            sb.Append(" } }");

            return sb.ToString();
        }
    }
}
