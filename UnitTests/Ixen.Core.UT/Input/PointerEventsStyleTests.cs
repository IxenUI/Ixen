using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class PointerEventsStyleTests
    {
        private const int VIEWPORT = 200;

        private VisualElement _root;
        private VisualElement _page;
        private VisualElement _overlay;
        private VisualElement _button;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = Element("root");
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };

            _page = Box("page", 0, 0, 200, 200);
            _overlay = Box("overlay", 0, 0, 200, 200);
            _button = Box("button", 20, 20, 60, 30);

            _overlay.AddChild(_button);
            _root.AddChildren(_page, _overlay);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private static VisualElement Element(string name) => new VisualElement { Name = name };

        private static VisualElement Box(string name, float x, float y, float width, float height)
        {
            VisualElement element = Element(name);

            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            element.Styles.Left = new LeftStyleDescriptor { Unit = SizeUnit.Pixels, Value = x };
            element.Styles.Top = new TopStyleDescriptor { Unit = SizeUnit.Pixels, Value = y };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };

            return element;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Set(VisualElement element, PointerEvents value)
        {
            element.Styles.PointerEvents = new PointerEventsStyleDescriptor { Value = value };
            element.Invalidate();

            Layout();
        }

        private string Hit(float x, float y) => _surface.HitTest(x, y)?.Name;

        [TestMethod]
        public void WithoutItTheTopmostElementSwallowsEverything()
        {
            Assert.AreEqual("overlay", Hit(150, 150),
                "Ixen hits on geometry and never asks whether an element paints, so a full "
                + "viewport layer takes every click - that is the trap this style exists for");
        }

        [TestMethod]
        public void NoneLetsThePointerThrough()
        {
            Set(_overlay, PointerEvents.None);

            Assert.AreEqual("page", Hit(150, 150));
        }

        [TestMethod]
        public void AndItReachesTheDescendantsToo()
        {
            Set(_overlay, PointerEvents.None);

            Assert.AreEqual("page", Hit(40, 30),
                "the style is inherited, so a child of an element that refuses the pointer "
                + "refuses it as well - which is what makes a transparent layer transparent");
        }

        [TestMethod]
        public void ButAChildCanTakeItBack()
        {
            Set(_overlay, PointerEvents.None);
            Set(_button, PointerEvents.Auto);

            Assert.AreEqual("button", Hit(40, 30),
                "auto on a descendant is what makes the useful shape: a layer that lets clicks "
                + "through everywhere except on the few things it actually shows");
            Assert.AreEqual("page", Hit(150, 150), "and the rest of the layer still lets go");
        }

        [TestMethod]
        public void AnUnhittableElementIsStillWalkedThrough()
        {
            Set(_overlay, PointerEvents.None);
            Set(_button, PointerEvents.Auto);

            Assert.AreEqual("button", Hit(40, 30),
                "refusing the element must not stop the descent, or a reachable child inside "
                + "an unreachable parent could never be found");
        }

        [TestMethod]
        public void ItFallsThroughRatherThanFallingBack()
        {
            _root.RemoveChild(_page);
            Set(_overlay, PointerEvents.None);

            Assert.AreEqual("root", Hit(150, 150),
                "with nothing else under it the click lands on what is behind, which here is "
                + "the root - never on the element that refused it");
        }

        [TestMethod]
        public void ItComesFromXnsEndToEnd()
        {
            var source = new XnsSource("overlay { pointer-events: none }");
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors);

            var registry = new StyleRegistry();

            registry.Add(set);

            _surface.Styles = registry;
            _root.Invalidate();

            Layout();

            Assert.AreEqual("page", Hit(150, 150),
                "the ApplyStyle arm is only ever reached by a rule from a stylesheet");
        }

        [TestMethod]
        public void AnythingElseIsADiagnostic()
        {
            var source = new XnsSource("overlay { pointer-events: sometimes }");

            source.Compile();

            Assert.IsTrue(source.HasErrors);
        }

        [TestMethod]
        public void ADeclaredAutoStopsAnInheritedNone()
        {
            Set(_overlay, PointerEvents.None);

            Assert.IsTrue(_overlay.StylesHandlers.PointerEvents.Descriptor.Blocks);
            Assert.IsTrue(_button.StylesHandlers.PointerEvents.Descriptor.Blocks,
                "inherited");

            Set(_button, PointerEvents.Auto);

            Assert.IsFalse(_button.StylesHandlers.PointerEvents.Descriptor.Blocks);
        }
    }
}
