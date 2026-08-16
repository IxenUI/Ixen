using Ixen.Core;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal sealed class MessageScheduler : IScheduler
    {
        private delegate void TimerProc(IntPtr hwnd, uint message, UIntPtr id, uint time);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern UIntPtr SetTimer(IntPtr hwnd, UIntPtr id, uint elapse, TimerProc callback);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool KillTimer(IntPtr hwnd, UIntPtr id);

        private static readonly Dictionary<ulong, Subscription> _subscriptions = new Dictionary<ulong, Subscription>();
        private static readonly TimerProc _proc = OnTimer;

        public IDisposable Schedule(int delayMilliseconds, bool repeat, Action callback)
        {
            UIntPtr id = SetTimer(IntPtr.Zero, UIntPtr.Zero, (uint)Math.Max(1, delayMilliseconds), _proc);

            if (id == UIntPtr.Zero)
            {
                return new Subscription(UIntPtr.Zero, false, null);
            }

            var subscription = new Subscription(id, repeat, callback);
            _subscriptions[id.ToUInt64()] = subscription;

            return subscription;
        }

        private static void OnTimer(IntPtr hwnd, uint message, UIntPtr id, uint time)
        {
            if (!_subscriptions.TryGetValue(id.ToUInt64(), out Subscription subscription))
            {
                KillTimer(IntPtr.Zero, id);
                return;
            }

            subscription.Tick();
        }

        private sealed class Subscription : IDisposable
        {
            private readonly UIntPtr _id;
            private readonly bool _repeat;
            private readonly Action _callback;

            private bool _disposed;

            internal Subscription(UIntPtr id, bool repeat, Action callback)
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
                _subscriptions.Remove(_id.ToUInt64());
                KillTimer(IntPtr.Zero, _id);
            }
        }
    }
}
