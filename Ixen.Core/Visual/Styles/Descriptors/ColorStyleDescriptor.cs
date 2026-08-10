namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ColorStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.COLOR;

        public string Value { get; set; } = null;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ColorStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
