namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum BorderType
    {
        Outer,
        Inner,
        Center
    }

    public enum BorderStyle
    {
        Solid,
        Dashed,
        Dotted
    }

    public class BorderStyleDescriptor : StyleDescriptor
    {
        public const float UNSET_THICKNESS = -1f;

        internal override string Identifier => StyleIdentifier.BORDER;
        public string Color { get; set; } = "#000000";
        public float Thickness { get; set; } = 0;
        public BorderType Type { get; set; } = BorderType.Outer;
        public BorderStyle Style { get; set; } = BorderStyle.Solid;

        public float TopThickness { get; set; } = UNSET_THICKNESS;
        public float RightThickness { get; set; } = UNSET_THICKNESS;
        public float BottomThickness { get; set; } = UNSET_THICKNESS;
        public float LeftThickness { get; set; } = UNSET_THICKNESS;

        public string TopColor { get; set; }
        public string RightColor { get; set; }
        public string BottomColor { get; set; }
        public string LeftColor { get; set; }

        internal float Top => TopThickness < 0 ? Thickness : TopThickness;
        internal float Right => RightThickness < 0 ? Thickness : RightThickness;
        internal float Bottom => BottomThickness < 0 ? Thickness : BottomThickness;
        internal float Left => LeftThickness < 0 ? Thickness : LeftThickness;

        internal string ColorTop => TopColor ?? Color;
        internal string ColorRight => RightColor ?? Color;
        internal string ColorBottom => BottomColor ?? Color;
        internal string ColorLeft => LeftColor ?? Color;

        internal bool IsUniform => Top == Right && Right == Bottom && Bottom == Left;

        internal bool IsOneColor
            => ColorTop == ColorRight && ColorRight == ColorBottom && ColorBottom == ColorLeft;

        internal bool HasBorder => Top > 0 || Right > 0 || Bottom > 0 || Left > 0;

        public void SetThickness(float top, float right, float bottom, float left)
        {
            TopThickness = top;
            RightThickness = right;
            BottomThickness = bottom;
            LeftThickness = left;
        }

        public void SetColors(string top, string right, string bottom, string left)
        {
            TopColor = top;
            RightColor = right;
            BottomColor = bottom;
            LeftColor = left;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(BorderStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Color)} = {SourceOf(Color)}, " +
                    $"{nameof(Thickness)} = {SourceOf(Thickness)}, " +
                    $"{nameof(TopThickness)} = {SourceOf(TopThickness)}, " +
                    $"{nameof(RightThickness)} = {SourceOf(RightThickness)}, " +
                    $"{nameof(BottomThickness)} = {SourceOf(BottomThickness)}, " +
                    $"{nameof(LeftThickness)} = {SourceOf(LeftThickness)}, " +
                    $"{nameof(TopColor)} = {SourceOf(TopColor)}, " +
                    $"{nameof(RightColor)} = {SourceOf(RightColor)}, " +
                    $"{nameof(BottomColor)} = {SourceOf(BottomColor)}, " +
                    $"{nameof(LeftColor)} = {SourceOf(LeftColor)}, " +
                    $"{nameof(Type)} = {nameof(BorderType)}.{Type}, " +
                    $"{nameof(Style)} = {nameof(BorderStyle)}.{Style} " +
                "}";
    }
}
