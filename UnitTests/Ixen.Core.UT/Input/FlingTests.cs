using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class FlingTests
    {
        private const int VIEWPORT = 200;
        private const int STEP = 16;

        private VisualElement _viewport;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;
        private FakeTimeSource _time;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();
            _time = new FakeTimeSource();

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _viewport = new VisualElement { Name = "viewport" };
            _viewport.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _viewport.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 180 };
            _viewport.Scrollable = true;

            for (int index = 0; index < 60; index++)
            {
                var row = new VisualElement { Name = "row" };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                _viewport.AddChild(row);
            }

            root.AddChild(_viewport);

            _surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                Scheduler = _scheduler,
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Touch(float y) => _surface.PointerMove(50, y, PointerKind.Touch);

        private void Press(float y)
        {
            _surface.PointerDown(50, y, PointerButton.Left, PointerKind.Touch);
        }

        private void Release(float y)
        {
            _surface.PointerUp(50, y, PointerButton.Left, PointerKind.Touch);
        }

        private void Flick(float from, float to, int samples = 4, int perSample = STEP)
        {
            Press(from);

            float step = (to - from) / samples;

            for (int index = 1; index <= samples; index++)
            {
                _time.Now += perSample;
                Touch(from + step * index);
            }

            Release(to);
        }

        private void Ticks(int count)
        {
            for (int index = 0; index < count; index++)
            {
                _time.Now += STEP;
                _scheduler.FireAll();
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }
        }

        [TestMethod]
        public void AFlickKeepsScrollingAfterTheFingerLeaves()
        {
            Flick(170, 50);

            float atRelease = _viewport.ScrollY;

            Assert.IsTrue(atRelease > 0, "the drag itself scrolled");

            Ticks(5);

            Assert.IsTrue(_viewport.ScrollY > atRelease,
                $"a release used to stop dead; it carried {atRelease} and is now at {_viewport.ScrollY}");
        }

        [TestMethod]
        public void ItSlowsDownRatherThanRunningOn()
        {
            Flick(170, 50);

            float start = _viewport.ScrollY;

            Ticks(1);

            float first = _viewport.ScrollY - start;

            Ticks(1);

            float second = _viewport.ScrollY - start - first;

            Assert.IsTrue(second < first,
                $"friction is applied per tick, so each step is shorter: {first} then {second}");
        }

        [TestMethod]
        public void ItComesToAStop()
        {
            Flick(170, 50);

            Ticks(200);

            float settled = _viewport.ScrollY;

            Ticks(20);

            Assert.AreEqual(settled, _viewport.ScrollY,
                "below the stop velocity the entry disposes itself rather than ticking forever");
            Assert.AreEqual(0, _scheduler.PendingCount, "and the ticker is gone");
        }

        [TestMethod]
        public void ASlowDragDoesNotFling()
        {
            Flick(170, 150, samples: 4, perSample: 200);

            float atRelease = _viewport.ScrollY;

            Assert.IsTrue(atRelease > 0, "it still scrolled while the finger was down");

            Ticks(5);

            Assert.AreEqual(atRelease, _viewport.ScrollY,
                "a slow drag is a drag, not a throw, so there is nothing to carry");
            Assert.AreEqual(0, _scheduler.PendingCount);
        }

        [TestMethod]
        public void AFingerThatStopsBeforeLiftingDoesNotFling()
        {
            Press(170);

            _time.Now += STEP;
            Touch(110);
            _time.Now += STEP;
            Touch(50);

            for (int index = 0; index < 4; index++)
            {
                _time.Now += STEP;
                Touch(50);
            }

            Release(50);

            float atRelease = _viewport.ScrollY;

            Ticks(5);

            Assert.AreEqual(atRelease, _viewport.ScrollY,
                "holding still before lifting is how you stop a list, so the smoothed velocity has "
                + "to decay towards zero rather than remembering the throw");
        }

        [TestMethod]
        public void ALastSampleThatTailsOffDoesNotThrowTheFlingAway()
        {
            Press(170);

            float at = 170;

            for (int index = 0; index < 3; index++)
            {
                at -= 40;
                _time.Now += STEP;
                Touch(at);
            }

            at -= 5;
            _time.Now += STEP;
            Touch(at);

            Release(at);

            float start = _viewport.ScrollY;

            Ticks(10);

            float carried = _viewport.ScrollY - start;

            Assert.IsTrue(carried > 100,
                $"three fast samples then one slow one carried {carried} units. A finger decelerates "
                + "before it lifts, so taking the last sample alone would read 0.3 units a ms where "
                + "the throw was really about 1.8, and the list would barely move");
        }

        [TestMethod]
        public void OneImplausibleSampleCannotThrowTheListToTheEnd()
        {
            Press(170);

            _time.Now += STEP;
            Touch(130);

            _time.Now += 1;
            Touch(-270);

            Release(-270);

            Ticks(1);

            Assert.IsTrue(_viewport.ScrollY < _viewport.MaxScrollY,
                $"400 units in one millisecond is a touch event that arrived late and was delivered "
                + "in one go, which a real device does. Without the cap that reads as 240 units a ms "
                + "and the first tick alone lands at the end of the content: {_viewport.ScrollY} "
                + $"of {_viewport.MaxScrollY}");
        }

        [TestMethod]
        public void ATouchCancelsAFlingInFlight()
        {
            Flick(170, 50);

            Ticks(2);

            float caught = _viewport.ScrollY;

            Press(100);
            Ticks(5);

            Assert.AreEqual(caught, _viewport.ScrollY,
                "putting a finger down is how you catch a list, so the throw has to be dropped "
                + "before the next tick rather than fighting the finger");
        }

        [TestMethod]
        public void ItStopsAtTheEndOfTheContent()
        {
            _viewport.ScrollY = _viewport.MaxScrollY - 4;

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Flick(170, 50);

            Ticks(30);

            Assert.AreEqual(_viewport.MaxScrollY, _viewport.ScrollY,
                "the clamp holds it, and the fling gives up rather than ticking against the edge");
            Assert.AreEqual(0, _scheduler.PendingCount);
        }

        [TestMethod]
        public void AMouseDragNeitherPansNorFlings()
        {
            _surface.PointerDown(50, 170, PointerButton.Left);
            _surface.PointerMove(50, 110);
            _surface.PointerMove(50, 50);
            _surface.PointerUp(50, 50, PointerButton.Left);

            Assert.AreEqual(0f, _viewport.ScrollY, "a mouse drag is a drag");
            Assert.AreEqual(0, _scheduler.PendingCount);
        }

        [TestMethod]
        public void WithNoSchedulerAFlickStillScrollsAndNothingBreaks()
        {
            _surface.Scheduler = null;

            Flick(170, 50);

            Assert.IsTrue(_viewport.ScrollY > 0,
                "the drag is unaffected by there being no timer; only the throw after it is lost");
        }

        [TestMethod]
        public void DetachingTheTargetStopsTheFling()
        {
            Flick(170, 50);

            Assert.AreEqual(1, _scheduler.PendingCount);

            _surface.Root.RemoveChild(_viewport);

            Assert.AreEqual(0, _scheduler.PendingCount,
                "one more reference that must not outlive its element");
        }
    }
}
