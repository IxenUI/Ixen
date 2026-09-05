using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    internal class FakeTimeSource : ITimeSource
    {
        internal long Now;

        public long Milliseconds => Now;
    }

    [TestClass]
    public class GestureTests
    {
        private const int VIEWPORT = 200;

        private List<string> _log;
        private FakeTimeSource _time;
        private VisualElement _box;
        private VisualElement _root;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();
            _time = new FakeTimeSource();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };
            _box.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _box.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                TimeSource = _time
            };

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private string Log => string.Join(" ", _log);

        private void WatchDrag(VisualElement element, string tag)
        {
            element.PointerDragStart += (s, e) => _log.Add($"start:{tag}({e.TotalX},{e.TotalY})");
            element.PointerDrag += (s, e) => _log.Add($"drag:{tag}({e.DeltaX},{e.DeltaY}|{e.TotalX},{e.TotalY})");
            element.PointerDragEnd += (s, e) => _log.Add($"end:{tag}({e.TotalX},{e.TotalY})");
        }

        [TestMethod]
        public void AMoveBelowTheThresholdIsNotADrag()
        {
            WatchDrag(_box, "box");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerMove(12, 12);
            _surface.PointerUp(12, 12, PointerButton.Left);

            Assert.AreEqual(string.Empty, Log, "a shaky click is still a click, not a drag");
        }

        [TestMethod]
        public void CrossingTheThresholdStartsADrag()
        {
            WatchDrag(_box, "box");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerMove(12, 12);
            _surface.PointerMove(30, 10);
            _surface.PointerMove(40, 15);
            _surface.PointerUp(40, 15, PointerButton.Left);

            Assert.AreEqual("start:box(20,0) drag:box(10,5|30,5) end:box(30,5)", Log);
        }

        [TestMethod]
        public void ADragKeepsGoingOutsideTheElement()
        {
            WatchDrag(_box, "box");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerMove(150, 150);
            _surface.PointerUp(150, 150, PointerButton.Left);

            Assert.AreEqual("start:box(140,140) end:box(140,140)", Log,
                "capture is what keeps the moves coming");
        }

        [TestMethod]
        public void ADragEndsWhenTheCaptureIsLost()
        {
            WatchDrag(_box, "box");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerMove(60, 10);
            _surface.PointerCaptureLost();

            Assert.AreEqual("start:box(50,0) end:box(50,0)", Log,
                "a stolen capture cancels the drag instead of leaving it open");
        }

        [TestMethod]
        public void DragEventsBubbleAndCarryTheirSource()
        {
            WatchDrag(_box, "box");
            WatchDrag(_root, "root");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerMove(60, 10);

            Assert.AreEqual("start:box(50,0) start:root(50,0)", Log);
        }

        [TestMethod]
        public void AHandledDragStartStopsAtTheElement()
        {
            _box.PointerDragStart += (s, e) => e.Handled = true;
            WatchDrag(_root, "root");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerMove(60, 10);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void ADragStillProducesAClickWhenItLandsOnTheSameElement()
        {
            _box.PointerClick += (s, e) => _log.Add("click");
            WatchDrag(_box, "box");

            _surface.PointerDown(10, 10, PointerButton.Left);
            _surface.PointerMove(60, 10);
            _surface.PointerUp(60, 10, PointerButton.Left);

            Assert.AreEqual("start:box(50,0) end:box(50,0) click", Log,
                "the drag ends before the click is synthesised");
        }

        [TestMethod]
        public void TwoQuickClicksOnTheSameElementMakeADoubleClick()
        {
            _box.PointerClick += (s, e) => _log.Add("click");
            _box.PointerDoubleClick += (s, e) => _log.Add("double");

            Click(10, 10);
            _time.Now = 200;
            Click(10, 10);

            Assert.AreEqual("click click double", Log, "the double comes after the second click");
        }

        [TestMethod]
        public void ASlowSecondClickIsNotADouble()
        {
            _box.PointerDoubleClick += (s, e) => _log.Add("double");

            Click(10, 10);
            _time.Now = 900;
            Click(10, 10);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void AFarSecondClickIsNotADouble()
        {
            _box.PointerDoubleClick += (s, e) => _log.Add("double");

            Click(10, 10);
            _time.Now = 100;
            Click(80, 80);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void AFingerMayLandSomewhereElseAndStillDouble()
        {
            _box.PointerDoubleClick += (s, e) => _log.Add("double");

            Tap(10, 10);
            _time.Now = 100;
            Tap(30, 30);

            Assert.AreEqual("double", Log,
                "a finger cannot land twice on the same pixel, so touch gets a wider window");
        }

        [TestMethod]
        public void AMouseStillHasToBePrecise()
        {
            _box.PointerDoubleClick += (s, e) => _log.Add("double");

            Click(10, 10);
            _time.Now = 100;
            Click(30, 30);

            Assert.AreEqual(string.Empty, Log,
                "a mouse does not wander between two clicks, so its window is unchanged");
        }

        [TestMethod]
        public void AFingerThatLandsFarAwayIsStillNotADouble()
        {
            _box.PointerDoubleClick += (s, e) => _log.Add("double");

            Tap(10, 10);
            _time.Now = 100;
            Tap(90, 90);

            Assert.AreEqual(string.Empty, Log, "the wider window is still a window");
        }

        [TestMethod]
        public void AClickOnAnotherElementResetsTheSequence()
        {
            var other = new VisualElement { Name = "other" };
            other.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            other.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            _root.AddChild(other);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _box.PointerDoubleClick += (s, e) => _log.Add("double:box");

            Click(10, 10);
            _time.Now = 100;
            Click(10, 120);
            _time.Now = 200;
            Click(10, 10);

            Assert.AreEqual(string.Empty, Log);
        }

        [TestMethod]
        public void AThirdClickStartsAFreshSequence()
        {
            _box.PointerDoubleClick += (s, e) => _log.Add("double");

            Click(10, 10);
            _time.Now = 100;
            Click(10, 10);
            _time.Now = 200;
            Click(10, 10);
            _time.Now = 300;
            Click(10, 10);

            Assert.AreEqual("double double", Log,
                "four clicks are two doubles, not three");
        }

        [TestMethod]
        public void ADoubleClickBubbles()
        {
            _root.PointerDoubleClick += (s, e) => _log.Add($"root({e.Source.Name})");

            Click(10, 10);
            _time.Now = 100;
            Click(10, 10);

            Assert.AreEqual("root(box)", Log);
        }

        private void Click(float x, float y)
        {
            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);
        }

        private void Tap(float x, float y)
        {
            _surface.PointerDown(x, y, PointerButton.Left, PointerKind.Touch);
            _surface.PointerUp(x, y, PointerButton.Left, PointerKind.Touch);
        }
    }
}
