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
        ResizeVertical
    }

    public class CursorStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.CURSOR;

        public CursorKind Value { get; set; } = CursorKind.Unset;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(CursorStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(CursorKind)}.{Value} " +
                "}";
    }
}
