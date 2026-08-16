namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class FontSizeStyleDescriptor : StyleDescriptor
    {
        internal const float DEFAULT_SIZE = 14;

        internal override string Identifier => StyleIdentifier.FONT_SIZE;

        public float Value { get; set; } = 0;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(FontSizeStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
