namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class TransformOriginStyleDescriptor : StyleDescriptor
    {
        internal const float CENTRE = 50f;

        internal override string Identifier => StyleIdentifier.TRANSFORM_ORIGIN;

        public SizeUnit XUnit { get; set; } = SizeUnit.Percents;
        public float X { get; set; } = CENTRE;
        public SizeUnit YUnit { get; set; } = SizeUnit.Percents;
        public float Y { get; set; } = CENTRE;

        internal bool IsDefault
            => XUnit == SizeUnit.Percents && X == CENTRE
            && YUnit == SizeUnit.Percents && Y == CENTRE;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TransformOriginStyleDescriptor)} " +
                "{ " +
                    $"{nameof(XUnit)} = {nameof(SizeUnit)}.{XUnit}, " +
                    $"{nameof(X)} = {SourceOf(X)}, " +
                    $"{nameof(YUnit)} = {nameof(SizeUnit)}.{YUnit}, " +
                    $"{nameof(Y)} = {SourceOf(Y)} " +
                "}";
    }
}
