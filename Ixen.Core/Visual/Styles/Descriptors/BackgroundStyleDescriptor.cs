using System;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BackgroundStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BACKGROUND;

        public const float UNSET_POSITION = -1f;

        public string Color { get; set; } = null;
        public string ImageUrl { get; set; }
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

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(BackgroundStyleDescriptor)} " +
                "{ " +
                    (string.IsNullOrWhiteSpace(Color) ? "" : $"{nameof(Color)} = {SourceOf(Color)}, ") +
                    (string.IsNullOrWhiteSpace(ImageUrl) ? "" : $"{nameof(ImageUrl)} = {SourceOf(ImageUrl)}, ") +
                    $"{nameof(RepeatX)} = {SourceOf(RepeatX)}, " +
                    $"{nameof(RepeatY)} = {SourceOf(RepeatY)}, " +
                    $"{nameof(Fit)} = {nameof(ObjectFit)}.{Fit}, " +
                    $"{nameof(PositionX)} = {SourceOf(PositionX)}, " +
                    $"{nameof(PositionY)} = {SourceOf(PositionY)} " +
                "}";
    }
}
