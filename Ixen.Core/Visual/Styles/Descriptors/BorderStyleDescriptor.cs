namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum BorderType
    {
        Outer,
        Inner,
        Center
    }

    public class BorderStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Border;
        public string Color { get; set; } = "#000000";
        public float Thickness { get; set; } = 0;
        public BorderType Type { get; set; } = BorderType.Outer;


        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(BorderStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Color)} = \"{Color}\", " +
                    $"{nameof(Thickness)} = {Thickness}, " +
                    $"{nameof(Type)} = {Type} " +
                "}";
    }
}
