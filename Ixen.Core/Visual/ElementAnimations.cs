using System;

namespace Ixen.Core.Visual
{
    internal class ColorTransition
    {
        internal Color From;
        internal Color To;
        internal Color Current;
        internal bool HasValue;
        internal int Step;
        internal int Steps;

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
            Step = 0;
            Steps = 0;
        }

        internal void Start(Color target, int steps)
        {
            From = Current;
            To = target;
            Step = 0;
            Steps = steps;
        }

        internal void Advance()
        {
            if (!Running)
            {
                return;
            }

            Step++;
            Current = Step >= Steps ? To : Color.Lerp(From, To, (float)Step / Steps);
        }
    }

    internal class ElementAnimations
    {
        internal const int TICK = 16;

        private readonly VisualElement _element;
        private IDisposable _ticker;

        internal ColorTransition Background;
        internal ColorTransition Color;
        internal ColorTransition Border;

        internal ElementAnimations(VisualElement element)
        {
            _element = element;
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

        internal bool Running
            => (Background != null && Background.Running)
                || (Color != null && Color.Running)
                || (Border != null && Border.Running);

        internal void Sync()
        {
            if (!Running)
            {
                Stop();
                return;
            }

            if (_ticker != null)
            {
                return;
            }

            _ticker = _element.Host?.Scheduler?.Schedule(TICK, true, Tick);

            if (_ticker == null)
            {
                Finish();
            }
        }

        internal void Stop()
        {
            _ticker?.Dispose();
            _ticker = null;
        }

        private void Tick()
        {
            Background?.Advance();
            Color?.Advance();
            Border?.Advance();

            _element.Host?.InvalidateVisual();

            if (!Running)
            {
                Stop();
            }
        }

        private void Finish()
        {
            Background?.Jump(Background.To);
            Color?.Jump(Color.To);
            Border?.Jump(Border.To);
        }
    }
}
