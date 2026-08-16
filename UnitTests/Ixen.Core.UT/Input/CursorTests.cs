using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class CursorTests
    {
        private const int VIEWPORT = 200;

        private List<CursorKind> _set;
        private VisualElement _root;
        private VisualElement _button;
        private VisualElement _label;
        private IxenSurface _surface;

        private static VisualElement Box(string name, float width, float height)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            element.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
            return element;
        }

        [TestInitialize]
        public void Setup()
        {
            _set = new List<CursorKind>();

            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _button = Box("button", 100, 50);
            _button.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Hand };

            _label = Box("label", 60, 20);
            _button.AddChild(_label);

            _root.AddChildren(_button, Box("plain", 100, 50));

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            _surface.CursorSetter = kind => _set.Add(kind);
            _set.Clear();
        }

        [TestMethod]
        public void HoveringAnElementAppliesItsCursor()
        {
            _surface.PointerMove(10, 10);

            Assert.AreEqual(CursorKind.Hand, _surface.Cursor);
            CollectionAssert.AreEqual(new[] { CursorKind.Hand }, _set);
        }

        [TestMethod]
        public void ACursorReachesInsideBecauseItLooksUpTheChain()
        {
            _surface.PointerMove(10, 10);
            _set.Clear();

            _surface.PointerMove(20, 15);

            Assert.AreEqual(CursorKind.Hand, _surface.Cursor,
                "the label has no cursor of its own, so the button's wins");
            Assert.AreEqual(0, _set.Count, "and nothing was pushed to the platform twice");
        }

        [TestMethod]
        public void AnExplicitDefaultStopsTheLookup()
        {
            _label.Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Default };
            _label.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.PointerMove(20, 15);

            Assert.AreEqual(CursorKind.Default, _surface.Cursor,
                "Unset means look further up, Default means the arrow");
        }

        [TestMethod]
        public void LeavingTheElementGoesBackToTheArrow()
        {
            _surface.PointerMove(10, 10);
            _surface.PointerMove(10, 80);

            Assert.AreEqual(CursorKind.Default, _surface.Cursor);
            CollectionAssert.AreEqual(new[] { CursorKind.Hand, CursorKind.Default }, _set);
        }

        [TestMethod]
        public void LeavingTheSurfaceGoesBackToTheArrow()
        {
            _surface.PointerMove(10, 10);
            _surface.PointerLeaveSurface();

            Assert.AreEqual(CursorKind.Default, _surface.Cursor);
        }

        [TestMethod]
        public void ThePlatformIsOnlyToldWhenItChanges()
        {
            _surface.PointerMove(10, 10);
            _surface.PointerMove(11, 11);
            _surface.PointerMove(12, 12);

            CollectionAssert.AreEqual(new[] { CursorKind.Hand }, _set,
                "one call, not one per mouse move");
        }

        [TestMethod]
        public void ATextFieldAsksForACaretWithoutBeingTold()
        {
            var field = new TextField { Name = "field" };
            field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            _root.AddChild(field);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.PointerMove(10, 110);

            Assert.AreEqual(CursorKind.Text, _surface.Cursor);
        }

        [TestMethod]
        public void AStylesheetCanOverrideAFieldsCursor()
        {
            var field = new TextField { Name = "field" };
            field.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 100 };
            field.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };
            _root.AddChild(field);

            var registry = new StyleRegistry();
            registry.Add(new StyleClass(StyleClassTarget.ElementName, null, null, "field", new()
            {
                new CursorStyleDescriptor { Value = CursorKind.Crosshair }
            }));

            _surface.Styles = registry;
            _root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.PointerMove(10, 110);

            Assert.AreEqual(CursorKind.Crosshair, _surface.Cursor,
                "the inline default a field sets is still just an inline style");
        }

        [TestMethod]
        public void WithNoSetterNothingBreaks()
        {
            var surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            surface.ComputeLayout(VIEWPORT, VIEWPORT);

            surface.PointerMove(10, 10);

            Assert.AreEqual(CursorKind.Hand, surface.Cursor, "the surface still knows, it just tells nobody");
        }
    }
}
