using Foundation;
using Ixen.Core;
using System;

namespace Ixen.View.iOS
{
    internal class IosScheduler : IScheduler
    {
        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            var entry = new Entry(repeat, callback);
            entry.Start(delayMilliseconds);

            return entry;
        }

        private sealed class Entry : IDisposable
        {
            private readonly bool _repeat;
            private readonly Action _callback;

            private NSTimer _timer;

            internal Entry(bool repeat, Action callback)
            {
                _repeat = repeat;
                _callback = callback;
            }

            internal void Start(int delayMilliseconds)
            {
                double seconds = Math.Max(0, delayMilliseconds) / 1000.0;

                _timer = NSTimer.CreateScheduledTimer(seconds, _repeat, Tick);
            }

            public void Dispose()
            {
                _timer?.Invalidate();
                _timer = null;
            }

            private void Tick(NSTimer timer)
            {
                if (_timer == null)
                {
                    return;
                }

                if (!_repeat)
                {
                    Dispose();
                }

                _callback();
            }
        }
    }
}
