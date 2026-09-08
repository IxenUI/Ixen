using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class AttachOrderTests
    {
        private const int VIEWPORT = 200;

        private static VisualElement Root()
        {
            var root = new VisualElement { Name = "root" };

            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            return root;
        }

        [TestMethod]
        public void ASchedulerInstalledAfterTheSurfaceIsStillReachedByOnAttached()
        {
            var ticker = new TickerComponent();
            var surface = new IxenSurface(ticker) { Styles = new StyleRegistry() };
            var scheduler = new FakeScheduler();

            surface.Scheduler = scheduler;

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(ticker.IsTicking,
                "a real host builds its surface first and installs its capabilities after");
            Assert.AreEqual(1, scheduler.PendingCount, "the entry OnAttached asked for");
        }

        [TestMethod]
        public void AndItActuallyTicks()
        {
            var ticker = new TickerComponent();
            var surface = new IxenSurface(ticker) { Styles = new StyleRegistry() };
            var scheduler = new FakeScheduler();

            surface.Scheduler = scheduler;

            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            scheduler.FireAll();

            Assert.AreEqual(1, ticker.Ticks);
        }

        [TestMethod]
        public void AnAttachIsRaisedAtTheFirstFrameRatherThanAtTheAssignment()
        {
            var tracked = new LifecycleComponent();
            var surface = new IxenSurface(tracked) { Styles = new StyleRegistry() };

            Assert.AreEqual(0, tracked.Attachments,
                "the tree is attached, but the host is still being built");

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, tracked.Attachments, "the first frame is what raises it");
            Assert.IsTrue(tracked.HadHostWhenAttached);
        }

        [TestMethod]
        public void OnlyOneAttachHoweverManyFramesRun()
        {
            var tracked = new LifecycleComponent();
            var surface = new IxenSurface(tracked) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            tracked.Initialize().Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, tracked.Attachments);
        }

        [TestMethod]
        public void ADetachIsRaisedAtOnceRatherThanWaitingForAFrame()
        {
            VisualElement root = Root();
            var tracked = new LifecycleComponent();

            root.AddChild(tracked.Initialize());

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, tracked.Attachments);

            root.RemoveChild(tracked.Initialize());

            Assert.AreEqual(1, tracked.Detachments,
                "a release cannot wait for a frame the element will not be part of");
        }

        [TestMethod]
        public void AttachedAndDetachedBeforeAnyFrameRaisesNeither()
        {
            VisualElement root = Root();
            var tracked = new LifecycleComponent();

            root.AddChild(tracked.Initialize());

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            root.RemoveChild(tracked.Initialize());
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0, tracked.Attachments, "it never got a host worth having");
            Assert.AreEqual(0, tracked.Detachments,
                "and OnDetached releases what OnAttached took, so it has nothing to release");
        }

        [TestMethod]
        public void AComponentThatArrivesLaterIsAttachedOnItsOwnFrame()
        {
            VisualElement root = Root();
            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.Scheduler = new FakeScheduler();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            var ticker = new TickerComponent();

            root.AddChild(ticker.Initialize());

            Assert.IsFalse(ticker.IsTicking);

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(ticker.IsTicking, "and it reaches the whole host too");
        }

        [TestMethod]
        public void ReattachingAfterADetachRaisesBoth()
        {
            VisualElement root = Root();
            var tracked = new LifecycleComponent();
            VisualElement view = tracked.Initialize();

            root.AddChild(view);

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            root.RemoveChild(view);
            root.AddChild(view);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(2, tracked.Attachments);
            Assert.AreEqual(1, tracked.Detachments);
        }

        [TestMethod]
        public void TheTraceIsInitializedThenAttached()
        {
            var tracked = new LifecycleComponent();
            var surface = new IxenSurface(tracked) { Styles = new StyleRegistry() };

            Assert.AreEqual("initialized", string.Join(",", tracked.Trace));

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("initialized,attached", string.Join(",", tracked.Trace));
        }

        [TestMethod]
        public void APropertySetInOnAttachedStillDoesNotReachTheFirstFrame()
        {
            var tracked = new LifecycleComponent();
            var surface = new IxenSurface(tracked) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("before", tracked.Initialize().FindByName("lifecycle_stamp")?.Text,
                "Initialize already replayed the bindings and left the component clean, so the "
                    + "frame's RenderIfDirty returns before reading what OnAttached just set");

            tracked.Touch();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("after", tracked.Initialize().FindByName("lifecycle_stamp")?.Text,
                "and one SetState is what an author has to write for it");
        }

        [TestMethod]
        public void TheAttachIsRaisedBeforeTheStateIsRestored()
        {
            var saver = new LifecycleComponent();
            var written = new IxenSurface(saver) { Styles = new StyleRegistry() };

            written.ComputeLayout(VIEWPORT, VIEWPORT);

            string state = written.SaveState();

            var tracked = new LifecycleComponent();
            var surface = new IxenSurface(tracked) { Styles = new StyleRegistry() };

            surface.RestoreState(state);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual("kept", tracked.Restored, "the state did arrive");
            Assert.IsNull(tracked.RestoredWhenAttached,
                "and the attach comes first, which is the order the surface had "
                    + "when it raised the attach from its own constructor");
        }

        [TestMethod]
        public void ADetachAfterAnAttachThatNeverRanRaisesNothing()
        {
            VisualElement root = Root();
            var tracked = new LifecycleComponent();
            VisualElement view = tracked.Initialize();

            root.AddChild(view);

            var surface = new IxenSurface(root) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);
            root.RemoveChild(view);

            root.AddChild(view);
            root.RemoveChild(view);

            Assert.AreEqual(1, tracked.Attachments);
            Assert.AreEqual(1, tracked.Detachments,
                "the second attach never became real, so it owes no release");
        }
    }
}
