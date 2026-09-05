using System;

namespace Ixen.Core.Visual
{
    internal class ColorTransition
    {
        internal Color From;
        internal Color To;
        internal Color Current;
        internal bool HasValue;
        internal bool Held;
        internal int Step;
        internal int Steps;
        internal int Delay;
        internal Styles.EasingKind Easing;

        private Rendering.Brush _brush;
        private Rendering.Pen _pen;

        internal bool Running => Steps > 0 && Step < Steps;

        internal Rendering.Brush Brush
        {
            get
            {
                if (_brush == null)
                {
                    _brush = new Rendering.Brush(Current);
                }
                else
                {
                    _brush.Color = Current;
                }

                return _brush;
            }
        }

        internal Rendering.Pen PenLike(Rendering.Pen source)
        {
            if (_pen == null)
            {
                _pen = new Rendering.Pen(Current, source.Width);
            }
            else
            {
                _pen.Color = Current;
                _pen.Width = source.Width;
            }

            return _pen;
        }

        internal void Jump(Color color)
        {
            From = color;
            To = color;
            Current = color;
            HasValue = true;
            Held = false;
            Step = 0;
            Steps = 0;
        }

        internal void Hold(Color color)
        {
            Jump(color);
            Held = true;
        }

        internal void Start(Color target, int steps, int delay, Styles.EasingKind easing)
        {
            From = Current;
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

            Current = Step >= Steps
                ? To
                : Color.Lerp(From, To, Visual.Easing.Apply(Easing, (float)Step / Steps));
        }
    }

    internal class SizeTransition
    {
        internal Styles.Descriptors.SizeUnit Unit;
        internal float Offset;
        internal float From;
        internal float To;
        internal float Current;
        internal bool HasValue;
        internal bool Held;
        internal int Step;
        internal int Steps;
        internal int Delay;
        internal Styles.EasingKind Easing;

        private Styles.Descriptors.OffsetStyleDescriptor _descriptor;

        internal static bool CanInterpolate(Styles.Descriptors.SizeUnit unit)
            => unit == Styles.Descriptors.SizeUnit.Pixels
                || unit == Styles.Descriptors.SizeUnit.Percents;

        internal bool Running => Steps > 0 && Step < Steps;

        internal Styles.Descriptors.OffsetStyleDescriptor Descriptor
        {
            get
            {
                if (_descriptor == null)
                {
                    _descriptor = new Styles.Descriptors.OffsetStyleDescriptor();
                }

                _descriptor.Unit = Unit;
                _descriptor.Value = Current;
                _descriptor.Offset = Offset;

                return _descriptor;
            }
        }

        internal void Jump(Styles.Descriptors.SizeUnit unit, float value, float offset = 0)
        {
            Unit = unit;
            Offset = offset;
            From = value;
            To = value;
            Current = value;
            HasValue = true;
            Held = false;
            Step = 0;
            Steps = 0;
        }

        internal void Hold(Styles.Descriptors.SizeUnit unit, float value)
        {
            Jump(unit, value);
            Held = true;
        }

        internal void Start(float target, int steps, int delay, Styles.EasingKind easing)
        {
            From = Current;
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

            Current = Step >= Steps
                ? To
                : From + (To - From) * Visual.Easing.Apply(Easing, (float)Step / Steps);
        }
    }

    internal class ScrollTransition
    {
        internal float FromX;
        internal float FromY;
        internal float ToX;
        internal float ToY;
        internal int Step;
        internal int Steps;

        internal bool Running => Steps > 0 && Step < Steps;

        internal void Start(float fromX, float fromY, float toX, float toY, int steps)
        {
            FromX = fromX;
            FromY = fromY;
            ToX = toX;
            ToY = toY;
            Step = 0;
            Steps = steps;
        }

        internal void Stop()
        {
            Step = 0;
            Steps = 0;
        }

        internal void Advance() => Step++;

        private float Progress
            => Steps <= 0 || Step >= Steps
                ? 1f
                : Visual.Easing.Apply(Styles.EasingKind.EaseOut, (float)Step / Steps);

        internal float X => FromX + (ToX - FromX) * Progress;

        internal float Y => FromY + (ToY - FromY) * Progress;
    }

    internal class ElementAnimations
    {
        internal const int TICK = 16;

        private readonly VisualElement _element;
        private Ixen.Core.IElementHost _registered;

        internal ColorTransition Background;
        internal ColorTransition Color;
        internal ColorTransition Border;

        internal SizeTransition Width;
        internal SizeTransition Height;
        internal SizeTransition Left;
        internal SizeTransition Top;
        internal SizeTransition Right;
        internal SizeTransition Bottom;

        internal TransformTransition Transform;
        internal ScrollTransition Scroll;

        private KeyframeAnimation _keyframes;

        internal ElementAnimations(VisualElement element)
        {
            _element = element;
        }

        internal KeyframeAnimation Keyframes
            => _keyframes ?? (_keyframes = new KeyframeAnimation(_element));

        internal bool HasKeyframes => _keyframes != null && _keyframes.Running;

        internal void StopKeyframes() => _keyframes?.Stop();

        internal SizeTransition SizeFor(string identifier)
        {
            switch (identifier)
            {
                case Styles.StyleIdentifier.WIDTH:
                    return Width ?? (Width = new SizeTransition());

                case Styles.StyleIdentifier.HEIGHT:
                    return Height ?? (Height = new SizeTransition());

                case Styles.StyleIdentifier.LEFT:
                    return Left ?? (Left = new SizeTransition());

                case Styles.StyleIdentifier.TOP:
                    return Top ?? (Top = new SizeTransition());

                case Styles.StyleIdentifier.RIGHT:
                    return Right ?? (Right = new SizeTransition());

                case Styles.StyleIdentifier.BOTTOM:
                    return Bottom ?? (Bottom = new SizeTransition());

                default:
                    return null;
            }
        }

