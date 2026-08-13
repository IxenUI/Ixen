namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class OffsetStyleDescriptor : SizeStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.OFFSET;

        public void Set(SizeStyleDescriptor sizeDescriptor)
        {
            Unit = sizeDescriptor.Unit;
            Value = sizeDescriptor.Value;
        }
    }
}
