using Ixen.Core.Rendering;
using System;
using System.Diagnostics;

namespace Ixen.Core.Visual
{
    public abstract class DrawingElement : VisualElement
    {
        private bool _animating;
        private IxenSurface _framedOn;
        private long _lastFrame;

        protected abstract void OnDraw(DrawingContext context);

        protected virtual void OnFrame(TimeSpan elapsed)
        {
        }

        public bool Animating
        {
            get => _animating;
            set
            {
                if (_animating == value)
                {
                    return;
                }

                _animating = value;

                SyncFrames();
            }
        }

        public void Redraw() => Host?.InvalidateVisual(this);

        protected internal override void OnHostChanged()
        {
            base.OnHostChanged();

            SyncFrames();
        }

        private void SyncFrames()
        {
            IxenSurface wanted = _animating ? Host as IxenSurface : null;

            if (wanted == _framedOn)
            {
                return;
            }

            _framedOn?.StopFrames(this);
            _framedOn = wanted;
            _lastFrame = 0;
            _framedOn?.StartFrames(this);
        }

        internal bool CanBeSeen()
        {
            if (Clip != null && Clip.IsVoidOrInvalid)
            {
                return false;
            }

            for (VisualElement element = this; element != null; element = element.Parent)
            {
                if (element.IsHidden)
                {
                    return false;
                }
            }

            return true;
        }

        internal void RaiseFrame()
        {
            long now = Stopwatch.GetTimestamp();
            TimeSpan elapsed = _lastFrame == 0
                ? TimeSpan.Zero
                : TimeSpan.FromTicks((now - _lastFrame) * TimeSpan.TicksPerSecond / Stopwatch.Frequency);

            _lastFrame = now;

            OnFrame(elapsed);
        }

        internal void RenderDrawing(RendererContext renderer)
        {
            DrawingContext context = renderer.Drawing;

            context.Begin(renderer, this);

            try
            {
                OnDraw(context);
            }
            finally
            {
                context.End();
            }
        }
    }
}
