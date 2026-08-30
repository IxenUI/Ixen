namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ObjectPositionStyleDescriptor : StyleDescriptor
    {
        internal const float CENTRE = 0.5f;

        internal override string Identifier => StyleIdentifier.OBJECT_POSITION;

        public float X { get; set; } = CENTRE;
        public float Y { get; set; } = CENTRE;

        internal bool IsDefault => X == CENTRE && Y == CENTRE;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ObjectPositionStyleDescriptor)} " +
                "{ " +
                    $"{nameof(X)} = {X.ToString("R", global::System.Globalization.CultureInfo.InvariantCulture)}f, " +
                    $"{nameof(Y)} = {Y.ToString("R", global::System.Globalization.CultureInfo.InvariantCulture)}f " +
                "}";
    }
}
