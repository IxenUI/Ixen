namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum TextAlign
    {
        Left,
        Center,
        Right
    }

    public enum TextVAlign
    {
        Top,
        Middle,
        Bottom
    }

    public class TextAlignStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TEXT_ALIGN;

        public TextAlign Horizontal { get; set; } = TextAlign.Left;
        public TextVAlign Vertical { get; set; } = TextVAlign.Top;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TextAlignStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Horizontal)} = {nameof(TextAlign)}.{Horizontal}, " +
                    $"{nameof(Vertical)} = {nameof(TextVAlign)}.{Vertical} " +
                "}";
    }
}
