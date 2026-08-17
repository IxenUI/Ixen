using Ixen.Core.UT.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class TransitionTests
    {
        private const int VIEWPORT = 200;
        private const string FROM = "#FF000000";
        private const string TO = "#FF0000FF";

        private FakeScheduler _scheduler;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = FROM };
            WithTransition(64);
            root.AddChild(_box);

            _surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                Scheduler = _scheduler
            };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private Color Current => _box.Animations.For(Visual.Styles.StyleIdentifier.BACKGROUND).Current;

        private void WithTransition(int milliseconds)
        {
            _box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { Visual.Styles.StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = milliseconds } } }
            };
        }

        private void ChangeTo(string color)
        {
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = color };
            _box.Invalidate();
            Layout();
        }

        [TestMethod]
        public void WithNoTransitionTheColourChangesAtOnce()
        {
            _box.Styles.Transition = new TransitionStyleDescriptor();
            _box.Invalidate();
            Layout();

            ChangeTo(TO);

            Assert.AreEqual(new Color(TO), Current);
            Assert.AreEqual(0, _scheduler.PendingCount, "nothing to tick");
        }

        [TestMethod]
        public void ATransitionWalksFromTheOldColourToTheNewOne()
        {
            ChangeTo(TO);

            Assert.AreEqual(new Color(FROM), Current, "it starts where it was");
            Assert.AreEqual(1, _scheduler.PendingCount, "and a ticker is running");

            _scheduler.FireAll();
            Color quarter = Current;

            Assert.AreNotEqual(new Color(FROM), quarter);
            Assert.AreNotEqual(new Color(TO), quarter, "a quarter of the way is neither end");

            _scheduler.FireAll();
            _scheduler.FireAll();
            _scheduler.FireAll();

            Assert.AreEqual(new Color(TO), Current, "four ticks of 16ms cover 64ms");
        }

        [TestMethod]
        public void TheTickerStopsWhenTheTransitionEnds()
        {
            WithTransition(32);
            _box.Invalidate();
            Layout();

            ChangeTo(TO);

            _scheduler.FireAll();
            _scheduler.FireAll();

            Assert.AreEqual(new Color(TO), Current);
            Assert.AreEqual(0, _scheduler.PendingCount, "an idle element does not keep a timer alive");
        }

        [TestMethod]
        public void ATransitionRepaintsWithoutRelayingOut()
        {
            ChangeTo(TO);
            Layout();

            _scheduler.FireAll();

            Assert.IsTrue(_surface.IsDirty, "the new colour has to be painted");
            Assert.IsFalse(_surface.Root.IsLayoutDirty, "but nothing moved");
        }

        [TestMethod]
        public void ChangingTargetMidFlightRestartsFromWhereItIs()
        {
            ChangeTo(TO);

            _scheduler.FireAll();
            Color midway = Current;

            ChangeTo("#FF00FF00");

            Assert.AreEqual(midway, Current, "it does not snap back before heading elsewhere");

            _scheduler.FireAll();
            _scheduler.FireAll();
            _scheduler.FireAll();
            _scheduler.FireAll();

            Assert.AreEqual(new Color("#FF00FF00"), Current);
        }

        [TestMethod]
        public void TheFirstStyleResolutionDoesNotAnimate()
        {
            var box = new VisualElement { Name = "fresh" };
            box.Styles.Background = new BackgroundStyleDescriptor { Color = TO };
            box.Styles.Transition = new TransitionStyleDescriptor
            {
                Specs = { { Visual.Styles.StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = 200 } } }
            };

            _surface.Root.AddChild(box);
            Layout();

            Assert.AreEqual(new Color(TO), box.Animations.For(Visual.Styles.StyleIdentifier.BACKGROUND).Current,
                "an element appears in its colour rather than fading in from nothing");
        }

        [TestMethod]
        public void WithNoSchedulerTheColourJumpsToItsTarget()
        {
            var surface = new IxenSurface(_surface.Root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            WithTransition(200);
            _box.Styles.Background = new BackgroundStyleDescriptor { Color = TO };
            _box.Invalidate();
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(new Color(TO), Current, "a host with no timer still shows the right colour");
        }

        [TestMethod]
        public void AStateChangeIsWhatTransitionsAreFor()
        {
            var registry = new StyleRegistry();
            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "box", new()
            {
                new BackgroundStyleDescriptor { Color = FROM },
                new TransitionStyleDescriptor
                {
                    Specs = { { Visual.Styles.StyleIdentifier.BACKGROUND, new TransitionSpec { Duration = 64 } } }
                }
            }));

            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "box:hover", new()
            {
                new BackgroundStyleDescriptor { Color = TO }
            }));

            _surface.Styles = registry;
            _surface.Root.Invalidate();
            Layout();

            Assert.AreEqual(new Color(FROM), Current);

            _box.AddState("hover");
            Layout();

            Assert.AreEqual(new Color(FROM), Current, "the hover colour is the target, not the current one");

            _scheduler.FireAll();
            _scheduler.FireAll();
            _scheduler.FireAll();
            _scheduler.FireAll();

            Assert.AreEqual(new Color(TO), Current);
        }
    }
}
