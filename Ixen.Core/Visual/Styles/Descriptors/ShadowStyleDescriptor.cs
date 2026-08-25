namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ShadowStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BOX_SHADOW;

        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float Blur { get; set; }
        public float Spread { get; set; }
        public string Color { get; set; }

        internal bool IsDeclared => Color != null;

        internal void Set(ShadowStyleDescriptor other)
        {
            OffsetX = other.OffsetX;
            OffsetY = other.OffsetY;
            Blur = other.Blur;
            Spread = other.Spread;
            Color = other.Color;
        }

        internal string Fields()
            => $"{nameof(OffsetX)} = {SourceOf(OffsetX)}, "
                + $"{nameof(OffsetY)} = {SourceOf(OffsetY)}, "
                + $"{nameof(Blur)} = {SourceOf(Blur)}, "
                + $"{nameof(Spread)} = {SourceOf(Spread)}, "
                + $"{nameof(Color)} = {SourceOf(Color)}";
    }
}
