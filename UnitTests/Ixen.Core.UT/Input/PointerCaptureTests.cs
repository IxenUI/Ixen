using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class PointerCaptureTests
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

            Watch(_root, "root");
            Watch(_left, "left");
            Watch(_right, "right");

            _surface = new IxenSurface(_root);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static VisualElement Element(string name, LayoutType layout = LayoutType.Column,
            SizeUnit widthUnit = SizeUnit.Unset, float widthValue = 1,
            SizeUnit heightUnit = SizeUnit.Unset, float heightValue = 1)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = layout };
            element.Styles.Width = new WidthStyleDescriptor { Unit = widthUnit, Value = widthValue };
            element.Styles.Height = new HeightStyleDescriptor { Unit = heightUnit, Value = heightValue };
            return element;
        }

        private static VisualElement Box(string name, float width, float height)
            => Element(name, LayoutType.Column, SizeUnit.Pixels, width, SizeUnit.Pixels, height);

        private void Watch(VisualElement element, string tag)
        {
            element.PointerUp += (s, e) => _log.Add($"up:{tag}");
            element.PointerMove += (s, e) => _log.Add($"move:{tag}");
            element.PointerClick += (s, e) => _log.Add($"click:{tag}");
            element.PointerEnter += (s, e) => _log.Add($"enter:{tag}");
            element.PointerLeave += (s, e) => _log.Add($"leave:{tag}");
        }

        private string Log => string.Join(" ", _log);

        [TestMethod]
        public void ADownCapturesTheHitElement()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);

            Assert.AreSame(_left, _surface.CapturedElement);
        }

        [TestMethod]
        public void AnUpReleasesTheCapture()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerUp(40, 40, PointerButton.Left);

            Assert.IsNull(_surface.CapturedElement);
        }

        [TestMethod]
        public void ADownOnNothingCapturesNothing()
        {
            _surface.PointerDown(-5, -5, PointerButton.Left);

            Assert.IsNull(_surface.CapturedElement);
        }

        [TestMethod]
        public void AnUpAwayFromThePressReachesThePressedElement()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _log.Clear();

            _surface.PointerUp(120, 40, PointerButton.Left);

            Assert.IsTrue(Log.Contains("up:left"),
                $"the pressed element must learn the press ended, got: {Log}");
            Assert.IsFalse(Log.Contains("up:right"),
                "the element under the cursor must not receive the up");
        }

        [TestMethod]
        public void AnUpAwayFromThePressIsNotAClick()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _log.Clear();

            _surface.PointerUp(120, 40, PointerButton.Left);

            Assert.IsFalse(Log.Contains("click"), $"no click expected, got: {Log}");
        }

        [TestMethod]
        public void DraggingOffAndBackStillClicks()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerMove(120, 40);
            _surface.PointerMove(40, 40);
            _log.Clear();

            _surface.PointerUp(40, 40, PointerButton.Left);

            Assert.AreEqual("up:left up:root click:left click:root", Log);
        }

        [TestMethod]
        public void MovesDuringACaptureGoToTheCapturedElement()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _log.Clear();

            _surface.PointerMove(120, 40);

            Assert.IsTrue(Log.Contains("move:left"), $"got: {Log}");
            Assert.IsFalse(Log.Contains("move:right"),
                "the element under the cursor must not receive moves during a capture");
        }

        [TestMethod]
        public void NothingElseIsHoveredDuringACapture()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _log.Clear();

            _surface.PointerMove(120, 40);

            Assert.IsFalse(Log.Contains("enter:right"),
                $"dragging over a sibling must not light it up, got: {Log}");
        }

        [TestMethod]
        public void TheCapturedElementStillTracksHover()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _log.Clear();

            _surface.PointerMove(120, 40);

            Assert.AreEqual("leave:left move:left move:root", Log,
                "dragging off the pressed element un-hovers it");

            _log.Clear();
            _surface.PointerMove(40, 40);

            Assert.AreEqual("enter:left move:left move:root", Log,
                "dragging back re-hovers it");
        }

        [TestMethod]
        public void ADescendantOfTheCapturedElementKeepsItHovered()
        {
            VisualElement root = Element("root");
            VisualElement card = Box("card", 100, 100);
            VisualElement label = Box("label", 40, 40);
            card.AddChild(label);
            root.AddChild(card);

            Watch(card, "card");
            Watch(label, "label");

            var surface = new IxenSurface(root);
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            surface.PointerDown(70, 70, PointerButton.Left);

            Assert.AreSame(card, surface.CapturedElement, "the press landed on the card, not the label");

            _log.Clear();
            surface.PointerMove(20, 20);

            Assert.IsTrue(Log.Contains("enter:label"),
                $"a descendant of the capture is still hoverable, got: {Log}");
        }

        [TestMethod]
        public void HoverResumesNormallyAfterTheRelease()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _surface.PointerMove(120, 40);
            _log.Clear();

            _surface.PointerUp(120, 40, PointerButton.Left);

            Assert.IsTrue(Log.Contains("enter:right"),
                $"releasing over another element hovers it, got: {Log}");

            _log.Clear();
            _surface.PointerMove(120, 60);

            Assert.IsTrue(Log.Contains("move:right"), $"normal routing is back, got: {Log}");
        }

        [TestMethod]
        public void LosingTheCaptureClearsTheState()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);

            _surface.PointerCaptureLost();

            Assert.IsNull(_surface.CapturedElement);

            _log.Clear();
            _surface.PointerUp(40, 40, PointerButton.Left);

            Assert.IsFalse(Log.Contains("click"),
                "the press was cancelled with the capture, so no click");
        }

        [TestMethod]
        public void LeavingTheSurfaceIsIgnoredWhileCaptured()
        {
            _surface.PointerDown(40, 40, PointerButton.Left);
            _log.Clear();

            _surface.PointerLeaveSurface();

            Assert.AreEqual(string.Empty, Log, "a drag that goes outside the window is not a leave");
            Assert.AreSame(_left, _surface.CapturedElement);

            _surface.PointerUp(40, 40, PointerButton.Left);

            Assert.IsTrue(Log.Contains("click:left"), "coming back and releasing still clicks");
        }
    }
}
