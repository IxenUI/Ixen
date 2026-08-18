namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class AnimationStyleDescriptor : StyleDescriptor
    {
        public const int INFINITE = 0;

        internal override string Identifier => StyleIdentifier.ANIMATION;

        public string Name { get; set; } = null;
        public int Duration { get; set; } = 0;
        public int Delay { get; set; } = 0;
        public EasingKind Easing { get; set; } = EasingKind.Linear;
        public int Iterations { get; set; } = 1;
        public bool Alternate { get; set; } = false;

        public bool IsDeclared => Name != null && Duration > 0;

        public bool Matches(AnimationStyleDescriptor other)
            => other != null
                && Name == other.Name
                && Duration == other.Duration
                && Delay == other.Delay
                && Easing == other.Easing
                && Iterations == other.Iterations
                && Alternate == other.Alternate;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(AnimationStyleDescriptor)} {{ "
                + $"{nameof(Name)} = {SourceOf(Name)}, "
                + $"{nameof(Duration)} = {Duration}, "
                + $"{nameof(Delay)} = {Delay}, "
                + $"{nameof(Easing)} = global::Ixen.Core.Visual.Styles.{nameof(EasingKind)}.{Easing}, "
                + $"{nameof(Iterations)} = {Iterations}, "
                + $"{nameof(Alternate)} = {SourceOf(Alternate)} }}";
    }
}
