using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    internal class KeyframeAnimation
    {
        private readonly VisualElement _element;

        private KeyframesSet _set;
        private AnimationStyleDescriptor _spec;
        private AnimationStyleDescriptor _started;

        private int _step;
        private int _steps;
        private int _delay;
        private int _iteration;
        private bool _reversed;
        private bool _running;

        internal KeyframeAnimation(VisualElement element)
        {
            _element = element;
        }

        internal bool Running => _running;
        internal bool AnimatesSize { get; private set; }
        internal AnimationStyleDescriptor Spec => _spec;

        internal bool Drives(string identifier)
        {
            if (!_running || _set == null)
            {
                return false;
            }

            IReadOnlyList<string> properties = _set.Properties;

            for (int index = 0; index < properties.Count; index++)
            {
                if (properties[index] == identifier)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool StartedWith(AnimationStyleDescriptor spec) => ReferenceEquals(_started, spec);

        internal void Start(KeyframesSet set, AnimationStyleDescriptor spec)
        {
            _started = spec;

            if (set == null || set.Properties.Count == 0)
            {
                _set = null;
                _spec = null;
                _running = false;
                AnimatesSize = false;
                return;
            }

            _set = set;
            _spec = spec;
            _steps = Math.Max(1, spec.Duration / ElementAnimations.TICK);
            _delay = spec.Delay / ElementAnimations.TICK;
            _step = 0;
            _iteration = 0;
            _reversed = false;
            _running = true;

            AnimatesSize = false;

            IReadOnlyList<string> properties = set.Properties;

            for (int index = 0; index < properties.Count; index++)
            {
                if (KeyframesSet.IsSizeProperty(properties[index]))
                {
                    AnimatesSize = true;
                    break;
                }
            }

            Apply();
        }

        internal void Stop()
        {
            Suspend();
            _started = null;
        }

        internal void Suspend()
        {
            Release();

            _running = false;
            _set = null;
            _spec = null;
            AnimatesSize = false;
        }

        private void Release()
        {
            if (_set == null)
            {
                return;
            }

            IReadOnlyList<string> properties = _set.Properties;

            for (int index = 0; index < properties.Count; index++)
            {
                string identifier = properties[index];

                SizeTransition size = _element.Animations.SizeIfAny(identifier);

                if (size != null)
                {
                    size.Held = false;
                }

                if (KeyframesSet.IsColorProperty(identifier))
                {
                    _element.Animations.For(identifier).Held = false;
                }

                if (KeyframesSet.IsTransformProperty(identifier)
                    && _element.Animations.Transform != null)
                {
                    _element.Animations.Transform.Held = false;
                }
            }
        }

        internal void Advance()
        {
            if (!_running)
            {
                return;
            }

            if (_delay > 0)
            {
                _delay--;
                return;
            }

            _step++;

            if (_step >= _steps)
            {
                _iteration++;

                if (_spec.Iterations != AnimationStyleDescriptor.INFINITE
                    && _iteration >= _spec.Iterations)
                {
                    _step = _steps;
                    Apply();
                    _running = false;
                    _element.Invalidate();
                    return;
                }

                _step = 0;

                if (_spec.Alternate)
                {
                    _reversed = !_reversed;
                }
            }

            Apply();
        }

        private void Apply()
        {
            float progress = (float)_step / _steps;

            if (_reversed)
            {
                progress = 1f - progress;
            }

            IReadOnlyList<string> properties = _set.Properties;

            for (int index = 0; index < properties.Count; index++)
            {
                string identifier = properties[index];

                ColorStop[] colors = _set.ColorTrack(identifier);

                if (colors != null)
                {
                    _element.Animations.For(identifier)
                        .Hold(SampleColor(colors, progress, _spec.Easing));

                    continue;
                }

                SizeStop[] sizes = _set.SizeTrack(identifier);

                if (sizes != null)
                {
                    SizeStop sampled = SampleSize(sizes, progress, _spec.Easing);
                    _element.Animations.SizeFor(identifier).Hold(sampled.Unit, sampled.Value);
                    continue;
                }

                TransformStop[] transforms = _set.TransformTrack(identifier);

                if (transforms != null)
                {
                    _element.Animations.TransformFor()
                        .Hold(SampleTransform(transforms, progress, _spec.Easing));
                }
            }
        }

        private readonly TransformBlend _blend = new TransformBlend();

        private TransformStyleDescriptor SampleTransform(TransformStop[] track, float progress,
            EasingKind easing)
        {
            int last = track.Length - 1;

            if (progress <= track[0].Offset)
            {
                return track[0].Value;
            }

            if (progress >= track[last].Offset)
            {
                return track[last].Value;
            }

            for (int index = 0; index < last; index++)
            {
                TransformStop from = track[index];
                TransformStop to = track[index + 1];

                if (progress > to.Offset)
                {
                    continue;
                }

                if (!TransformStyleDescriptor.Compatible(from.Value, to.Value))
                {
                    return from.Value;
                }

                float span = to.Offset - from.Offset;

                if (span <= 0)
                {
                    return to.Value;
                }

                return _blend.Of(from.Value, to.Value,
                    Easing.Apply(easing, (progress - from.Offset) / span));
            }

            return track[last].Value;
        }

        private static Color SampleColor(ColorStop[] track, float progress, EasingKind easing)
        {
            int last = track.Length - 1;

            if (progress <= track[0].Offset)
            {
                return track[0].Value;
            }

            if (progress >= track[last].Offset)
            {
                return track[last].Value;
            }

            for (int index = 0; index < last; index++)
            {
                ColorStop from = track[index];
                ColorStop to = track[index + 1];

                if (progress > to.Offset)
                {
                    continue;
                }

                float span = to.Offset - from.Offset;

                if (span <= 0)
                {
                    return to.Value;
                }

                return Color.Lerp(from.Value, to.Value,
                    Easing.Apply(easing, (progress - from.Offset) / span));
            }

            return track[last].Value;
        }

        private static SizeStop SampleSize(SizeStop[] track, float progress, EasingKind easing)
        {
            int last = track.Length - 1;

            if (progress <= track[0].Offset)
            {
                return track[0];
            }

            if (progress >= track[last].Offset)
            {
                return track[last];
            }

            for (int index = 0; index < last; index++)
            {
                SizeStop from = track[index];
                SizeStop to = track[index + 1];

                if (progress > to.Offset)
                {
                    continue;
                }

                if (from.Unit != to.Unit || !SizeTransition.CanInterpolate(from.Unit))
                {
                    return from;
                }

                float span = to.Offset - from.Offset;

                if (span <= 0)
                {
                    return to;
                }

                float eased = Easing.Apply(easing, (progress - from.Offset) / span);

                return new SizeStop
                {
                    Offset = progress,
                    Unit = from.Unit,
                    Value = from.Value + (to.Value - from.Value) * eased
                };
            }

            return track[last];
        }
    }
}
