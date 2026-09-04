using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
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
    public class PostedActionTests
    {
        private const int VIEWPORT = 200;

        private int _repaints;
        private VisualElement _box;
        private IxenSurface _surface;
        private IxenHost _host;
        private List<IxenErrorEventArgs> _seen;
        private SynchronizationContext _previous;

        [TestInitialize]
        public void Setup()
        {
            _repaints = 0;
            _seen = new List<IxenErrorEventArgs>();
            _previous = SynchronizationContext.Current;

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            root.AddChild(_box);

            _surface = new IxenSurface { Styles = new StyleRegistry() };
            _host = new IxenHost(_surface, () => _repaints++);
            _host.Root = root;

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

        private void Watch()
        {
            _host.UnhandledError += (sender, args) =>
            {
                _seen.Add(args);
                args.Handled = true;
            };
        }

        private void Widen()
        {
            _box.Styles.Width = new WidthStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 120
            };

            _box.Invalidate();
        }

        [TestMethod]
        public void APostedActionRunsBeforeTheNextLayoutPass()
        {
            _surface.Post(Widen);

            Assert.AreEqual(80f, _box.ActualWidth,
                "posting must not run the action there and then - it belongs to the frame");

            Paint();

            Assert.AreEqual(120f, _box.ActualWidth,
                "the drain runs before the passes, so a posted mutation is laid out in the same "
                + "frame rather than needing a second one");
        }

        [TestMethod]
        public void PostingAsksForAFrame()
        {
            int before = _repaints;

            _surface.Post(() => { });

            Assert.AreEqual(before + 1, _repaints,
                "nothing else would ask - the framework paints on demand, so a posted action that "
                + "never woke the host would sit in the queue forever");
        }

        [TestMethod]
        public void AnActionPostedByAPostedActionWaitsForTheNextFrame()
        {
            var order = new List<string>();

            _surface.Post(() =>
            {
                order.Add("first");
                _surface.Post(() => order.Add("second"));
            });

            Paint();

            Assert.AreEqual(1, order.Count,
                "the drain is bounded by what was queued when it started, or an action that posts "
                + "another would spin inside one frame");

            Paint();

            Assert.AreEqual(2, order.Count);
            Assert.AreEqual("second", order[1]);
        }

        [TestMethod]
        public void OneFailureDoesNotStopTheOthers()
        {
            bool second = false;

            Watch();

            _surface.Post(() => throw new InvalidOperationException("posted"));
            _surface.Post(() => second = true);

            Paint();

            Assert.IsTrue(second,
                "the queue holds independent continuations, so one that throws must not take the "
                + "rest of the batch with it");

            Assert.AreEqual(1, _seen.Count);
            Assert.AreEqual(IxenErrorPhase.Posted, _seen[0].Phase);
        }

        [TestMethod]
        public void AFailureStillLetsTheFrameRun()
        {
            Watch();

            _surface.Post(() => throw new InvalidOperationException("posted"));
            _surface.Post(Widen);

            Paint();

            Assert.AreEqual(120f, _box.ActualWidth);
        }

        [TestMethod]
        public void WithNoHostAPostedFailureTravels()
        {
            var surface = new IxenSurface { Styles = new StyleRegistry() };

            surface.Post(() => throw new InvalidOperationException("posted"));

            Assert.Throws<InvalidOperationException>(
                () => surface.ComputeLayout(VIEWPORT, VIEWPORT),
                "unobserved, an exception still travels - the rule the error boundary states");
        }

        [TestMethod]
        public void PostingNothingIsSafe()
        {
            int before = _repaints;

            _surface.Post(null);
            Paint();

            Assert.AreEqual(before, _repaints);
        }

        [TestMethod]
        public void TheContextPostsThroughTheSurface()
        {
            var context = new IxenSynchronizationContext(_surface);
            bool ran = false;

            context.Post(state => ran = true, null);

            Assert.IsFalse(ran);

            Paint();

            Assert.IsTrue(ran);
        }

        [TestMethod]
        public void AnAwaitResumesOnTheSurfaceThread()
        {
            IxenSynchronizationContext.Install(_surface);

            int thread = Thread.CurrentThread.ManagedThreadId;
            int resumed = 0;
            bool done = false;

            var gate = new TaskCompletionSource<bool>();

            Func<Task> load = async () =>
            {
                await gate.Task;

                resumed = Thread.CurrentThread.ManagedThreadId;
                done = true;
            };

            Task running = load();

            new Thread(() => gate.SetResult(true)).Start();

            for (int frame = 0; frame < 400 && !done; frame++)
            {
                Paint();
                Thread.Sleep(1);
            }

            Assert.IsTrue(done, "the continuation never came back to the surface");
            Assert.AreEqual(thread, resumed,
                "an await inside a component has to resume where the tree can be touched, which "
                + "is the whole point of installing the context");
        }

        [TestMethod]
        public void SendOnTheOwnThreadRunsInline()
        {
            var context = new IxenSynchronizationContext(_surface);
            bool ran = false;

            context.Send(state => ran = true, null);

            Assert.IsTrue(ran,
                "there is nothing to wait for when the caller is already the surface");
        }

        [TestMethod]
        public void SendFromAnotherThreadIsRefused()
        {
            var context = new IxenSynchronizationContext(_surface);
            Exception caught = null;

            var thread = new Thread(() =>
            {
                try
                {
                    context.Send(state => { }, null);
                }
                catch (Exception error)
                {
                    caught = error;
                }
            });

            thread.Start();
            thread.Join();

            Assert.IsInstanceOfType(caught, typeof(InvalidOperationException),
                "a blocking Send waits for a frame that only the platform can ask for, so it is "
                + "refused rather than left to deadlock");
        }

        [TestMethod]
        public void TheSurfaceKnowsItsOwnThread()
        {
            bool elsewhere = true;

            var thread = new Thread(() => elsewhere = _surface.IsOwnThread);

            thread.Start();
            thread.Join();

            Assert.IsTrue(_surface.IsOwnThread);
            Assert.IsFalse(elsewhere);
        }

        [TestMethod]
        public void AnElementReachesTheQueueThroughItsHost()
        {
            bool ran = false;

            IElementHost host = _box.Host;

            Assert.IsNotNull(host);

            host.Post(() => ran = true);
            Paint();

            Assert.IsTrue(ran,
                "an element has no other route out, which is why Post belongs on the narrow "
                + "interface beside the scheduler");
        }
    }
}
