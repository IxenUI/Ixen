namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class OpacityStyleDescriptor : StyleDescriptor
    {
        internal const float OPAQUE = 1f;

        internal override string Identifier => StyleIdentifier.OPACITY;

        public float Value { get; set; } = OPAQUE;

        internal bool IsTransparent => Value < OPAQUE;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(OpacityStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
