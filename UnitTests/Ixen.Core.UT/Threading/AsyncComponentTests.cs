using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Threading
{
    [TestClass]
    public class AsyncComponentTests
    {
        private const int VIEWPORT = 200;

        private IxenSurface _surface;
        private IxenHost _host;
        private List<IxenErrorEventArgs> _seen;
        private SynchronizationContext _previous;

        private class LoadingComponent : Component<VisualElement>
        {
            internal TaskCompletionSource<string> Gate = new TaskCompletionSource<string>();
            internal string State = "waiting";
            internal int Thread = -1;
            internal bool Throw;
            internal bool ThrowBeforeAwait;

            protected override void OnAttached()
            {
                Run(Load);
            }

            private async Task Load()
            {
                if (ThrowBeforeAwait)
                {
                    throw new InvalidOperationException("before the first await");
                }

                string loaded = await Gate.Task;

                if (Throw)
                {
                    throw new InvalidOperationException("after the await");
                }

                SetState(() =>
                {
                    Thread = System.Threading.Thread.CurrentThread.ManagedThreadId;
                    State = loaded;
                });
            }

            protected override void Render()
            {
                View.Text = State;
            }
        }

        private class SyncThrowComponent : Component<VisualElement>
        {
            internal bool NoTask;

            protected override void OnAttached()
            {
                if (NoTask)
                {
                    Run(() => null);
                    return;
                }

                Run(() => throw new InvalidOperationException("no task at all"));
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _seen = new List<IxenErrorEventArgs>();
            _previous = SynchronizationContext.Current;

            _surface = new IxenSurface { Styles = new StyleRegistry() };
            _host = new IxenHost(_surface, () => { });

            _host.UnhandledError += (sender, args) =>
            {
                _seen.Add(args);
                args.Handled = true;
            };

            IxenSynchronizationContext.Install(_surface);
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

        private LoadingComponent Attach(bool fail = false, bool failEarly = false)
        {
            var component = new LoadingComponent { Throw = fail, ThrowBeforeAwait = failEarly };
            var root = new VisualElement { Name = "root" };

            root.AddChild(component.Initialize());

            _host.Root = root;
            Paint();

            return component;
        }

        private void Pump(Func<bool> until)
        {
            for (int frame = 0; frame < 400 && !until(); frame++)
            {
                Paint();
                Thread.Sleep(1);
            }
        }

        [TestMethod]
        public void AnAwaitedLoadComesBackAndSetsState()
        {
            LoadingComponent component = Attach();

            Assert.AreEqual("waiting", component.State,
                "the load is still in flight, which is the state a view has to be able to show");

            new Thread(() => component.Gate.SetResult("loaded")).Start();

            Pump(() => component.State != "waiting");

            Assert.AreEqual("loaded", component.State);
            Assert.AreEqual(Thread.CurrentThread.ManagedThreadId, component.Thread,
                "SetState touches the tree, so the continuation has to be back on the surface "
                + "thread before it runs");
        }

        [TestMethod]
        public void TheViewFollowsTheLoad()
        {
            LoadingComponent component = Attach();

            new Thread(() => component.Gate.SetResult("loaded")).Start();

            Pump(() => component.State != "waiting");

            Paint();

            Assert.AreEqual("loaded", ((VisualElement)component.Initialize()).Text,
                "SetState from a continuation has to reach Render like any other state change");
        }

        [TestMethod]
        public void AFaultedLoadReachesTheErrorBoundary()
        {
            LoadingComponent component = Attach(fail: true);

            new Thread(() => component.Gate.SetResult("loaded")).Start();

            Pump(() => _seen.Count > 0);

            Assert.AreEqual(1, _seen.Count,
                "a Task nobody awaits swallows its own failure - which is exactly the silence the "
                + "error boundary exists to break");

            Assert.AreEqual(IxenErrorPhase.Posted, _seen[0].Phase);
            Assert.AreEqual("after the await", _seen[0].Error.Message);
        }

        [TestMethod]
        public void AThrowBeforeTheFirstAwaitIsStillAFaultedTask()
        {
            Attach(failEarly: true);

            Pump(() => _seen.Count > 0);

            Assert.AreEqual(1, _seen.Count,
                "an async method never throws at its caller - it hands back a faulted task even "
                + "when it fails before the first await, so this goes through the same arm");

            Assert.AreEqual("before the first await", _seen[0].Error.Message);
        }

        [TestMethod]
        public void ALambdaThatThrowsWithNoTaskAtAllIsReportedToo()
        {
            var component = new SyncThrowComponent();
            var root = new VisualElement { Name = "root" };

            root.AddChild(component.Initialize());

            _host.Root = root;
            Paint();

            Pump(() => _seen.Count > 0);

            Assert.AreEqual(1, _seen.Count,
                "a plain Func<Task> that throws before it returns anything is the one shape that "
                + "reaches the synchronous arm, and nothing else in these tests exercises it");

            Assert.AreEqual("no task at all", _seen[0].Error.Message);
        }

        [TestMethod]
        public void ALambdaThatHandsBackNoTaskIsSafe()
        {
            var component = new SyncThrowComponent { NoTask = true };
            var root = new VisualElement { Name = "root" };

            root.AddChild(component.Initialize());

            _host.Root = root;
            Paint();

            Assert.AreEqual(0, _seen.Count);
        }

        [TestMethod]
        public void AFailureIsUnwrappedRatherThanAggregated()
        {
            LoadingComponent component = Attach(fail: true);

            new Thread(() => component.Gate.SetResult("loaded")).Start();

            Pump(() => _seen.Count > 0);

            Assert.IsInstanceOfType(_seen[0].Error, typeof(InvalidOperationException),
                "the handler should see what the load threw, not the AggregateException a task "
                + "wraps it in");
        }

        [TestMethod]
        public void AFaultedLoadDoesNotStopTheFrames()
        {
            LoadingComponent component = Attach(fail: true);

            new Thread(() => component.Gate.SetResult("loaded")).Start();

            Pump(() => _seen.Count > 0);

            bool ran = false;

            _surface.Post(() => ran = true);
            Paint();

            Assert.IsTrue(ran);
        }
    }
}
