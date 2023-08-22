namespace Ixen.Core.Visual.Styles.Descriptors
{
    internal enum SizeStyleDescriptorType
    {
        Width,
        Height
    }

    public enum SizeUnit
    {
        Unset, 
        Pixels,
        Percents,
        Weight
    }

    public class SizeStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Size;

        public SizeUnit Unit { get; set; } = SizeUnit.Unset; // by default, is equivalent to Weight, but does not override inherited value
        public float Value { get; set; } = 1;
    }
}
