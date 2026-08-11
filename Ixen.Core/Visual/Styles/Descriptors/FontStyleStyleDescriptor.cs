namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum FontStyle
    {
        Normal,
        Italic
    }

    public class FontStyleStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.FONT_STYLE;

        public FontStyle Value { get; set; } = FontStyle.Normal;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(FontStyleStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(FontStyle)}.{Value} " +
                "}";
    }
}
