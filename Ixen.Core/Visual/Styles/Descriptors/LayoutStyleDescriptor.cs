namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum LayoutType
    {
        Column,
        Row,
        Grid,
        Absolute,
        Fixed,
        Dock
    }

    public class LayoutStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Layout;

        public LayoutType Type { get; set; } = LayoutType.Column;
    }
}
