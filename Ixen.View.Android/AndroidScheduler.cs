using Android.OS;
using Ixen.Core;
using System;

namespace Ixen.View.Android
{
    internal class AndroidScheduler : IScheduler
    {
        private readonly Handler _handler = new Handler(Looper.MainLooper);

        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            var entry = new Entry(_handler, delayMilliseconds, repeat, callback);
            entry.Start();

            return entry;
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
