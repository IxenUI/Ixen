namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BoundStyleDescriptor : SizeStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BOUND;

        internal bool IsDeclared => Unit == SizeUnit.Pixels;

        public void Set(SizeStyleDescriptor sizeDescriptor)
        {
            Unit = sizeDescriptor.Unit;
            Value = sizeDescriptor.Value;
        }
    }
}
