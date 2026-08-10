namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class CornerRadiusStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.CORNER_RADIUS;

        public float TopLeft { get; set; }
        public float TopRight { get; set; }
        public float BottomRight { get; set; }
        public float BottomLeft { get; set; }

        internal bool HasRadius
            => TopLeft > 0 || TopRight > 0 || BottomRight > 0 || BottomLeft > 0;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(CornerRadiusStyleDescriptor)} " +
                "{ " +
                    $"{nameof(TopLeft)} = {SourceOf(TopLeft)}, " +
                    $"{nameof(TopRight)} = {SourceOf(TopRight)}, " +
                    $"{nameof(BottomRight)} = {SourceOf(BottomRight)}, " +
                    $"{nameof(BottomLeft)} = {SourceOf(BottomLeft)} " +
                "}";
    }
}
