namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class LayoutIndexStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Boolean;

        public int IndexFrom { get; set; } = 0;
        public int IndexTo { get; set; } = 0;
    }
}
