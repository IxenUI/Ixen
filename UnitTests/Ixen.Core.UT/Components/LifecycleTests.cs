using Ixen.Core.UT.Components.Fixtures;
using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Components
{
    [TestClass]
    public class LifecycleTests
    {
        private const int VIEWPORT = 200;

        private LifecycleHostComponent _host;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _host = new LifecycleHostComponent();
            _surface = new IxenSurface(_host) { Styles = new StyleRegistry() };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Show(bool show)
        {
            _host.Show = show;
            _host.Refresh();
            Layout();
        }

        private LifecycleComponent Tracked()
            => _host.Initialize().FindByName("tracked")?.Owner as LifecycleComponent;

        [TestMethod]
        public void OpeningARegionAttachesTheComponentInside()
        {
            Layout();
            Assert.IsNull(Tracked(), "the region starts closed");

            Show(true);

            LifecycleComponent tracked = Tracked();

            Assert.IsNotNull(tracked);
            Assert.AreEqual(1, tracked.Attachments);
            Assert.AreEqual(0, tracked.Detachments);
        }

        [TestMethod]
        public void ClosingARegionDetachesTheComponentInside()
        {
            Layout();
            Show(true);

            LifecycleComponent tracked = Tracked();

            Show(false);

            Assert.AreEqual(1, tracked.Detachments);
            Assert.IsNull(Tracked(), "and the element really is out of the tree");
        }

        [TestMethod]
        public void InitializationComesBeforeAttachment()
        {
            Layout();
            Show(true);

            CollectionAssert.AreEqual(
                new[] { "initialized", "attached" },
                Tracked().Trace.ToArray(),
                "props are set and the view exists before the tree has a host");
        }

        [TestMethod]
        public void AttachmentIsWhereAHostIsFinallyReachable()
        {
            Layout();
            Show(true);

            Assert.IsTrue(Tracked().HadHostWhenAttached,
                "which is the whole point: OnInitialized runs before the view is in a tree");
        }

        [TestMethod]
        public void DetachmentSeesTheHostAlreadyGone()
        {
            Layout();
            Show(true);

            LifecycleComponent tracked = Tracked();

            Show(false);

            Assert.IsFalse(tracked.HadHostWhenDetached,
                "release what you stored, not what you look up - same rule as OnHostChanged");
        }

        [TestMethod]
        public void ANestedComponentIsNotifiedToo()
        {
            Layout();
            Show(true);

            LifecycleInnerComponent inner = Tracked().Inner;

            Assert.IsNotNull(inner);
            Assert.AreEqual(1, inner.Attachments);

            Show(false);

            Assert.AreEqual(1, inner.Detachments,
                "the host walk covers the whole subtree, so a nested component is reached");
        }

        [TestMethod]
        public void TheHooksAreEdgeTriggered()
        {
            Layout();
            Show(true);

            LifecycleComponent tracked = Tracked();

            Layout();
            _host.Refresh();
            Layout();

            Assert.AreEqual(1, tracked.Attachments,
                "a relayout is not a re-attachment");
            Assert.AreEqual(0, tracked.Detachments);
        }

        [TestMethod]
        public void AComponentThatNeverEnteredATreeIsNotDetached()
        {
            var lone = new LifecycleComponent();
            VisualElement view = lone.Initialize();

            view.DetachHost();

            Assert.AreEqual(0, lone.Attachments);
            Assert.AreEqual(0, lone.Detachments);
        }

        [TestMethod]
        public void RemovingTheElementByHandAlsoDetaches()
        {
            Layout();
            Show(true);

            LifecycleComponent tracked = Tracked();
            VisualElement root = _host.Initialize().FindByName("lifecycle_host_root");

            root.RemoveChild(tracked.Initialize());

            Assert.AreEqual(1, tracked.Detachments);
        }

        [TestMethod]
        public void ReopeningTheRegionBuildsAFreshComponent()
        {
            Layout();
            Show(true);

            LifecycleComponent first = Tracked();

            Show(false);
            Show(true);

            LifecycleComponent second = Tracked();

            Assert.AreNotSame(first, second,
                "a region rebuilds its body, so the old component is gone for good");

            Assert.AreEqual(1, second.Attachments);
            Assert.AreEqual(0, second.Detachments);
            Assert.AreEqual(1, first.Detachments);
        }

        [TestMethod]
        public void TheRootComponentIsAttachedByTheSurface()
        {
            var lone = new LifecycleComponent();
            var surface = new IxenSurface(lone) { Styles = new StyleRegistry() };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(1, lone.Attachments);
            Assert.IsTrue(lone.HadHostWhenAttached);
        }

        [TestMethod]
        public void AKeyedReorderDoesNotDetachAnything()
        {
            var component = new ListComponent();
            component.Items.Add(new ListItem { Id = 1, Name = "one" });
            component.Items.Add(new ListItem { Id = 2, Name = "two" });

            var surface = new IxenSurface(component) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            VisualElement first = component.Initialize()
                .FindByName("list_root").Children.First(c => c.Name == "row");

            ListItem head = component.Items[0];
            component.Items.RemoveAt(0);
            component.Items.Add(head);
            component.Refresh();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNotNull(first.Host,
                "a keyed move is a list splice, not a detach - that is what keeps a caret alive");
        }

        [TestMethod]
        public void MovingBetweenTwoSurfacesIsNotALeaveAndAReturn()
        {
            var lone = new LifecycleComponent();
            var first = new IxenSurface(lone) { Styles = new StyleRegistry() };

            first.ComputeLayout(VIEWPORT, VIEWPORT);
            Assert.AreEqual(1, lone.Attachments);

            var holder = new VisualElement { Name = "holder" };
            var second = new IxenSurface(holder) { Styles = new StyleRegistry() };

            second.ComputeLayout(VIEWPORT, VIEWPORT);
            holder.AddChild(lone.Initialize());

            Assert.AreEqual(1, lone.Attachments,
                "the host changed but the component never left a tree, so nothing is raised");
            Assert.AreEqual(0, lone.Detachments);
        }

        private TickerComponent Ticker()
            => _host.Initialize().FindByName("ticker")?.Owner as TickerComponent;

        [TestMethod]
        public void AComponentCanTakeASchedulerOnAttachAndGiveItBackOnDetach()
        {
            var scheduler = new FakeScheduler();
            _surface.Scheduler = scheduler;

            Layout();
            Show(true);

            Assert.AreEqual(1, scheduler.PendingCount,
                "OnAttached is the first moment a component can reach a host at all");
            Assert.IsTrue(Ticker().IsTicking);

            TickerComponent ticker = Ticker();

            Show(false);

            Assert.AreEqual(0, scheduler.PendingCount,
                "and OnDetached is what stops it leaking when the region closes");
            Assert.IsFalse(ticker.IsTicking);
        }

        [TestMethod]
        public void TheScheduledWorkActuallyRunsWhileAttached()
        {
            var scheduler = new FakeScheduler();
            _surface.Scheduler = scheduler;

            Layout();
            Show(true);

            scheduler.FireAll();
            scheduler.FireAll();
            Layout();

            Assert.AreEqual(2, Ticker().Ticks);
            Assert.AreEqual("2 ticks", Ticker().Initialize().FindByName("ticker_label").Text);
        }

        [TestMethod]
        public void WithNoSchedulerTheComponentJustDoesNotTick()
        {
            Layout();
            Show(true);

            Assert.IsFalse(Ticker().IsTicking,
                "a host without a scheduler degrades, the same way StartAnimating does");

            Show(false);
        }
    }
}
