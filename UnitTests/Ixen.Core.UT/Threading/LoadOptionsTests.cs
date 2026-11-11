using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Threading
{
    [TestClass]
    public class LoadOptionsTests
    {
        private const int VIEWPORT = 100;

        private IxenSurface _surface;
        private IxenHost _host;
        private SynchronizationContext _previous;
        private StoreComponent _component;
        private List<Clock> _clocks;
        private List<Attempt> _attempts;

        private sealed class Clock
        {
            internal TimeSpan Span;
            internal TaskCompletionSource<bool> Gate;
        }

        private sealed class Attempt
        {
            internal CancellationToken Token;
            internal TaskCompletionSource<string> Gate;
        }

        private class StoreComponent : Component<VisualElement>
        {
            public AsyncValue<string> Albums { get; } = new AsyncValue<string>();

            internal void Start(Func<CancellationToken, Task<string>> work, LoadOptions options)
                => Load(Albums, work, options);

            protected override void Render()
            {
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _previous = SynchronizationContext.Current;

            _surface = new IxenSurface { Styles = new StyleRegistry() };
            _host = new IxenHost(_surface, () => { });

            IxenSynchronizationContext.Install(_surface);

            _component = new StoreComponent();

            var root = new VisualElement { Name = "root" };
            root.AddChild(_component.Initialize());

            _host.Root = root;
            _clocks = new List<Clock>();
            _attempts = new List<Attempt>();

            Paint();
        }

        [TestCleanup]
        public void Restore()
        {
            SynchronizationContext.SetSynchronizationContext(_previous);
        }

        private void Paint()
        {
            using (var bitmap = new SKBitmap(VIEWPORT, VIEWPORT))
            using (var canvas = new SKCanvas(bitmap))
            {
                _host.Paint(canvas, VIEWPORT, VIEWPORT);
            }
        }

        private void Until(Func<bool> condition)
        {
            for (int round = 0; round < 500 && !condition(); round++)
            {
                Paint();
                Thread.Sleep(1);
            }

            Paint();
        }

        private AsyncValue<string> Albums => _component.Albums;

        private LoadOptions Options(int attempts = 1, int timeout = 0)
        {
            return new LoadOptions
            {
                Attempts = attempts,
                Timeout = TimeSpan.FromMilliseconds(timeout),
                Backoff = TimeSpan.FromMilliseconds(500),
                Delay = (span, token) =>
                {
                    var gate = new TaskCompletionSource<bool>();

                    token.Register(() => gate.TrySetCanceled());

                    lock (_clocks)
                    {
                        _clocks.Add(new Clock { Span = span, Gate = gate });
                    }

                    return gate.Task;
                }
            };
        }

        private Task<string> Ask(CancellationToken token)
        {
            var attempt = new Attempt { Token = token, Gate = new TaskCompletionSource<string>() };

            lock (_attempts)
            {
                _attempts.Add(attempt);
            }

            return attempt.Gate.Task;
        }

        private Attempt LastAttempt
        {
            get
            {
                lock (_attempts)
                {
                    return _attempts.Last();
                }
            }
        }

        private int AttemptCount
        {
            get
            {
                lock (_attempts)
                {
                    return _attempts.Count;
                }
            }
        }

        private Clock ClockFor(TimeSpan span)
        {
            lock (_clocks)
            {
                return _clocks.LastOrDefault(clock => clock.Span == span && !clock.Gate.Task.IsCompleted);
            }
        }

        private void Fire(int milliseconds)
        {
            TimeSpan span = TimeSpan.FromMilliseconds(milliseconds);

            Until(() => ClockFor(span) != null);

            Clock clock = ClockFor(span);

            Assert.IsNotNull(clock, $"nothing was waiting {milliseconds} ms");

            clock.Gate.SetResult(true);
        }

        [TestMethod]
        public void ALoadThatTakesTooLongFailsWithATimeout()
        {
            _component.Start(Ask, Options(timeout: 3000));
            Paint();

            Assert.IsTrue(Albums.IsLoading);

            Fire(3000);
            Until(() => !Albums.IsLoading);

            Assert.IsTrue(Albums.IsFailed);
            Assert.IsInstanceOfType(Albums.Error, typeof(TimeoutException));
            Assert.IsTrue(_attempts[0].Token.IsCancellationRequested, "the abandoned request is told to stop");
        }

        [TestMethod]
        public void AnAnswerInTimeIsTaken()
        {
            _component.Start(Ask, Options(timeout: 3000));
            Paint();

            LastAttempt.Gate.SetResult("twelve albums");
            Until(() => !Albums.IsLoading);

            Assert.AreEqual("twelve albums", Albums.Value);
            Assert.IsNull(ClockFor(TimeSpan.FromMilliseconds(3000)), "the clock is stopped");
        }

        [TestMethod]
        public void AFailureIsTriedAgainAfterAGrowingPause()
        {
            _component.Start(Ask, Options(attempts: 3));
            Paint();

            LastAttempt.Gate.SetException(new InvalidOperationException("offline"));
            Fire(500);
            Until(() => AttemptCount == 2);

            Assert.IsTrue(Albums.IsLoading, "still loading between attempts: the view has nothing new to show");

            LastAttempt.Gate.SetException(new InvalidOperationException("offline"));
            Fire(1000);
            Until(() => AttemptCount == 3);

            LastAttempt.Gate.SetResult("twelve albums");
            Until(() => !Albums.IsLoading);

            Assert.IsTrue(Albums.IsReady);
            Assert.AreEqual(3, AttemptCount);
        }

        [TestMethod]
        public void AfterTheLastAttemptItFailsWithTheLastError()
        {
            _component.Start(Ask, Options(attempts: 2));
            Paint();

            LastAttempt.Gate.SetException(new InvalidOperationException("offline"));
            Fire(500);
            Until(() => AttemptCount == 2);

            LastAttempt.Gate.SetException(new InvalidOperationException("still offline"));
            Until(() => !Albums.IsLoading);

            Assert.IsTrue(Albums.IsFailed);
            Assert.AreEqual("still offline", Albums.Message);
            Assert.AreEqual(2, AttemptCount);
        }

        [TestMethod]
        public void ATimeoutCountsAsAFailedAttempt()
        {
            _component.Start(Ask, Options(attempts: 2, timeout: 3000));
            Paint();

            Fire(3000);
            Fire(500);
            Until(() => AttemptCount == 2);

            LastAttempt.Gate.SetResult("twelve albums");
            Until(() => !Albums.IsLoading);

            Assert.IsTrue(Albums.IsReady);
        }

        [TestMethod]
        public void AnErrorThatWillNotGoAwayIsNotRetried()
        {
            LoadOptions options = Options(attempts: 5);
            options.RetryWhen = error => !(error is UnauthorizedAccessException);

            _component.Start(Ask, options);
            Paint();

            LastAttempt.Gate.SetException(new UnauthorizedAccessException("signed out"));
            Until(() => !Albums.IsLoading);

            Assert.IsTrue(Albums.IsFailed);
            Assert.AreEqual(1, AttemptCount);
        }

        [TestMethod]
        public void CancellingDuringThePauseStopsForGood()
        {
            _component.Start(Ask, Options(attempts: 3));
            Paint();

            LastAttempt.Gate.SetException(new InvalidOperationException("offline"));
            Until(() => ClockFor(TimeSpan.FromMilliseconds(500)) != null);

            Clock pause = ClockFor(TimeSpan.FromMilliseconds(500));

            Albums.Cancel();
            Until(() => pause.Gate.Task.IsCompleted);

            Assert.IsTrue(pause.Gate.Task.IsCanceled, "the pause itself is cancelled");
            Assert.IsTrue(Albums.IsIdle);
            Assert.AreEqual(1, AttemptCount, "no second attempt");
        }

        [TestMethod]
        public void RetryReplaysTheWholePolicy()
        {
            _component.Start(Ask, Options(attempts: 2));
            Paint();

            LastAttempt.Gate.SetException(new InvalidOperationException("offline"));
            Fire(500);
            Until(() => AttemptCount == 2);
            LastAttempt.Gate.SetException(new InvalidOperationException("offline"));
            Until(() => Albums.IsFailed);

            Albums.Retry();
            Paint();

            Assert.AreEqual(3, AttemptCount);

            LastAttempt.Gate.SetException(new InvalidOperationException("offline"));
            Fire(500);
            Until(() => AttemptCount == 4);

            Assert.IsTrue(Albums.IsLoading, "two attempts again, not one");
        }

        [TestMethod]
        public void ThePauseDoubles()
        {
            var options = new LoadOptions { Backoff = TimeSpan.FromMilliseconds(250) };

            Assert.AreEqual(250, options.BackoffBefore(2).TotalMilliseconds);
            Assert.AreEqual(500, options.BackoffBefore(3).TotalMilliseconds);
            Assert.AreEqual(1000, options.BackoffBefore(4).TotalMilliseconds);
        }
    }
}