        internal SizeTransition SizeIfAny(string identifier)
        {
            switch (identifier)
            {
                case Styles.StyleIdentifier.WIDTH:
                    return Width;

                case Styles.StyleIdentifier.HEIGHT:
                    return Height;

                case Styles.StyleIdentifier.LEFT:
                    return Left;

                case Styles.StyleIdentifier.TOP:
                    return Top;

                case Styles.StyleIdentifier.RIGHT:
                    return Right;

                case Styles.StyleIdentifier.BOTTOM:
                    return Bottom;

                default:
                    return null;
            }
        }

        internal ColorTransition For(string identifier)
        {
            switch (identifier)
            {
                case Styles.StyleIdentifier.BACKGROUND:
                    return Background ?? (Background = new ColorTransition());

                case Styles.StyleIdentifier.COLOR:
                    return Color ?? (Color = new ColorTransition());

                case Styles.StyleIdentifier.BORDER:
                    return Border ?? (Border = new ColorTransition());

                default:
                    return null;
            }
        }

        internal TransformTransition TransformFor()
            => Transform ?? (Transform = new TransformTransition());

        internal ScrollTransition ScrollFor()
            => Scroll ?? (Scroll = new ScrollTransition());

        internal bool Running
            => (Background != null && Background.Running)
                || (Color != null && Color.Running)
                || (Border != null && Border.Running)
                || (Transform != null && Transform.Running)
                || (Scroll != null && Scroll.Running)
                || HasKeyframes
                || SizeRunning;

        internal bool SizeRunning
            => (Width != null && Width.Running)
                || (Height != null && Height.Running)
                || (Left != null && Left.Running)
                || (Top != null && Top.Running)
                || (Right != null && Right.Running)
                || (Bottom != null && Bottom.Running)
                || (HasKeyframes && _keyframes.AnimatesSize);

        internal void Sync()
        {
            if (!Running)
            {
                Stop();
                return;
            }

            IElementHost host = _element.Host;

            if (host == null)
            {
                Finish();
                return;
            }

            if (_registered == host)
            {
                return;
            }

            Stop();

            _registered = host;
            host.StartAnimating(_element);
        }

        internal void Stop()
        {
            if (_registered == null)
            {
                return;
            }

            IElementHost host = _registered;
            _registered = null;
            host.StopAnimating(_element);
        }

        private void Advance(ColorTransition transition, string identifier)
        {
            if (transition == null || !transition.Running)
            {
                return;
            }

            transition.Advance();

            if (!transition.Running && _element.HasTransitionEndedHandler)
            {
                _element.RaiseTransitionEnded(new TransitionEventArgs(identifier, _element));
            }
        }

        private void Advance(SizeTransition transition, string identifier)
        {
            if (transition == null || !transition.Running)
            {
                return;
            }

            transition.Advance();

            if (!transition.Running && _element.HasTransitionEndedHandler)
            {
                _element.RaiseTransitionEnded(new TransitionEventArgs(identifier, _element));
            }
        }

        private void AdvanceScroll()
        {
            if (Scroll == null || !Scroll.Running)
            {
                return;
            }

            Scroll.Advance();
            _element.ApplyScroll(Scroll.X, Scroll.Y);
        }

        private void AdvanceTransform()
        {
            if (Transform == null || !Transform.Running)
            {
                return;
            }

            Transform.Advance();

            if (!Transform.Running && _element.HasTransitionEndedHandler)
            {
                _element.RaiseTransitionEnded(
                    new TransitionEventArgs(Styles.StyleIdentifier.TRANSFORM, _element));
            }
        }

        internal bool CanBeSeen()
        {
            if (_element.Clip != null && _element.Clip.IsVoidOrInvalid)
            {
                return false;
            }

            for (VisualElement element = _element; element != null; element = element.Parent)
            {
                if (element.IsHidden)
                {
                    return false;
                }
            }

            return true;
        }

        internal bool Tick()
        {
            bool sizes = SizeRunning;

            _keyframes?.Advance();

            Advance(Background, Styles.StyleIdentifier.BACKGROUND);
            Advance(Color, Styles.StyleIdentifier.COLOR);
            Advance(Border, Styles.StyleIdentifier.BORDER);

            Advance(Width, Styles.StyleIdentifier.WIDTH);
            Advance(Height, Styles.StyleIdentifier.HEIGHT);
            Advance(Left, Styles.StyleIdentifier.LEFT);
            Advance(Top, Styles.StyleIdentifier.TOP);
            Advance(Right, Styles.StyleIdentifier.RIGHT);
            Advance(Bottom, Styles.StyleIdentifier.BOTTOM);

            AdvanceTransform();
            AdvanceScroll();

            if (sizes && CanBeSeen())
            {
                _element.InvalidateLayout();
            }

            if (Running)
            {
                return true;
            }

            _registered = null;
            return false;
        }

        internal void Finish()
        {
            Background?.Jump(Background.To);
            Color?.Jump(Color.To);
            Border?.Jump(Border.To);

            Width?.Jump(Width.Unit, Width.To);
            Height?.Jump(Height.Unit, Height.To);
            Left?.Jump(Left.Unit, Left.To);
            Top?.Jump(Top.Unit, Top.To);
            Right?.Jump(Right.Unit, Right.To);
            Bottom?.Jump(Bottom.Unit, Bottom.To);

            Transform?.Jump(Transform.To);

            if (Scroll != null && Scroll.Running)
            {
                _element.ApplyScroll(Scroll.ToX, Scroll.ToY);
                Scroll.Stop();
            }

            _keyframes?.Complete();
        }
    }
}
