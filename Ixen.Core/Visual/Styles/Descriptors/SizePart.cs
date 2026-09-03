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
            => $"new {nameof(SizePart)} {{ {nameof(Value)} = {StyleDescriptor.SourceOf(Value)}, "
                + $"{nameof(Offset)} = {StyleDescriptor.SourceOf(Offset)} }}";
    }
}
