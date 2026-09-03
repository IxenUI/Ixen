using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class AnimationStyleDescriptor : StyleDescriptor
    {
        public const int INFINITE = 0;

        internal override string Identifier => StyleIdentifier.ANIMATION;

        public List<AnimationSpec> Animations { get; set; } = new List<AnimationSpec>();

        internal AnimationSpec First => Animations.Count > 0 ? Animations[0] : null;

        private AnimationSpec Ensure()
        {
            if (Animations.Count == 0)
            {
                Animations.Add(new AnimationSpec());
            }

            return Animations[0];
        }

        public string Name
        {
            get => First?.Name;
            set => Ensure().Name = value;
        }

        public int Duration
        {
            get => First?.Duration ?? 0;
            set => Ensure().Duration = value;
        }

        public int Delay
        {
            get => First?.Delay ?? 0;
            set => Ensure().Delay = value;
        }

        public EasingKind Easing
        {
            get => First?.Easing ?? EasingKind.Linear;
            set => Ensure().Easing = value;
        }

        public int Iterations
        {
            get => First?.Iterations ?? 1;
            set => Ensure().Iterations = value;
        }

        public bool Alternate
        {
            get => First?.Alternate ?? false;
            set => Ensure().Alternate = value;
        }

        public AnimationFill Fill
        {
            get => First?.Fill ?? AnimationFill.None;
            set => Ensure().Fill = value;
        }

        public bool IsDeclared => Animations.Exists(a => a.IsDeclared);

        public bool Matches(AnimationStyleDescriptor other)
        {
            if (other == null || other.Animations.Count != Animations.Count)
            {
                return false;
            }

            for (int index = 0; index < Animations.Count; index++)
            {
                if (!Animations[index].Matches(other.Animations[index]))
                {
                    return false;
                }
            }

            return true;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(AnimationStyleDescriptor)} {{ "
                + $"{nameof(Animations)} = new() {{ "
                    + string.Join(", ", Animations.Select(a => a.ToSource()))
                + "} }";
    }
}
