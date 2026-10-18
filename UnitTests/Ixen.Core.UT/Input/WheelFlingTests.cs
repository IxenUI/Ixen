using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class WheelFlingTests
    {
        private const int VIEWPORT = 200;
        private const int STEP = 16;
        private const float MAX_PER_TICK = 4f * STEP;

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

        private void Notch(float deltaY)
        {
            _time.Now += STEP;
            _surface.PointerWheel(50, 50, 0, deltaY);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
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
        public void AFlingAfterTheWheelCarriesTheScroll()
        {
            Notch(-1);

            float atRelease = _viewport.ScrollY;

            _surface.PointerWheelFling(0, -20);
            Ticks(5);

            Assert.IsTrue(_viewport.ScrollY > atRelease,
                $"the scroll stopped dead at {atRelease} and is at {_viewport.ScrollY}");
        }

        [TestMethod]
        public void ItSlowsDownAndStops()
        {
            Notch(-1);

            _surface.PointerWheelFling(0, -20);
            Ticks(400);

            Assert.AreEqual(0, _scheduler.PendingCount, "the fling should have run out");
        }

        [TestMethod]
        public void TheDirectionFollowsTheWheel()
        {
            Notch(-1);
            Notch(-1);
            Notch(-1);

            float before = _viewport.ScrollY;

            Notch(1);

            float afterNotch = _viewport.ScrollY;

            _surface.PointerWheelFling(0, 20);
            Ticks(3);

            Assert.IsTrue(afterNotch < before, "the notch itself scrolled up");
            Assert.IsTrue(_viewport.ScrollY < afterNotch,
                $"a fling towards the user must keep scrolling up, it went from {afterNotch} to {_viewport.ScrollY}");
        }

        [TestMethod]
        public void TheVelocityIsCapped()
        {
            Notch(-1);

            float atRelease = _viewport.ScrollY;

            _surface.PointerWheelFling(0, -100000);
            Ticks(1);

            Assert.IsTrue(_viewport.ScrollY - atRelease <= MAX_PER_TICK + 0.01f,
                $"one tick moved {_viewport.ScrollY - atRelease}, more than the {MAX_PER_TICK} the cap allows");
        }

        [TestMethod]
        public void AZeroVelocityStopsARunningFling()
        {
            Notch(-1);

            _surface.PointerWheelFling(0, -20);
            Ticks(2);

            float caught = _viewport.ScrollY;

            _surface.PointerWheelFling(0, 0);
            Ticks(5);

            Assert.AreEqual(caught, _viewport.ScrollY, "putting the fingers down must stop the inertia");
            Assert.AreEqual(0, _scheduler.PendingCount);
        }

        [TestMethod]
        public void ASecondFlingReplacesTheFirstRatherThanRunningBeside()
        {
            Notch(-1);

            _surface.PointerWheelFling(0, -20);
            Ticks(1);
            _surface.PointerWheelFling(0, -20);
            Ticks(400);

            Assert.AreEqual(0, _scheduler.PendingCount, "the first fling was left running with nothing able to stop it");
        }

        [TestMethod]
        public void ANewNotchStopsTheFling()
        {
            Notch(-1);

            _surface.PointerWheelFling(0, -20);
            Ticks(2);

            Notch(-1);

            Assert.AreEqual(0, _scheduler.PendingCount, "a fresh wheel gesture must not fight the old one");
        }

        [TestMethod]
        public void WithNoWheelBeforeItNothingFlings()
        {
            _surface.PointerWheelFling(0, -20);
            Ticks(5);

            Assert.AreEqual(0, _viewport.ScrollY);
            Assert.AreEqual(0, _scheduler.PendingCount);
        }

        [TestMethod]
        public void AnExpiredLatchDoesNotFling()
        {
            Notch(-1);

            float before = _viewport.ScrollY;

            _time.Now += 1000;
            _surface.PointerWheelFling(0, -20);
            Ticks(5);

            Assert.AreEqual(before, _viewport.ScrollY, "a fling long after the gesture has nothing to carry on");
        }

        [TestMethod]
        public void ASlowReleaseDoesNotFling()
        {
            Notch(-1);

            _surface.PointerWheelFling(0, -0.5f);

            Assert.AreEqual(0, _scheduler.PendingCount, "below the minimum velocity the release just stops");
        }

        [TestMethod]
        public void WithNoSchedulerNothingBreaks()
        {
            _surface.Scheduler = null;

            Notch(-1);

            float before = _viewport.ScrollY;

            _surface.PointerWheelFling(0, -20);

            Assert.AreEqual(before, _viewport.ScrollY);
        }
    }
}
