using Ixen.Core;
using Ixen.Core.Accessibility;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class MenuTests
    {
        private const int VIEWPORT = 300;

        private VisualElement _root;
        private VisualElement _page;
        private Menu _menu;
        private MenuItem _first;
        private MenuItem _second;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _page = new VisualElement { Name = "page" };
            _page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _menu = new Menu { Name = "menu" };

            _first = new MenuItem { Name = "open", Text = "Open" };
            _second = new MenuItem { Name = "quit", Text = "Quit" };

            foreach (MenuItem item in new[] { _first, _second })
            {
                item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };
            }

            _menu.AddChildren(_first, _second);
            _root.AddChildren(_page, _menu);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void AMenuStartsClosedAndIsNotThere()
        {
            Assert.IsFalse(_menu.Open);
            Assert.AreEqual(0, _surface.BuildAccessibilityTree().Children.Count,
                "a closed menu is hidden, and hidden leaves the accessibility tree - this is "
                + "the whole reason visibility had to exist before this control could");
        }

        [TestMethod]
        public void OpeningShowsItAndKeepsItsItems()
        {
            _menu.Open = true;
            Layout();

            AccessibleNode tree = _surface.BuildAccessibilityTree();
            AccessibleNode menu = tree.Children.Single(c => c.Role == AccessibleRole.Menu);

            Assert.AreEqual(2, menu.Children.Count);
            Assert.AreEqual("Open", menu.Children[0].Name);
            Assert.AreEqual(AccessibleRole.MenuItem, menu.Children[0].Role);
        }

        [TestMethod]
        public void ClosingAndReopeningKeepsTheSameElements()
        {
            _menu.Open = true;
            Layout();

            _menu.Open = false;
            Layout();

            _menu.Open = true;
            Layout();

            Assert.AreSame(_first, _menu.Items.First(),
                "visibility hides, it does not destroy - an @if would have rebuilt these and "
                + "thrown away anything they held");
        }

        [TestMethod]
        public void TheMenuIsALayerAndTakesNoSpace()
        {
            _menu.Open = true;
            Layout();

            Assert.AreEqual(LayoutType.Fixed, _menu.Styles.Layout.Type,
                "the control sets this inline, and the default theme must NOT restate it - the "
                + "cascade beats inline Styles, so a #Menu { layout: column } rule silently "
                + "turned the layer back into an ordinary box");

            Assert.AreEqual(0f, _menu.BoxHeight,
                "a layer is still laid out by its parent, so it is sized 0x0 - otherwise an open "
                + "menu would push its siblings around and a closed one would leave a gap, since "
                + "visibility keeps its space");

            Assert.AreEqual(200f, _page.ActualHeight, "and the page is untouched");
        }

        [TestMethod]
        public void TheItemsFlowInAPanelRatherThanStacking()
        {
            _menu.Open = true;
            Layout();

            Assert.AreSame(_menu.Panel, _first.Parent,
                "a fixed container places every child at its own offsets, so items declared "
                + "directly under the layer would all land on the same spot - ContentHost routes "
                + "them into a column panel instead");

            Assert.AreEqual(_second.Y, _first.Y + _first.BoxHeight,
                "so the second item sits under the first");
        }

        [TestMethod]
        public void AnAnchorMovesThePanelAndItsPainting()
        {
            _menu.Styles.Anchor = new AnchorStyleDescriptor { Name = "page" };
            _menu.Styles.AnchorPlacement = new AnchorPlacementStyleDescriptor
            {
                Side = AnchorSide.Below,
                Align = AnchorAlign.Start
            };

            _menu.Open = true;
            _menu.Invalidate();
            Layout();

            Assert.AreEqual(_page.X, _menu.Panel.X,
                "the panel is what carries the background and the border, so it has to be the "
                + "thing that lands on the anchor - the layer's own box stays where its parent "
                + "put it and paints nothing");

            Assert.AreEqual(_page.Y + _page.ActualHeight, _menu.Panel.Y);
        }

        [TestMethod]
        public void ClickingAnItemInvokesItAndClosesTheMenu()
        {
            int invoked = 0;
            _first.Invoked += (sender, e) => invoked++;

            _menu.Open = true;
            Layout();

            _first.PerformClick();

            Assert.AreEqual(1, invoked);
            Assert.IsFalse(_menu.Open, "an item closes the menu it belongs to");
        }

        [TestMethod]
        public void EnterActivatesTheFocusedItem()
        {
            int invoked = 0;
            _second.Invoked += (sender, e) => invoked++;

            _menu.Open = true;
            Layout();

            _surface.Focus(_second);
            _surface.KeyDown(Key.Enter, KeyModifiers.None);

            Assert.AreEqual(1, invoked);
            Assert.IsFalse(_menu.Open);
        }

        [TestMethod]
        public void TheArrowsWalkTheItemsAndWrap()
        {
            _menu.Open = true;
            Layout();

            _surface.Focus(_first);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(_second, _surface.FocusedElement);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.AreEqual(_first, _surface.FocusedElement, "and it wraps");

            _surface.KeyDown(Key.Up, KeyModifiers.None);
            Assert.AreEqual(_second, _surface.FocusedElement, "the other way too");
        }

        [TestMethod]
        public void EscapeCloses()
        {
            _menu.Open = true;
            Layout();

            _surface.Focus(_first);
            _surface.KeyDown(Key.Escape, KeyModifiers.None);

            Assert.IsFalse(_menu.Open);
        }

        [TestMethod]
        public void ClickingOutsideCloses()
        {
            _menu.Open = true;
            Layout();

            _surface.PointerDown(10, 190, PointerButton.Left);

            Assert.IsFalse(_menu.Open,
                "a menu that stays open when you click the page behind it is broken, and Ixen "
                + "has no click-outside event - so the menu listens on the root while it is open");
        }

        [TestMethod]
        public void ClickingInsideDoesNot()
        {
            _menu.Open = true;
            Layout();

            _surface.PointerDown(_first.X + 2, _first.Y + 2, PointerButton.Left);

            Assert.IsTrue(_menu.Open,
                "the walk goes up from what was hit, so anything nested inside the menu counts "
                + "as inside");
        }


        [TestMethod]
        public void ClosedIsRaisedOnce()
        {
            int closes = 0;
            _menu.Closed += (sender, e) => closes++;

            _menu.Open = true;
            _menu.Close();
            _menu.Close();

            Assert.AreEqual(1, closes, "the setter early-returns on an unchanged value");
        }

        [TestMethod]
        public void AClosedMenuCannotBeClicked()
        {
            int invoked = 0;
            _first.Invoked += (sender, e) => invoked++;

            _menu.Open = true;
            Layout();

            float x = _first.X + 2;
            float y = _first.Y + 2;

            _menu.Open = false;
            Layout();

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerUp(x, y, PointerButton.Left);

            Assert.AreEqual(0, invoked,
                "the coordinates are taken while it is open, so this really is a click where "
                + "the item used to be - hidden means the hit test does not see it either");
        }
    }
}
