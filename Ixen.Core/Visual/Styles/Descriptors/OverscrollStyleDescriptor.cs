namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum OverscrollKind
    {
        Unset,
        Auto,
        Contain
    }

    public class OverscrollStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.OVERSCROLL_BEHAVIOR;

        public OverscrollKind Value { get; set; } = OverscrollKind.Unset;

        internal bool Contains => Value == OverscrollKind.Contain;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(OverscrollStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(OverscrollKind)}.{Value} " +
                "}";
    }
}
