namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum FontWeight
    {
        Normal,
        Bold
    }

    public class FontWeightStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.FONT_WEIGHT;

        public FontWeight Value { get; set; } = FontWeight.Normal;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(FontWeightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(FontWeight)}.{Value} " +
                "}";
    }
}
