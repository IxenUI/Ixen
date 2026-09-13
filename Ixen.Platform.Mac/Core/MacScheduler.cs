using Ixen.Core;
using Ixen.Platform.Mac.NativeApi;
using System;
using System.Collections.Generic;

namespace Ixen.Platform.Mac
{
    public class MacScheduler : IScheduler
    {
        private static readonly Dictionary<long, Action> _callbacks = new Dictionary<long, Action>();
        private static readonly MacApi.OnTimerCallBack _onTimer = OnTimer;

        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            if (callback == null)
            {
                return null;
            }

            long id = MacApi.Schedule(delayMilliseconds, repeat ? 1 : 0, _onTimer);

            if (id == 0)
            {
                return null;
            }

            _callbacks[id] = callback;

            return new Entry(id, repeat);
        }

        private static void OnTimer(long id)
        {
            if (!_callbacks.TryGetValue(id, out Action callback))
            {
                return;
            }

            callback();
        }

        private sealed class Entry : IDisposable
        {
            private readonly long _id;
            private readonly bool _repeat;
            private bool _disposed;

            internal Entry(long id, bool repeat)
            {
                _id = id;
                _repeat = repeat;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _disposed = true;

                _callbacks.Remove(_id);

                if (_repeat)
                {
                    MacApi.Cancel(_id);
                }
            }
        }
    }
}
