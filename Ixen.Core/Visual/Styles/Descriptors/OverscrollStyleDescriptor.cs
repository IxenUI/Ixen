namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum OverscrollKind
    {
        Unset,
        Auto,
        Contain,
        None
    }

    public class OverscrollStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.OVERSCROLL_BEHAVIOR;

        public OverscrollKind X { get; set; } = OverscrollKind.Unset;

        public OverscrollKind Y { get; set; } = OverscrollKind.Unset;

        internal bool IsDeclared
            => X != OverscrollKind.Unset || Y != OverscrollKind.Unset;

        internal bool Contains(bool horizontal)
        {
            OverscrollKind kind = horizontal ? X : Y;

            return kind == OverscrollKind.Contain || kind == OverscrollKind.None;
        }

        internal bool Bounces(bool horizontal)
            => (horizontal ? X : Y) != OverscrollKind.None;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(OverscrollStyleDescriptor)} " +
                "{ " +
                    $"{nameof(X)} = {nameof(OverscrollKind)}.{X}, " +
                    $"{nameof(Y)} = {nameof(OverscrollKind)}.{Y} " +
                "}";
    }
}
