namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum LetterSpacingKind
    {
        Unset,
        Normal,
        Pixels
    }

    public class LetterSpacingStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.LETTER_SPACING;

        public LetterSpacingKind Kind { get; set; } = LetterSpacingKind.Unset;
        public float Value { get; set; }

        internal bool IsDeclared => Kind != LetterSpacingKind.Unset;

        internal float Resolve() => Kind == LetterSpacingKind.Pixels ? Value : 0;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(LetterSpacingStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Kind)} = {nameof(LetterSpacingKind)}.{Kind}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
