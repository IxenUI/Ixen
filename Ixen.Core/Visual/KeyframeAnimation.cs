using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    internal class KeyframeAnimation
    {
        private class Instance
        {
            internal KeyframesSet Set;
            internal AnimationSpec Spec;

            internal int Step;
            internal int Steps;
            internal int Delay;
            internal int Iteration;
            internal bool Reversed;
            internal bool Running;
            internal bool Holding;
            internal bool AnimatesSize;

            internal readonly TransformBlend Blend = new TransformBlend();

            internal bool Owns(string identifier)
            {
                if ((!Running && !Holding) || Set == null)
                {
                    return false;
                }

                IReadOnlyList<string> properties = Set.Properties;

                for (int index = 0; index < properties.Count; index++)
                {
                    if (properties[index] == identifier)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private readonly VisualElement _element;
        private readonly List<Instance> _instances = new List<Instance>();

        private AnimationStyleDescriptor _started;

        internal KeyframeAnimation(VisualElement element)
        {
            _element = element;
        }

        internal bool Running => _instances.Exists(i => i.Running);
        internal bool AnimatesSize => _instances.Exists(i => i.Running && i.AnimatesSize);

        internal bool Drives(string identifier) => _instances.Exists(i => i.Owns(identifier));

        internal bool StartedWith(AnimationStyleDescriptor spec) => ReferenceEquals(_started, spec);

        internal void Start(AnimationStyleDescriptor declaration, StyleRegistry registry)
        {
            Release();

            _started = declaration;
            _instances.Clear();

            if (declaration == null || registry == null)
            {
                return;
            }

            foreach (AnimationSpec spec in declaration.Animations)
            {
                KeyframesSet set = registry.GetKeyframes(spec.Name);

                if (set == null || set.Properties.Count == 0)
                {
                    continue;
                }

                _instances.Add(Fresh(set, spec));
            }

            Apply();
        }

        private static Instance Fresh(KeyframesSet set, AnimationSpec spec)
        {
            var instance = new Instance
            {
                Set = set,
                Spec = spec,
                Steps = Math.Max(1, spec.Duration / ElementAnimations.TICK),
                Delay = spec.Delay / ElementAnimations.TICK,
                Running = true
            };

            IReadOnlyList<string> properties = set.Properties;

            for (int index = 0; index < properties.Count; index++)
            {
                if (KeyframesSet.IsSizeProperty(properties[index]))
                {
                    instance.AnimatesSize = true;
                    break;
                }
            }

            return instance;
        }

        internal void Stop()
        {
            Suspend();
            _started = null;
        }

        internal void Complete()
        {
            var held = new List<Instance>();

            foreach (Instance instance in _instances)
            {
                if (instance.Spec.Fill != AnimationFill.Forwards
                    || instance.Spec.Iterations == AnimationStyleDescriptor.INFINITE)
                {
                    continue;
                }

                instance.Delay = 0;
                instance.Step = instance.Steps;
                instance.Iteration = instance.Spec.Iterations;
                instance.Reversed = instance.Spec.Alternate
                    && instance.Spec.Iterations % 2 == 0;

                held.Add(instance);
            }

            Release();

            _instances.Clear();
            _instances.AddRange(held);

            foreach (Instance instance in _instances)
            {
                instance.Running = false;
                instance.Holding = true;
            }

            Apply();
        }

        internal void Suspend()
        {
            Release();

            _instances.Clear();
        }

        private void Release()
        {
            foreach (Instance instance in _instances)
            {
                ReleaseOne(instance);
            }
        }

        private void ReleaseOne(Instance instance)
        {
            if (instance.Set == null)
            {
                return;
            }

            IReadOnlyList<string> properties = instance.Set.Properties;

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
            bool ended = false;

            foreach (Instance instance in _instances)
            {
                if (!instance.Running)
                {
                    continue;
                }

                if (instance.Delay > 0)
                {
                    instance.Delay--;
                    continue;
                }

                instance.Step++;

                if (instance.Step < instance.Steps)
                {
                    continue;
                }

                instance.Iteration++;

                if (instance.Spec.Iterations != AnimationStyleDescriptor.INFINITE
                    && instance.Iteration >= instance.Spec.Iterations)
                {
                    instance.Step = instance.Steps;
                    ApplyOne(instance);
                    instance.Running = false;
                    instance.Holding = instance.Spec.Fill == AnimationFill.Forwards;
                    ended = true;
                    continue;
                }

                instance.Step = 0;

                if (instance.Spec.Alternate)
                {
                    instance.Reversed = !instance.Reversed;
                }
            }

            Apply();

            if (ended)
            {
                _element.Invalidate();
            }
        }

        private void Apply()
        {
            foreach (Instance instance in _instances)
            {
                ApplyOne(instance);
            }
        }

        private void ApplyOne(Instance instance)
        {
            if (instance.Set == null || (!instance.Running && !instance.Holding))
            {
                return;
            }

            float progress = (float)instance.Step / instance.Steps;

            if (instance.Reversed)
            {
                progress = 1f - progress;
            }

            IReadOnlyList<string> properties = instance.Set.Properties;

            for (int index = 0; index < properties.Count; index++)
            {
                string identifier = properties[index];

                ColorStop[] colors = instance.Set.ColorTrack(identifier);

                if (colors != null)
                {
                    _element.Animations.For(identifier)
                        .Hold(SampleColor(colors, progress, instance.Spec.Easing));

                    continue;
                }

                SizeStop[] sizes = instance.Set.SizeTrack(identifier);

                if (sizes != null)
                {
                    SizeStop sampled = SampleSize(sizes, progress, instance.Spec.Easing);
                    _element.Animations.SizeFor(identifier).Hold(sampled.Unit, sampled.Value);
                    continue;
                }

                TransformStop[] transforms = instance.Set.TransformTrack(identifier);

                if (transforms != null)
                {
                    _element.Animations.TransformFor()
                        .Hold(SampleTransform(instance, transforms, progress, instance.Spec.Easing));
                }
            }
        }

        private static TransformStyleDescriptor SampleTransform(Instance instance,
            TransformStop[] track, float progress, EasingKind easing)
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

                return instance.Blend.Of(from.Value, to.Value,
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
