namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum TextAlign
    {
        Left,
        Center,
        Right
    }

    public class TextAlignStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TEXT_ALIGN;

        public TextAlign Value { get; set; } = TextAlign.Left;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TextAlignStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(TextAlign)}.{Value} " +
                "}";
    }
}
