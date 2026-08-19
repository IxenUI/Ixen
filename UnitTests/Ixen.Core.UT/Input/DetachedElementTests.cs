using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class DetachedElementTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _first;
        private VisualElement _second;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = Element("root");
            _first = Element("first");
            _second = Element("second");

            Size(_first, 100, 40);
            Size(_second, 100, 40);

            _root.AddChildren(_first, _second);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private static VisualElement Element(string name)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            return element;
        }

        private static void Size(VisualElement element, float width, float height)
        {
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
        }

        [TestMethod]
        public void RemovingTheHoveredElementClearsTheHover()
        {
            _surface.PointerMove(20, 20);

            Assert.AreEqual(_first, _surface.HoveredElement);

            _root.RemoveChild(_first);

            Assert.IsNull(_surface.HoveredElement,
                "it used to keep pointing at an element that is no longer in the tree");
        }

        [TestMethod]
        public void ADetachedElementNoLongerReceivesAStaleLeave()
        {
            _surface.PointerMove(20, 20);

            bool left = false;
            _first.PointerLeave += (sender, args) => left = true;

            _root.RemoveChild(_first);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.PointerMove(20, 20);

            Assert.IsFalse(left, "the next move used to fire Leave on a detached element");
        }

        [TestMethod]
        public void RemovingTheFocusedElementClearsTheFocus()
        {
            _first.Focusable = true;
            _surface.Focus(_first);

            Assert.AreEqual(_first, _surface.FocusedElement);

            _root.RemoveChild(_first);

            Assert.IsNull(_surface.FocusedElement);
        }

        [TestMethod]
        public void RemovingThePressedElementReleasesTheCapture()
        {
            _surface.PointerDown(20, 20, PointerButton.Left);

            Assert.AreEqual(_first, _surface.CapturedElement);
            Assert.AreEqual(_first, _surface.PressedElement);

            _root.RemoveChild(_first);

            Assert.IsNull(_surface.CapturedElement,
                "a captured element that leaves the tree can never release the pointer itself");
            Assert.IsNull(_surface.PressedElement);
        }

        [TestMethod]
        public void AnUpAfterTheCapturedElementLeftGoesToWhatIsActuallyThere()
        {
            _surface.PointerDown(20, 20, PointerButton.Left);
            _root.RemoveChild(_first);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            bool upOnSecond = false;
            _second.PointerUp += (sender, args) => upOnSecond = true;

            _surface.PointerUp(20, 20, PointerButton.Left);

            Assert.IsTrue(upOnSecond, "the second element moved up into that spot and is the real target");
        }

        [TestMethod]
        public void RemovingADescendantOfTheHoveredChainClearsIt()
        {
            VisualElement inner = Element("inner");
            Size(inner, 40, 20);
            _first.AddChild(inner);

            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.PointerMove(10, 10);

            Assert.AreEqual(inner, _surface.HoveredElement);

            _root.RemoveChild(_first);

            Assert.IsNull(_surface.HoveredElement,
                "the detach walks the whole subtree, not just the element handed to RemoveChild");
        }

        [TestMethod]
        public void RemovingAnUnrelatedElementLeavesTheHoverAlone()
        {
            _surface.PointerMove(20, 20);

            Assert.AreEqual(_first, _surface.HoveredElement);

            _root.RemoveChild(_second);

            Assert.AreEqual(_first, _surface.HoveredElement);
        }
    }
}
