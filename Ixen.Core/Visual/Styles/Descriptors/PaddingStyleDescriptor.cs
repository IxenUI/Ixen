namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class PaddingStyleDescriptor : MarginStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Padding;

        public void Set(SpaceStyleDescriptor spaceDescriptor)
        {
            Top = spaceDescriptor.Top;
            Right = spaceDescriptor.Right;
            Bottom = spaceDescriptor.Bottom;
            Left = spaceDescriptor.Left;
        }
    }
}
