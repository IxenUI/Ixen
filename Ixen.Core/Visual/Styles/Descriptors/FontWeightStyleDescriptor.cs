namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum FontWeight
    {
        Unset = 0,
        Thin = 100,
        ExtraLight = 200,
        Light = 300,
        Normal = 400,
        Medium = 500,
        SemiBold = 600,
        Bold = 700,
        ExtraBold = 800,
        Black = 900
    }

    public class FontWeightStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.FONT_WEIGHT;

        public FontWeight Value { get; set; } = FontWeight.Unset;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(FontWeightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(FontWeight)}.{Value} " +
                "}";
    }
}
