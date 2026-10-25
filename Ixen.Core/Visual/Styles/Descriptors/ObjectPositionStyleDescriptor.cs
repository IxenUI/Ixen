namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ObjectPositionStyleDescriptor : StyleDescriptor
    {
        internal const float CENTRE = 0.5f;

        internal override string Identifier => StyleIdentifier.OBJECT_POSITION;

        public float X { get; set; } = CENTRE;
        public float Y { get; set; } = CENTRE;
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }

        internal bool IsDefault => X == CENTRE && Y == CENTRE && OffsetX == 0 && OffsetY == 0;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ObjectPositionStyleDescriptor)} " +
                "{ " +
                    $"{nameof(X)} = {Source(X)}, " +
                    $"{nameof(Y)} = {Source(Y)}, " +
                    $"{nameof(OffsetX)} = {Source(OffsetX)}, " +
                    $"{nameof(OffsetY)} = {Source(OffsetY)} " +
                "}";

        private static string Source(float value)
            => value.ToString("R", global::System.Globalization.CultureInfo.InvariantCulture) + "f";
    }
}
