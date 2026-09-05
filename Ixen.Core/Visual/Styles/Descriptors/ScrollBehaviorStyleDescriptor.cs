namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum ScrollBehavior
    {
        Auto,
        Smooth
    }

    public class ScrollBehaviorStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.SCROLL_BEHAVIOR;

        public ScrollBehavior Value { get; set; } = ScrollBehavior.Auto;

        internal bool IsDeclared => Value == ScrollBehavior.Smooth;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ScrollBehaviorStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(ScrollBehavior)}.{Value} " +
                "}";
    }
}
