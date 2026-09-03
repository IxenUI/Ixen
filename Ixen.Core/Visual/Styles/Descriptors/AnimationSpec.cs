namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class AnimationSpec
    {
        public string Name { get; set; } = null;
        public int Duration { get; set; } = 0;
        public int Delay { get; set; } = 0;
        public EasingKind Easing { get; set; } = EasingKind.Linear;
        public int Iterations { get; set; } = 1;
        public bool Alternate { get; set; } = false;
        public AnimationFill Fill { get; set; } = AnimationFill.None;

        public bool IsDeclared => Name != null && Duration > 0;

        public bool Matches(AnimationSpec other)
            => other != null
                && Name == other.Name
                && Duration == other.Duration
                && Delay == other.Delay
                && Easing == other.Easing
                && Iterations == other.Iterations
                && Alternate == other.Alternate
                && Fill == other.Fill;

        internal string ToSource()
            => $"new {nameof(AnimationSpec)} {{ "
                + $"{nameof(Name)} = {StyleDescriptor.SourceOf(Name)}, "
                + $"{nameof(Duration)} = {Duration}, "
                + $"{nameof(Delay)} = {Delay}, "
                + $"{nameof(Easing)} = global::Ixen.Core.Visual.Styles.{nameof(EasingKind)}.{Easing}, "
                + $"{nameof(Iterations)} = {Iterations}, "
                + $"{nameof(Alternate)} = {StyleDescriptor.SourceOf(Alternate)}, "
                + $"{nameof(Fill)} = global::Ixen.Core.Visual.Styles.{nameof(AnimationFill)}.{Fill} }}";
    }
}
