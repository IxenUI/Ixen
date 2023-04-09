namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class HeightStyleDescriptor : SizeStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Height;

        public void Set(SizeStyleDescriptor sizeDescriptor)
        {
            Unit = sizeDescriptor.Unit;
            Value = sizeDescriptor.Value;
        }
    }
}
