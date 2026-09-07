namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum CursorKind
    {
        Unset,
        Default,
        Hand,
        Text,
        Wait,
        Crosshair,
        ResizeHorizontal,
        ResizeVertical,
        ResizeDiagonalUp,
        ResizeDiagonalDown,
        Move,
        NotAllowed,
        Help,
        Progress,
        Hidden,
        Image
    }

    public class CursorStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.CURSOR;

        public CursorKind Value { get; set; } = CursorKind.Unset;

        public string Image { get; set; }

        public int HotspotX { get; set; }

        public int HotspotY { get; set; }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(CursorStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(CursorKind)}.{Value}" +
                    (string.IsNullOrWhiteSpace(Image)
                        ? " " : $", {nameof(Image)} = \"{Image}\", " +
                            $"{nameof(HotspotX)} = {HotspotX}, " +
                            $"{nameof(HotspotY)} = {HotspotY} ") +
                "}";
    }
}
