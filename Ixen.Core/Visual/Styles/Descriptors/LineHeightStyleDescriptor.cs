namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum LineHeightKind
    {
        Unset,
        Normal,
        Multiplier,
        Pixels,
        Percents
    }

    public class LineHeightStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.LINE_HEIGHT;

        public LineHeightKind Kind { get; set; } = LineHeightKind.Unset;
        public float Value { get; set; }

        internal bool IsDeclared => Kind != LineHeightKind.Unset;

        internal float Resolve(float fontSize)
        {
            switch (Kind)
            {
                case LineHeightKind.Multiplier:
                    return fontSize * Value;

                case LineHeightKind.Pixels:
                    return Value;

                case LineHeightKind.Percents:
                    return fontSize * Value / 100f;

                default:
                    return 0;
            }
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(LineHeightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Kind)} = {nameof(LineHeightKind)}.{Kind}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
