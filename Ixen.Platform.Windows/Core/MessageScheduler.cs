using Ixen.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ixen.Platform.Windows
{
    internal sealed class MessageScheduler : IFrameScheduler
    {
        private delegate void TimerProc(IntPtr hwnd, uint message, UIntPtr id, uint time);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern UIntPtr SetTimer(IntPtr hwnd, UIntPtr id, uint elapse, TimerProc callback);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool KillTimer(IntPtr hwnd, UIntPtr id);

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        private static readonly Dictionary<ulong, Subscription> _subscriptions = new Dictionary<ulong, Subscription>();
        private static readonly TimerProc _proc = OnTimer;

        private readonly Action<Action> _post;

        internal MessageScheduler(Action<Action> post)
        {
            _post = post;
        }

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

        public IDisposable ScheduleFrames(Action<long> onFrame)
        {
            var frames = new Frames(_post, onFrame);

            frames.Start();

            return frames;
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

        private sealed class Frames : IDisposable
        {
            private const int FALLBACK_MILLISECONDS = 16;

            private readonly Action<Action> _post;
            private readonly Action<long> _onFrame;
            private readonly Action _deliver;

            private volatile bool _stopped;
            private long _latest;
            private int _queued;

            internal Frames(Action<Action> post, Action<long> onFrame)
            {
                _post = post;
                _onFrame = onFrame;
                _deliver = Deliver;
            }

            internal void Start()
            {
                var thread = new Thread(Run) { IsBackground = true, Name = "Ixen frames" };

                thread.Start();
            }

            private void Run()
            {
                while (!_stopped)
                {
                    if (DwmFlush() != 0)
                    {
                        Thread.Sleep(FALLBACK_MILLISECONDS);
                    }

                    if (_stopped)
                    {
                        return;
                    }

                    Interlocked.Exchange(ref _latest,
                        (long)(Stopwatch.GetTimestamp() * (1_000_000_000.0 / Stopwatch.Frequency)));

                    if (Interlocked.Exchange(ref _queued, 1) == 0)
                    {
                        _post(_deliver);
                    }
                }
            }

            private void Deliver()
            {
                Interlocked.Exchange(ref _queued, 0);

                if (!_stopped)
                {
                    _onFrame(Interlocked.Read(ref _latest));
                }
            }

            public void Dispose() => _stopped = true;
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
