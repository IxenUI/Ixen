namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class AspectRatioStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.ASPECT_RATIO;

        public float Ratio { get; set; }

        internal bool IsDeclared => Ratio > 0;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(AspectRatioStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Ratio)} = {SourceOf(Ratio)} " +
                "}";
    }
}
