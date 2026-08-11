namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum TextVAlign
    {
        Top,
        Middle,
        Bottom
    }

    public class TextVAlignStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TEXT_VALIGN;

        public TextVAlign Value { get; set; } = TextVAlign.Top;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TextVAlignStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(TextVAlign)}.{Value} " +
                "}";
    }
}
