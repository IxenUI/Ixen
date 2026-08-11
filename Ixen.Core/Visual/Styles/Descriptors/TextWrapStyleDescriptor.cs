namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum TextWrap
    {
        Wrap,
        NoWrap
    }

    public class TextWrapStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TEXT_WRAP;

        public TextWrap Value { get; set; } = TextWrap.Wrap;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TextWrapStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(TextWrap)}.{Value} " +
                "}";
    }
}
