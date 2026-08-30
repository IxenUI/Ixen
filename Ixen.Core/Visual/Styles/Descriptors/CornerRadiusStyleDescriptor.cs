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

        internal float ScaleFor(float width, float height)
        {
            float scale = 1f;

            scale = Reduce(scale, width, TopLeft + TopRight);
            scale = Reduce(scale, height, TopRight + BottomRight);
            scale = Reduce(scale, width, BottomRight + BottomLeft);
            scale = Reduce(scale, height, TopLeft + BottomLeft);

            return scale;
        }

        private static float Reduce(float scale, float side, float sum)
        {
            if (sum <= 0 || side < 0)
            {
                return scale;
            }

            float allowed = side / sum;

            return allowed < scale ? allowed : scale;
        }

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
