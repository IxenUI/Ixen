using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    internal class TransformTransition
    {
        internal TransformStyleDescriptor From;
        internal TransformStyleDescriptor To;
        internal bool HasValue;
        internal bool Held;
        internal int Step;
        internal int Steps;
        internal int Delay;
        internal EasingKind Easing;

        private readonly TransformBlend _blend = new TransformBlend();
        private TransformBlend _snapshot;

        internal bool Running => Steps > 0 && Step < Steps;

        internal TransformStyleDescriptor Descriptor
        {
            get
            {
                if (Steps == 0 || Step >= Steps)
                {
                    return To;
                }

                return _blend.Of(From, To, Visual.Easing.Apply(Easing, (float)Step / Steps));
            }
        }

        internal void Jump(TransformStyleDescriptor value)
        {
            From = value;
            To = value;
            HasValue = true;
            Held = false;
            Step = 0;
            Steps = 0;
        }

        internal void Hold(TransformStyleDescriptor value)
        {
            Jump(value);
            Held = true;
        }

        internal void Start(TransformStyleDescriptor target, int steps, int delay, EasingKind easing)
        {
            From = Detached(Descriptor);
            To = target;
            Step = 0;
            Steps = steps;
            Delay = delay;
            Easing = easing;
            Held = false;
        }

        internal void Advance()
        {
            if (!Running)
            {
                return;
            }

            if (Delay > 0)
            {
                Delay--;
                return;
            }

            Step++;
        }

        private TransformStyleDescriptor Detached(TransformStyleDescriptor source)
        {
            if (!_blend.Produced(source))
            {
                return source;
            }

            if (_snapshot == null)
            {
                _snapshot = new TransformBlend();
            }

            return _snapshot.CopyOf(source);
        }
    }
}
