namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum Visibility
    {
        Visible,
        Hidden
    }

    public class VisibilityStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.VISIBILITY;

        public Visibility Value { get; set; } = Visibility.Visible;

        internal bool IsDeclared => Value == Visibility.Hidden;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(VisibilityStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(Visibility)}.{Value} " +
                "}";
    }
}
