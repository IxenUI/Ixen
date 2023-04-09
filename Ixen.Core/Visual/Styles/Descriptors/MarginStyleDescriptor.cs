namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class MarginStyleDescriptor : SpaceStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Margin;

        public void Set(SpaceStyleDescriptor spaceDescriptor)
        {
            Top = spaceDescriptor.Top;
            Right = spaceDescriptor.Right;
            Bottom = spaceDescriptor.Bottom;
            Left = spaceDescriptor.Left;
        }
    }
}
