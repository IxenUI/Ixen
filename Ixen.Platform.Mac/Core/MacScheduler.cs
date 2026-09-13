using Ixen.Core;
using Ixen.Platform.Mac.NativeApi;
using System;
using System.Collections.Generic;

namespace Ixen.Platform.Mac
{
    public class MacScheduler : IScheduler
    {
        private static readonly Dictionary<long, Subscription> _subscriptions = new Dictionary<long, Subscription>();
        private static readonly MacApi.OnTimerCallBack _onTimer = OnTimer;

        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            long id = MacApi.Schedule(Math.Max(1, delayMilliseconds), repeat ? 1 : 0, _onTimer);

            if (id == 0)
            {
                return new Subscription(0, false, null);
            }

            var subscription = new Subscription(id, repeat, callback);
            _subscriptions[id] = subscription;

            return subscription;
        }

        private static void OnTimer(long id)
        {
            if (!_subscriptions.TryGetValue(id, out Subscription subscription))
            {
                MacApi.Cancel(id);
                return;
            }

            subscription.Tick();
        }

        private sealed class Subscription : IDisposable
        {
            private readonly long _id;
            private readonly bool _repeat;
            private readonly Action _callback;

            private bool _disposed;

            internal Subscription(long id, bool repeat, Action callback)
            {
                _id = id;
                _repeat = repeat;
                _callback = callback;
                _disposed = callback == null;
            }

            internal void Tick()
            {
                if (_disposed)
                {
                    return;
                }

                if (!_repeat)
                {
                    Dispose();
                }

                _callback();
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;
                _subscriptions.Remove(_id);

                MacApi.Cancel(_id);
            }
        }
    }
}
