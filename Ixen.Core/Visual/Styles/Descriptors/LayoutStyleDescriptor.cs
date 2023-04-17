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

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(LayoutStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Type)} = {nameof(LayoutType)}.{Type} " +
                "}";
    }
}
