using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class WheelLatchTests
    {
        private const int VIEWPORT = 200;
        private const float NOTCH = 48f;
        private const long PAUSE = 200;

        private FakeTimeSource _time;
        private VisualElement _root;
        private VisualElement _outer;
        private VisualElement _inner;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _time = new FakeTimeSource();

            _root = Element("root");
            _outer = Box("outer", 100, 100);
            _outer.Scrollable = true;

            _inner = Box("inner", 100, 60);
            _inner.Scrollable = true;

            _inner.AddChild(Box("innerContent", 100, 200));

            _outer.AddChildren(_inner, Box("filler", 100, 200));
            _root.AddChild(_outer);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static VisualElement Element(string name)
        {
            var element = new VisualElement { Name = name };

            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            return element;
        }

        private static VisualElement Box(string name, float width, float height)
        {
            VisualElement element = Element(name);

            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private void Notch(float x, float y, float deltaY)
        {
            _time.Now += 16;

            _surface.PointerWheel(x, y, 0, deltaY);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Down(int times)
        {
            for (int i = 0; i < times; i++)
            {
                Notch(50, 20, -1);
            }
        }

        [TestMethod]
        public void TheInnerListIsWhatMovesWhileItCan()
        {
            Down(1);

            Assert.AreEqual(NOTCH, _inner.ScrollY);
            Assert.AreEqual(0, _outer.ScrollY);
        }

        [TestMethod]
        public void AndItKeepsTheGestureAfterItRunsOut()
        {
            Down(20);

            Assert.AreEqual(_inner.MaxScrollY, _inner.ScrollY, "the inner list is at its end");
            Assert.AreEqual(0, _outer.ScrollY,
                "and the page has not moved a pixel. Without the latch the very notch that "
                + "exhausts the list hands the next one to the page, which is the jump this "
                + "exists to remove");
        }

        [TestMethod]
        public void ButAPauseEndsTheGestureAndTheNextNotchChains()
        {
            Down(20);

            _time.Now += PAUSE;

            Down(1);

            Assert.AreEqual(NOTCH, _outer.ScrollY,
                "stopping and starting again is what hands the wheel to the page - the same "
                + "rule the browsers use, and the reason the hand-over reads as deliberate");
        }

        [TestMethod]
        public void AGestureThatStartsOnAnExhaustedListChainsStraightAway()
        {
            Down(20);

            _time.Now += PAUSE;

            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, _outer.ScrollY,
                "nothing is being held back: the list could not move when this gesture began, "
                + "so the page takes the very first notch");
        }

        [TestMethod]
        public void ReversingInsideAGestureStaysOnTheSameElement()
        {
            Down(20);

            Notch(50, 20, 1);

            Assert.AreEqual(_inner.MaxScrollY - NOTCH, _inner.ScrollY);
            Assert.AreEqual(0, _outer.ScrollY,
                "the latch is the gesture's, not the direction's - scrolling back up must not "
                + "be treated as a new gesture");
        }

        [TestMethod]
        public void MovingThePointerInsideAGestureDoesNotChangeTheTarget()
        {
            VisualElement root = Element("root");
            VisualElement first = Box("first", 100, 60);
            VisualElement second = Box("second", 100, 60);

            first.Scrollable = true;
            second.Scrollable = true;

            first.AddChild(Box("firstContent", 100, 200));
            second.AddChild(Box("secondContent", 100, 200));

            root.AddChildren(first, second);

            _surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Notch(50, 20, -1);
            Notch(50, 80, -1);

            Assert.AreEqual(2 * NOTCH, first.ScrollY,
                "the pointer is over the second list, but the gesture belongs to the first");
            Assert.AreEqual(0, second.ScrollY);
        }

        [TestMethod]
        public void AnElementLeavingTheTreeIsNotHeldByTheLatch()
        {
            Down(1);

            _outer.RemoveChild(_inner);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, _outer.ScrollY,
                "the dispatchers drop a detached element immediately, and the latch is one more "
                + "reference that must go with it");
        }

        [TestMethod]
        public void AListThatStopsScrollingHandsTheGestureBack()
        {
            Down(1);

            _inner.Scrollable = false;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Notch(50, 20, -1);

            Assert.AreEqual(NOTCH, _outer.ScrollY,
                "an overflow style can be turned off between two notches, and a latch on "
                + "something that no longer scrolls would swallow the rest of the gesture");
        }

        [TestMethod]
        public void AWheelOverNothingScrollableMovesNothing()
        {
            VisualElement root = Element("root");

            root.AddChild(Box("plain", 100, 60));

            var surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                TimeSource = _time
            };

            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            surface.PointerWheel(50, 20, 0, -1);

            Assert.IsFalse(surface.IsDirty,
                "nothing moved, so nothing asks for a frame that would look identical");
        }

        [TestMethod]
        public void AndItClearsTheLatchRatherThanLeavingItArmed()
        {
            VisualElement root = Element("root");
            VisualElement first = Box("first", 100, 60);

            first.Scrollable = true;
            first.AddChild(Box("firstContent", 100, 200));

            root.AddChildren(first, Box("plain", 100, 60));

            _surface = new IxenSurface(root)
            {
                Styles = new StyleRegistry(),
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Notch(50, 20, -1);

            _time.Now += PAUSE;

            Notch(50, 80, -1);
            Notch(50, 80, -1);

            Assert.AreEqual(NOTCH, first.ScrollY,
                "a gesture over something that scrolls nothing must not leave the previous "
                + "target armed, or the second notch of it revives a list the pointer left "
                + "long ago");
        }
    }
}
