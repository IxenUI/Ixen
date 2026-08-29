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
            _menu.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            _menu.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };

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

            Layout();

            _surface.PointerDown(_first.X + 2, _first.Y + 2, PointerButton.Left);
            _surface.PointerUp(_first.X + 2, _first.Y + 2, PointerButton.Left);

            Assert.AreEqual(0, invoked, "hidden means the hit test does not see it either");
        }
    }
}
