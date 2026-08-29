namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum PointerEvents
    {
        Unset,
        Auto,
        None
    }

    public class PointerEventsStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.POINTER_EVENTS;

        public PointerEvents Value { get; set; } = PointerEvents.Unset;

        internal bool IsDeclared => Value != PointerEvents.Unset;

        internal bool Blocks => Value == PointerEvents.None;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(PointerEventsStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(PointerEvents)}.{Value} " +
                "}";
    }
}
