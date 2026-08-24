namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum AnchorSide
    {
        Below,
        Above,
        Left,
        Right
    }

    public enum AnchorAlign
    {
        Start,
        Center,
        End
    }

    public class AnchorPlacementStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.ANCHOR_PLACEMENT;

        public AnchorSide Side { get; set; } = AnchorSide.Below;
        public AnchorAlign Align { get; set; } = AnchorAlign.Start;
        public bool NoFlip { get; set; }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(AnchorPlacementStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Side)} = {nameof(AnchorSide)}.{Side}, " +
                    $"{nameof(Align)} = {nameof(AnchorAlign)}.{Align}, " +
                    $"{nameof(NoFlip)} = {NoFlip.ToString().ToLower()} " +
                "}";
    }
}
