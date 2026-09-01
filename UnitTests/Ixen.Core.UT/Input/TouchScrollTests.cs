using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class TouchScrollTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _viewport;
        private VisualElement _row;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = Element("root");

            _viewport = Element("viewport");
            _viewport.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _viewport.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            _viewport.Scrollable = true;

            for (int index = 0; index < 5; index++)
            {
                VisualElement row = Element($"row{index}");
                row.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 40 };
                _viewport.AddChild(row);

                if (index == 1)
                {
                    _row = row;
                }
            }

            _root.AddChild(_viewport);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static VisualElement Element(string name)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return element;
        }

        private void Touch(float fromY, float toY)
        {
            _surface.PointerDown(20, fromY, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, toY, PointerKind.Touch);
            _surface.PointerUp(20, toY, PointerButton.Left, PointerKind.Touch);
        }

        [TestMethod]
        public void AFingerDragScrollsTheContainer()
        {
            Assert.IsTrue(_viewport.MaxScrollY > 0, "there is something to scroll");

            Touch(60, 10);

            Assert.AreEqual(50f, _viewport.ScrollY,
                "dragging 50 units up moves the content 50 units, one to one with the finger");
        }

        [TestMethod]
        public void DraggingTheOtherWayComesBack()
        {
            Touch(60, 10);

            Assert.AreEqual(50f, _viewport.ScrollY);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            Touch(10, 60);

            Assert.AreEqual(0f, _viewport.ScrollY, "and dragging down returns to the start");
        }

        [TestMethod]
        public void AMouseDragDoesNotScroll()
        {
            _surface.PointerDown(20, 60, PointerButton.Left);
            _surface.PointerMove(20, 10);
            _surface.PointerUp(20, 10, PointerButton.Left);

            Assert.AreEqual(0f, _viewport.ScrollY,
                "drag-to-scroll is a touch gesture; a mouse drag must behave exactly as it always did");
        }

        [TestMethod]
        public void AHandlerThatClaimsTheDragKeepsIt()
        {
            var log = new List<string>();

            _row.PointerDragStart += (sender, e) =>
            {
                log.Add("start");
                e.Handled = true;
            };

            _row.PointerDrag += (sender, e) => log.Add("drag");

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, 10, PointerKind.Touch);
            _surface.PointerMove(20, 5, PointerKind.Touch);

            Assert.AreEqual(0f, _viewport.ScrollY,
                "the child asked for the gesture, so the container must not steal it");

            CollectionAssert.Contains(log, "start");
            CollectionAssert.Contains(log, "drag", "and it keeps receiving the drag");
        }

        [TestMethod]
        public void AnUnclaimedDragStartIsStillRaisedOnce()
        {
            var log = new List<string>();

            _row.PointerDragStart += (sender, e) => log.Add("start");
            _row.PointerDrag += (sender, e) => log.Add("drag");

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, 10, PointerKind.Touch);
            _surface.PointerMove(20, 5, PointerKind.Touch);

            CollectionAssert.AreEqual(new[] { "start" }, log,
                "the child gets its refusal, then hears nothing more - the container took over");

            Assert.IsTrue(_viewport.ScrollY > 0);
        }

        [TestMethod]
        public void PanningCancelsThePressAndProducesNoClick()
        {
            var registry = new StyleRegistry();

            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "row1:pressed",
                new List<StyleDescriptor> { new BackgroundStyleDescriptor { Color = "#FF0000" } }));

            _surface.Styles = registry;
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            var log = new List<string>();
            _row.PointerClick += (sender, e) => log.Add("click");

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);

            Assert.IsTrue(_row.HasState(StyleStates.PRESSED),
                "the press really starts on the child, so clearing it means something");

            _surface.PointerMove(20, 10, PointerKind.Touch);

            Assert.IsFalse(_row.HasState(StyleStates.PRESSED),
                "taking over the gesture cancels the press instead of leaving it stuck");

            _surface.PointerUp(20, 10, PointerButton.Left, PointerKind.Touch);

            Assert.AreEqual(0, log.Count, "and a scroll is not a click");
        }

        [TestMethod]
        public void ATapStillClicks()
        {
            var log = new List<string>();

            _row.PointerClick += (sender, e) => log.Add("click");

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerUp(20, 60, PointerButton.Left, PointerKind.Touch);

            CollectionAssert.AreEqual(new[] { "click" }, log,
                "a touch that does not move is still a tap");
        }

        [TestMethod]
        public void ANonScrollableAncestorIsSkipped()
        {
            _viewport.Scrollable = false;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            var log = new List<string>();
            _row.PointerDrag += (sender, e) => log.Add("drag");

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, 10, PointerKind.Touch);
            _surface.PointerMove(20, 5, PointerKind.Touch);

            CollectionAssert.Contains(log, "drag",
                "with nothing to scroll, the drag stays an ordinary drag");
        }

        [TestMethod]
        public void AContainerAtItsEndTakesTheGestureAndPulls()
        {
            _viewport.ScrollY = _viewport.MaxScrollY;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            var log = new List<string>();

            _viewport.PointerDrag += (sender, e) => log.Add("drag");

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, 10, PointerKind.Touch);
            _surface.PointerMove(20, 5, PointerKind.Touch);

            Assert.IsTrue(_viewport.OverscrollY > 0f,
                "this used to hand the gesture back as an ordinary drag because nothing could "
                + "move; the edge pulls now, which is what every phone does");

            CollectionAssert.DoesNotContain(log, "drag");
        }

        [TestMethod]
        public void UnlessItRefusesToBounce()
        {
            _viewport.Styles.Overscroll = new OverscrollStyleDescriptor
            {
                X = OverscrollKind.None,
                Y = OverscrollKind.None
            };

            _viewport.Invalidate();

            _viewport.ScrollY = _viewport.MaxScrollY;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            var log = new List<string>();

            _viewport.PointerDrag += (sender, e) => log.Add("drag");

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, 10, PointerKind.Touch);
            _surface.PointerMove(20, 5, PointerKind.Touch);

            CollectionAssert.Contains(log, "drag",
                "none is how the old behaviour is asked for: no band, so a gesture the list "
                + "cannot use is left to whatever is under the finger");
        }

        [TestMethod]
        public void AStolenCaptureEndsThePan()
        {
            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, 20, PointerKind.Touch);

            float scrolled = _viewport.ScrollY;

            Assert.IsTrue(scrolled > 0);

            _surface.PointerCaptureLost();
            _surface.PointerMove(20, 10, PointerKind.Touch);

            Assert.AreEqual(scrolled, _viewport.ScrollY,
                "losing the capture stops the pan rather than leaving it stuck to the pointer");
        }

        [TestMethod]
        public void TheDragArgsReportTheKind()
        {
            PointerKind seen = PointerKind.Mouse;

            _row.PointerDragStart += (sender, e) =>
            {
                seen = e.Kind;
                e.Handled = true;
            };

            _surface.PointerDown(20, 60, PointerButton.Left, PointerKind.Touch);
            _surface.PointerMove(20, 10, PointerKind.Touch);

            Assert.AreEqual(PointerKind.Touch, seen,
                "a handler can tell a finger from a mouse, which is what makes the rule expressible");
        }
    }
}
