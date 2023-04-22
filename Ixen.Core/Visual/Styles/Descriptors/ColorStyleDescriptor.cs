namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ColorStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Color;

        public string Value { get; set; } = null;
    }
}
