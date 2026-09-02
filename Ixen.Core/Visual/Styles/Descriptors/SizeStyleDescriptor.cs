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
        Weight,
        Content
    }

    public class SizeStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.SIZE;

        public SizeUnit Unit { get; set; } = SizeUnit.Unset; // by default, is equivalent to Weight, but does not override inherited value
        public float Value { get; set; } = 1;
        public float Offset { get; set; } = 0;

        public void Set(SizeStyleDescriptor other)
        {
            Unit = other.Unit;
            Value = other.Value;
            Offset = other.Offset;
        }

        internal float Of(float available)
            => (available / 100f) * Value + Offset;
    }
}
