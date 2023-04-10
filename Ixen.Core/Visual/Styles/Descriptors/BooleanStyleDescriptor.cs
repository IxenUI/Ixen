namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BooleanStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Boolean;

        public bool Value { get; set; } = false;
    }
}
