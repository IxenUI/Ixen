using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class MultiButtonTests
    {
        private const int VIEWPORT = 200;

        private List<string> _log;
        private VisualElement _root;
        private VisualElement _left;
        private VisualElement _right;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _log = new List<string>();

            _root = Element("root", LayoutType.Row);
            _left = Box("left", 80, 80);
            _right = Box("right", 80, 80);
            _root.AddChildren(_left, _right);

            Watch(_left, "left");
            Watch(_right, "right");

            _surface = new IxenSurface(_root);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static VisualElement Element(string name, LayoutType layout)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = layout };
            return element;
        }

        private static VisualElement Box(string name, float width, float height)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        private void Watch(VisualElement element, string tag)
        {
            element.PointerDown += (s, e) => _log.Add($"down:{tag}:{e.Button}");
            element.PointerUp += (s, e) => _log.Add($"up:{tag}:{e.Button}");
            element.PointerClick += (s, e) => _log.Add($"click:{tag}:{e.Button}");
            element.PointerDragStart += (s, e) => _log.Add($"dragstart:{tag}");
            element.PointerDrag += (s, e) => _log.Add($"drag:{tag}:{e.TotalX}");
            element.PointerDragEnd += (s, e) => _log.Add($"dragend:{tag}");
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void ASecondButtonDoesNotStealTheCapture()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerDown(120, 40, PointerButton.Right);

            Assert.AreSame(_left, _surface.CapturedElement,
                "the first button to press owns the capture, so a second one cannot move it "
                + "onto whatever happens to be under the pointer");
        }

        [TestMethod]
        public void ReleasingTheSecondButtonKeepsTheCapture()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerDown(40, 40, PointerButton.Right);
            _surface.PointerUp(40, 40, PointerButton.Right);

            Assert.AreSame(_left, _surface.CapturedElement,
                "this is the bug: the second button released its own press and took the first "
                + "button's capture down with it while it was still held");
        }

        [TestMethod]
        public void ReleasingTheOwnerReleasesTheCapture()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerDown(40, 40, PointerButton.Right);
            _surface.PointerUp(40, 40, PointerButton.Left);

            Assert.IsNull(_surface.CapturedElement,
                "the capture ends with the button that took it, not with the last one held");
        }

        [TestMethod]
        public void TheSecondButtonStillGetsItsDownAndUp()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerDown(120, 40, PointerButton.Right);
            _surface.PointerUp(120, 40, PointerButton.Right);

            Assert.AreEqual("down:left:Left down:left:Right up:left:Right", Log,
                "it is dispatched to the captured element rather than dropped, and never to what "
                + "is under the pointer, which is what capture means");
        }

        [TestMethod]
        public void NoClickIsSynthesisedForTheSecondButton()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerDown(40, 40, PointerButton.Right);
            _surface.PointerUp(40, 40, PointerButton.Right);

            Assert.IsFalse(Log.Contains("click"),
                "a click in the middle of somebody else's drag is a false positive");
        }

        [TestMethod]
        public void TheOwnerStillClicksAfterwards()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerDown(40, 40, PointerButton.Right);
            _surface.PointerUp(40, 40, PointerButton.Right);
            _surface.PointerUp(40, 40, PointerButton.Left);

            Assert.IsTrue(Log.Contains("click:left:Left"),
                "the counter-case: the owning button is unaffected by the visitor");
        }

        [TestMethod]
        public void ADragKeepsItsOriginAcrossASecondButton()
        {
            _surface.PointerDown(10, 40, PointerButton.Left);
            _surface.PointerMove(30, 40);

            _log.Clear();

            _surface.PointerDown(30, 40, PointerButton.Right);
            _surface.PointerUp(30, 40, PointerButton.Right);
            _surface.PointerMove(50, 40);

            Assert.IsTrue(Log.Contains("drag:left:40"),
                $"the drag started at x 10 and the pointer is at 50, so TotalX is 40. A second "
                + $"button used to reset the press origin and the drag with it. Log was: {Log}");
        }

        [TestMethod]
        public void ASecondButtonRaisesNoDragEnd()
        {
            _surface.PointerDown(10, 40, PointerButton.Left);
            _surface.PointerMove(30, 40);

            _log.Clear();

            _surface.PointerDown(30, 40, PointerButton.Right);
            _surface.PointerUp(30, 40, PointerButton.Right);

            Assert.IsFalse(Log.Contains("dragend"),
                "the drag belongs to the button still held");
        }

        [TestMethod]
        public void AStolenCaptureLetsTheNextPressCaptureAgain()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerDown(40, 40, PointerButton.Right);
            _surface.PointerCaptureLost();

            _surface.PointerDown(120, 40, PointerButton.Left);

            Assert.AreSame(_right, _surface.CapturedElement,
                "a press while something is captured is a visitor, so losing the capture has to be "
                + "what lets the next one own it again");
        }
    }
}
