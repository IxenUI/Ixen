using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class RubberBandTests
    {
        private const int VIEWPORT = 200;
        private const int STEP = 16;

        private VisualElement _root;
        private VisualElement _viewport;
        private VisualElement _first;
        private IxenSurface _surface;
        private FakeScheduler _scheduler;
        private FakeTimeSource _time;

        [TestInitialize]
        public void Setup()
        {
            _scheduler = new FakeScheduler();
            _time = new FakeTimeSource();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _viewport = new VisualElement { Name = "viewport" };
            _viewport.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            _viewport.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 180 };
            _viewport.Scrollable = true;

            for (int index = 0; index < 60; index++)
            {
                var row = new VisualElement { Name = "row" };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                _viewport.AddChild(row);

                if (index == 0)
                {
                    _first = row;
                }
            }

            _root.AddChild(_viewport);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                Scheduler = _scheduler,
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Press(float y)
            => _surface.PointerDown(50, y, PointerButton.Left, PointerKind.Touch);

        private void Touch(float y)
        {
            _time.Now += STEP;
            _surface.PointerMove(50, y, PointerKind.Touch);
        }

        private void Release(float y) => Release(50, y);

        private void Release(float x, float y)
            => _surface.PointerUp(x, y, PointerButton.Left, PointerKind.Touch);

        private void Pull(float from, float to)
        {
            Press(from);
            Touch(to);
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

        private void Behaviour(OverscrollKind kind)
        {
            _viewport.Styles.Overscroll = new OverscrollStyleDescriptor { X = kind, Y = kind };
            _viewport.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void DraggingPastTheTopPullsTheContent()
        {
            Pull(50, 110);

            Assert.IsTrue(_viewport.OverscrollY < 0f,
                "a list already at the top used to meet a hard stop; the content follows the finger now");
        }

        [TestMethod]
        public void TheOffsetItselfNeverLeavesItsRange()
        {
            Pull(50, 110);

            Assert.AreEqual(0f, _viewport.ScrollY,
                "the pull is a separate offset, so ScrollY, MaxScrollY and the scrollbar thumb are "
                + "exactly what they were");
        }

        [TestMethod]
        public void ThePullIsDampedRatherThanFollowingTheFinger()
        {
            Pull(50, 110);

            float pulled = -_viewport.OverscrollY;

            Assert.IsTrue(pulled > 0f && pulled < 60f,
                $"sixty units of finger gave {pulled} units of content, which is what makes it feel "
                + "like rubber rather than a loose sheet");
        }

        [TestMethod]
        public void ThePullSaturates()
        {
            Press(50);

            for (int index = 1; index <= 40; index++)
            {
                Touch(50 + index * 40);
            }

            Assert.IsTrue(-_viewport.OverscrollY < _viewport.ContentHeight,
                "an arbitrarily long drag cannot pull the content off the screen");
        }

        [TestMethod]
        public void DraggingBackCancelsThePullBeforeItScrolls()
        {
            Press(50);
            Touch(110);

            float pulled = _viewport.OverscrollY;

            Touch(90);

            Assert.IsTrue(_viewport.OverscrollY > pulled && _viewport.OverscrollY < 0f,
                "coming back releases the band");

            Assert.AreEqual(0f, _viewport.ScrollY,
                "and nothing scrolls until the band is spent");

            Touch(20);

            Assert.AreEqual(0f, _viewport.OverscrollY, "the band is spent");
            Assert.IsTrue(_viewport.ScrollY > 0f, "so the rest of the finger scrolls");
        }

        [TestMethod]
        public void ReleasingBringsItBack()
        {
            Pull(50, 110);
            Release(110);

            Assert.IsTrue(_viewport.OverscrollY < 0f, "the release does not snap it");

            Ticks(60);

            Assert.AreEqual(0f, _viewport.OverscrollY, "the band closes on the shared ticker");
            Assert.AreEqual(0, _scheduler.PendingCount, "and stops rather than ticking forever");
        }

        [TestMethod]
        public void TheReturnIsProgressive()
        {
            Pull(50, 130);
            Release(130);

            float atRelease = _viewport.OverscrollY;

            Ticks(2);

            float midway = _viewport.OverscrollY;

            Assert.IsTrue(midway > atRelease && midway < 0f,
                $"it eases back rather than jumping: {atRelease} then {midway}");
        }

        [TestMethod]
        public void ThePullMovesTheChildren()
        {
            float resting = _first.Y;

            Pull(50, 110);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(resting - _viewport.OverscrollY, _first.Y, 0.01f,
                "arrange reads the pull the same way it reads the scroll offset, so hit testing and "
                + "clipping follow with no change of their own");
        }

        [TestMethod]
        public void NoneRefusesToBounce()
        {
            Behaviour(OverscrollKind.None);

            Pull(50, 110);

            Assert.AreEqual(0f, _viewport.OverscrollY,
                "none is what finally tells contain and none apart: both refuse to chain, only "
                + "none refuses the band as well");
        }

        [TestMethod]
        public void ContainStillBounces()
        {
            Behaviour(OverscrollKind.Contain);

            Pull(50, 110);

            Assert.IsTrue(_viewport.OverscrollY < 0f,
                "contain is about what the gesture does to the ancestors, not about the edge");
        }

        [TestMethod]
        public void AutoBounces()
        {
            Behaviour(OverscrollKind.Auto);

            Pull(50, 110);

            Assert.IsTrue(_viewport.OverscrollY < 0f, "so does auto, and so does saying nothing");
        }

        [TestMethod]
        public void CatchingABounceSnapsItBack()
        {
            Pull(50, 110);
            Release(110);

            Ticks(1);

            Assert.IsTrue(_viewport.OverscrollY < 0f, "a bounce is running");

            Press(100);

            Assert.AreEqual(0f, _viewport.OverscrollY,
                "catching a moving list drops the band rather than letting the finger fight it");

            Ticks(4);

            Assert.AreEqual(0f, _viewport.OverscrollY, "and it stays dropped");
        }

        [TestMethod]
        public void WithNoSchedulerTheReleaseSnapsBack()
        {
            _surface.Scheduler = null;

            Pull(50, 110);

            Assert.IsTrue(_viewport.OverscrollY < 0f, "the drag itself is unaffected");

            Release(110);

            Assert.AreEqual(0f, _viewport.OverscrollY,
                "a host with no timer loses the animation, not the model; nothing may stay pulled");
        }

        [TestMethod]
        public void DetachingTheElementDuringABounceStopsIt()
        {
            Pull(50, 110);
            Release(110);

            Ticks(1);

            _root.RemoveChild(_viewport);

            Assert.AreEqual(0f, _viewport.OverscrollY);
            Assert.AreEqual(0, _scheduler.PendingCount,
                "one more reference that must not outlive its element");
        }

        [TestMethod]
        public void AWheelDoesNotBounce()
        {
            _surface.PointerWheel(50, 100, 0, -3);
            _surface.PointerWheel(50, 100, 0, -3);

            Assert.AreEqual(0f, _viewport.OverscrollY,
                "no platform rubber-bands a notched wheel, and Ixen counts notches");
        }

        [TestMethod]
        public void AnElementThatStopsBeingScrollableLosesItsPull()
        {
            Pull(50, 110);

            Assert.IsTrue(_viewport.OverscrollY < 0f);

            _viewport.Scrollable = false;

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0f, _viewport.OverscrollY,
                "measure zeroes it beside the clamp, so a pull cannot survive what produced it");
        }

        [TestMethod]
        public void APullDownAndAPullUpAreSymmetric()
        {
            _viewport.ScrollY = _viewport.MaxScrollY;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Pull(110, 50);

            Assert.IsTrue(_viewport.OverscrollY > 0f, "the bottom edge pulls the other way");
            Assert.AreEqual(_viewport.MaxScrollY, _viewport.ScrollY);
        }
        [TestMethod]
        public void NoneRefusesToBounceEvenWhenTheEdgeArrivesMidGesture()
        {
            Behaviour(OverscrollKind.None);

            _viewport.ScrollY = _viewport.MaxScrollY - 10;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Press(110);
            Touch(40);

            Assert.AreEqual(_viewport.MaxScrollY, _viewport.ScrollY,
                "ten of the seventy units were real scrolling");

            Assert.AreEqual(0f, _viewport.OverscrollY,
                "the pan was chosen while the list could still move, so the refusal has to hold "
                + "inside the gesture as well as when choosing what to pan");
        }

        [TestMethod]
        public void ContainBouncesWhenTheEdgeArrivesMidGesture()
        {
            Behaviour(OverscrollKind.Contain);

            _viewport.ScrollY = _viewport.MaxScrollY - 10;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Press(110);
            Touch(40);

            Assert.IsTrue(_viewport.OverscrollY > 0f,
                "and the same gesture on a list that does bounce carries the rest into the band");
        }

        private void PressAt(float x, float y)
            => _surface.PointerDown(x, y, PointerButton.Left, PointerKind.Touch);

        private void TouchAt(float x, float y)
        {
            _time.Now += STEP;
            _surface.PointerMove(x, y, PointerKind.Touch);
        }

        private VisualElement Surface(string name, float childWidth, float childHeight)
        {
            var box = new VisualElement { Name = name };
            box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            box.Scrollable = true;

            var content = new VisualElement { Name = name + "_content" };
            content.Styles.Width = new WidthStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = childWidth
            };

            content.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = childHeight
            };

            box.AddChild(content);

            _root.RemoveChild(_viewport);
            _root.AddChild(box);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return box;
        }

        [TestMethod]
        public void AVerticalListDoesNotPullSideways()
        {
            PressAt(50, 50);
            TouchAt(58, 110);

            Assert.IsTrue(_viewport.OverscrollY < 0f, "the axis it scrolls on pulls");

            Assert.AreEqual(0f, _viewport.OverscrollX,
                "Kevin found this on a phone: every wobble of the finger used to accumulate "
                + "sideways on a list that cannot scroll sideways at all");
        }

        [TestMethod]
        public void AnAxisWithNothingToScrollNeverPulls()
        {
            PressAt(50, 100);
            TouchAt(120, 100);

            Assert.AreEqual(0f, _viewport.OverscrollX,
                "and a deliberate sideways drag does not pull either, because the band is "
                + "refused on an axis with no overflow rather than merely being unlikely");
        }

        [TestMethod]
        public void TheBandFollowsTheAxisTheGestureStartedOn()
        {
            VisualElement box = Surface("both", 300, 300);

            Assert.IsTrue(box.MaxScrollX > 0f && box.MaxScrollY > 0f,
                "this one really does scroll both ways");

            PressAt(40, 40);
            TouchAt(60, 100);

            Assert.IsTrue(box.OverscrollY < 0f, "the dominant axis pulls");

            Assert.AreEqual(0f, box.OverscrollX,
                "and the other one does not, although it could have: a diagonal drag reads as "
                + "one gesture, not two");
        }

        [TestMethod]
        public void TheAxisIsKeptForTheWholeGesture()
        {
            VisualElement box = Surface("both", 300, 300);

            PressAt(40, 40);
            TouchAt(40, 100);
            TouchAt(160, 104);

            Assert.AreEqual(0f, box.OverscrollX,
                "the axis is decided once, when the drag crosses its threshold, so turning "
                + "the finger mid-gesture cannot start a second band");
        }

        [TestMethod]
        public void AHorizontalSurfacePullsSideways()
        {
            VisualElement box = Surface("wide", 300, 40);

            Assert.IsTrue(box.MaxScrollX > 0f && box.MaxScrollY == 0f,
                "it overflows sideways only");

            PressAt(40, 40);
            TouchAt(110, 44);

            Assert.IsTrue(box.OverscrollX < 0f, "the rule is symmetric");
            Assert.AreEqual(0f, box.OverscrollY);
        }

        [TestMethod]
        public void TheBehaviourIsPerAxis()
        {
            VisualElement box = Surface("both", 300, 300);

            box.Styles.Overscroll = new OverscrollStyleDescriptor
            {
                X = OverscrollKind.None,
                Y = OverscrollKind.Auto
            };

            box.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            PressAt(40, 40);
            TouchAt(110, 44);

            Assert.AreEqual(0f, box.OverscrollX, "none on the horizontal axis");

            Release(110, 44);

            PressAt(40, 40);
            TouchAt(44, 110);

            Assert.IsTrue(box.OverscrollY < 0f,
                "and auto on the vertical one, which is the whole point of the two-value form");
        }

        private VisualElement Rows(string name, int count, float height)
        {
            var list = new VisualElement { Name = name };
            list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 80 };
            list.Scrollable = true;

            for (int index = 0; index < count; index++)
            {
                var row = new VisualElement { Name = name + "_row" };
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
                list.AddChild(row);
            }

            return list;
        }

        [TestMethod]
        public void AListWithNothingToScrollDoesNotBounce()
        {
            _root.RemoveChild(_viewport);

            VisualElement list = Rows("short", 2, 20);
            _root.AddChild(list);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.AreEqual(0f, list.MaxScrollY, "its content fits");

            Pull(20, 70);

            Assert.AreEqual(0f, list.OverscrollY,
                "a box that happens to be scrollable but has nothing to scroll is not a list, "
                + "and pulling it would be motion with no meaning");
        }

        [TestMethod]
        public void ContainStopsTheSearchForABouncer()
        {
            _root.RemoveChild(_viewport);

            VisualElement outer = Rows("outer", 6, 40);
            VisualElement inner = Rows("inner", 1, 10);

            inner.Styles.Overscroll = new OverscrollStyleDescriptor
            {
                X = OverscrollKind.Contain,
                Y = OverscrollKind.Contain
            };

            outer.InsertChild(0, inner);
            _root.AddChild(outer);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsTrue(outer.MaxScrollY > 0f && inner.MaxScrollY == 0f,
                "the outer one overflows and the inner one does not");

            Pull(20, 60);

            Assert.AreEqual(0f, outer.OverscrollY,
                "contain means the gesture stops at that list, so it must stop the search for "
                + "something to pull exactly as it stops the search for something to scroll");

            Assert.AreEqual(0f, inner.OverscrollY);
        }
    }
}
