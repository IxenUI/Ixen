namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BackgroundLayer
    {
        public const float UNSET_POSITION = -1f;

        public string ImageUrl { get; set; }
        public Gradient Gradient { get; set; }
        public bool RepeatX { get; set; } = false;
        public bool RepeatY { get; set; } = false;
        public ObjectFit Fit { get; set; } = ObjectFit.None;
        public float PositionX { get; set; } = UNSET_POSITION;
        public float PositionY { get; set; } = UNSET_POSITION;

        public bool IsScaled => Fit != ObjectFit.None;

        public bool HasPosition => PositionX >= 0f || PositionY >= 0f;

        public float AnchorX => PositionX >= 0f ? PositionX : DefaultAnchor;
        public float AnchorY => PositionY >= 0f ? PositionY : DefaultAnchor;

        private float DefaultAnchor => IsScaled ? 0.5f : 0f;

        internal bool IsEmpty => string.IsNullOrWhiteSpace(ImageUrl) && Gradient == null;

        internal string ToSource()
            => $"new {nameof(BackgroundLayer)} " +
                "{ " +
                    (string.IsNullOrWhiteSpace(ImageUrl)
                        ? "" : $"{nameof(ImageUrl)} = \"{ImageUrl}\", ") +
                    (Gradient == null ? "" : $"{nameof(Gradient)} = {Gradient.ToSource()}, ") +
                    $"{nameof(RepeatX)} = {(RepeatX ? "true" : "false")}, " +
                    $"{nameof(RepeatY)} = {(RepeatY ? "true" : "false")}, " +
                    $"{nameof(Fit)} = {nameof(ObjectFit)}.{Fit}, " +
                    $"{nameof(PositionX)} = {Source(PositionX)}, " +
                    $"{nameof(PositionY)} = {Source(PositionY)} " +
                "}";

        private static string Source(float value)
            => value.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + "f";
    }
}
