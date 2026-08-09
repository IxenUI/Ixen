namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class FontFamilyStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.FONT_FAMILY;

        public string Value { get; set; } = null;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(FontFamilyStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {(Value == null ? "null" : $"\"{Value}\"")} " +
                "}";
    }
}
