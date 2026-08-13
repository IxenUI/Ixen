namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum DockSide
    {
        Fill,
        Left,
        Top,
        Right,
        Bottom
    }

    public class DockStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.DOCK;

        public DockSide Side { get; set; } = DockSide.Fill;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(DockStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Side)} = {nameof(DockSide)}.{Side} " +
                "}";
    }
}
