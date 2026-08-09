namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class LayoutIndexStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BOOLEAN;

        public int IndexFrom { get; set; } = 0;
        public int IndexTo { get; set; } = 0;
    }
}
