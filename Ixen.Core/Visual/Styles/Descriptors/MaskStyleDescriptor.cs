namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class MaskStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Mask;

        public bool Top { get; set; } = false;
        public bool Bottom { get; set; } = false;
        public bool Right { get; set; } = false;
        public bool Left { get; set; } = false;
    }
}
