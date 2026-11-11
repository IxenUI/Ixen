using Android.OS;
using Android.Views;
using Ixen.Core;
using System;

namespace Ixen.View.Android
{
    internal class AndroidScheduler : IFrameScheduler
    {
        private readonly Handler _handler = new Handler(Looper.MainLooper);

        internal static bool InFrame { get; private set; }

        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            var entry = new Entry(_handler, delayMilliseconds, repeat, callback);
            entry.Start();

            return entry;
        }

        public IDisposable ScheduleFrames(Action<long> onFrame)
        {
            var frames = new Frames(onFrame);
            frames.Start();

            return new FrameSubscription(frames);
        }

        private sealed class FrameSubscription : IDisposable
        {
            private readonly Frames _frames;

            internal FrameSubscription(Frames frames)
            {
                _frames = frames;
            }

            public void Dispose() => _frames.Cancel();
        }

        private sealed class Frames : Java.Lang.Object, Choreographer.IFrameCallback
        {
            private readonly Action<long> _onFrame;
            private bool _cancelled;

            internal Frames(Action<long> onFrame)
            {
                _onFrame = onFrame;
            }

            internal void Start() => Choreographer.Instance?.PostFrameCallback(this);

            public void DoFrame(long frameTimeNanos)
            {
                if (_cancelled)
                {
                    return;
                }

                InFrame = true;

                try
                {
                    _onFrame(frameTimeNanos);
                }
                finally
                {
                    InFrame = false;
                }

                if (!_cancelled)
                {
                    Choreographer.Instance?.PostFrameCallback(this);
                }
            }

            internal void Cancel()
            {
                _cancelled = true;
                Choreographer.Instance?.RemoveFrameCallback(this);
            }
        }

        private sealed class Entry : IDisposable
        {
            private readonly Handler _handler;
            private readonly int _delay;
            private readonly bool _repeat;
            private readonly Action _callback;

            private bool _cancelled;

            internal Entry(Handler handler, int delay, bool repeat, Action callback)
            {
                _handler = handler;
                _delay = delay;
                _repeat = repeat;
                _callback = callback;
            }

            internal void Start() => _handler.PostDelayed(Tick, _delay);

            public void Dispose() => _cancelled = true;

            private void Tick()
            {
                if (_cancelled)
                {
                    return;
                }

                _callback();

                if (!_cancelled && _repeat)
                {
                    _handler.PostDelayed(Tick, _delay);
                }
            }
        }
    }
}
