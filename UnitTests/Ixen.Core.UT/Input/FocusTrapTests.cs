using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class FocusTrapTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private VisualElement _page;
        private VisualElement _behind;
        private VisualElement _layer;
        private VisualElement _first;
        private VisualElement _second;
        private IxenSurface _surface;

        private static VisualElement Focusable(string name)
        {
            var element = new VisualElement { Name = name, Focusable = true };
            element.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            return element;
        }

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _page = new VisualElement { Name = "page" };
            _page.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _behind = Focusable("behind");
            _page.AddChildren(_behind, Focusable("behind2"));

            _layer = new VisualElement { Name = "layer" };
            _layer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            _layer.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _layer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            _first = Focusable("first");
            _second = Focusable("second");
            _layer.AddChildren(_first, _second);

            _root.AddChildren(_page, _layer);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private void Tab(bool backwards = false)
        {
            _surface.KeyDown(Key.Tab, backwards ? KeyModifiers.Shift : KeyModifiers.None);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        [TestMethod]
        public void WithoutModalTabWalksTheWholeTree()
        {
            _surface.Focus(_second);

            Tab();

            Assert.AreEqual(_behind, _surface.FocusedElement,
                "an ordinary layer is a dropdown or a tooltip, not a trap - Tab carries on into "
                + "the page behind it and wraps");
        }

        [TestMethod]
        public void AModalLayerKeepsTabInside()
        {
            _layer.Modal = true;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.Focus(_second);

            Tab();

            Assert.AreEqual(_first, _surface.FocusedElement,
                "the last focusable of a modal wraps to its first instead of leaking into the "
                + "page underneath, which is the whole point of a modal dialog");
        }

        [TestMethod]
        public void AndShiftTabWrapsTheOtherWay()
        {
            _layer.Modal = true;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.Focus(_first);

            Tab(backwards: true);

            Assert.AreEqual(_second, _surface.FocusedElement);
        }

        [TestMethod]
        public void FocusComingFromOutsideEntersTheModal()
        {
            _layer.Modal = true;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.Focus(_behind);

            Tab();

            Assert.AreEqual(_first, _surface.FocusedElement,
                "the focused element is not in the modal's list at all, so Tab starts at its "
                + "beginning rather than doing nothing");
        }

        [TestMethod]
        public void TheTopmostModalWins()
        {
            _layer.Modal = true;

            var second = new VisualElement { Name = "second_layer", Modal = true };
            second.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            second.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            second.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            VisualElement only = Focusable("only");
            second.AddChild(only);

            _root.AddChild(second);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.Focus(_first);

            Tab();

            Assert.AreEqual(only, _surface.FocusedElement,
                "layers are ordered by z-index then declaration, so a dialog opened on top of a "
                + "dialog is the one that traps");
        }

        [TestMethod]
        public void ClosingTheModalGivesTheTreeBack()
        {
            _layer.Modal = true;
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _root.RemoveChild(_layer);
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            _surface.Focus(_behind);

            Tab();

            Assert.AreEqual("behind2", _surface.FocusedElement.Name,
                "the trap is read from the live overlay list, so nothing has to be undone when "
                + "the dialog goes away");
        }
    }
}
