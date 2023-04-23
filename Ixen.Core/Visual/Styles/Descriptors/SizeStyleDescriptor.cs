namespace Ixen.Core.Visual.Styles.Descriptors
{
    internal enum SizeStyleDescriptorType
    {
        Width,
        Height
    }

    public enum SizeUnit
    {
        Undefined,
        Pixels,
        Percents,
        Weight
    }

    public class SizeStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Size;

        public SizeUnit Unit { get; set; } = SizeUnit.Pixels;
        public float Value { get; set; } = 0;
    }
}
