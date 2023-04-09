namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class SpaceStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Space;

        public SizeStyleDescriptor Top { get; set; } = new();
        public SizeStyleDescriptor Right { get; set; } = new();
        public SizeStyleDescriptor Bottom { get; set; } = new();
        public SizeStyleDescriptor Left { get; set; } = new();
    }
}
