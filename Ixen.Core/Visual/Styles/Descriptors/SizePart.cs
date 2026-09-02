using System.Globalization;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum SizeFunction
    {
        None,
        Min,
        Max,
        Clamp
    }

    public class SizePart
    {
        public float Value { get; set; }
        public float Offset { get; set; }

        internal float Of(float available)
            => (available / 100f) * Value + Offset;

        internal string ToSource()
            => $"new {nameof(SizePart)} {{ {nameof(Value)} = {Source(Value)}, "
                + $"{nameof(Offset)} = {Source(Offset)} }}";

        private static string Source(float value)
            => value.ToString("R", CultureInfo.InvariantCulture) + "f";
    }
}
