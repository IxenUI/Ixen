namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class SpaceStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Space;

        public SizeStyleDescriptor Top { get; set; }
        public SizeStyleDescriptor Right { get; set; }
        public SizeStyleDescriptor Bottom { get; set; }
        public SizeStyleDescriptor Left { get; set; }
    }
}
