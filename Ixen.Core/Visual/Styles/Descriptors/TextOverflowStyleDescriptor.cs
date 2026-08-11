namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum TextOverflow
    {
        Clip,
        Ellipsis
    }

    public class TextOverflowStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TEXT_OVERFLOW;

        public TextOverflow Value { get; set; } = TextOverflow.Clip;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TextOverflowStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(TextOverflow)}.{Value} " +
                "}";
    }
}
