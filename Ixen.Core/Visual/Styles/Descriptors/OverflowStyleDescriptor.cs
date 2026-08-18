namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum OverflowKind
    {
        Unset,
        Hidden,
        Scroll
    }

    public class OverflowStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.OVERFLOW;

        public OverflowKind Value { get; set; } = OverflowKind.Unset;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(OverflowStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(OverflowKind)}.{Value} " +
                "}";
    }
}
