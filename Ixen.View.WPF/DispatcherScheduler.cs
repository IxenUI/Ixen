using Ixen.Core;
using System;
using System.Windows.Threading;

namespace Ixen.View.WPF
{
    internal sealed class DispatcherScheduler : IScheduler
    {
        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
            => new Subscription(delayMilliseconds, repeat, callback);

        private sealed class Subscription : IDisposable
        {
            private readonly DispatcherTimer _timer;
            private readonly Action _callback;
            private readonly bool _repeat;

            internal Subscription(int delayMilliseconds, bool repeat, Action callback)
            {
                _callback = callback;
                _repeat = repeat;

                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(delayMilliseconds)
                };

                _timer.Tick += OnTick;
                _timer.Start();
            }

            private void OnTick(object sender, EventArgs e)
            {
                if (!_repeat)
                {
                    Dispose();
                }

                _callback();
            }

            public void Dispose()
            {
                _timer.Stop();
                _timer.Tick -= OnTick;
            }
        }
    }
}
