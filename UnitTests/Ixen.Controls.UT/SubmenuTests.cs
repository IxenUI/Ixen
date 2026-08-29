using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class SubmenuTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private VisualElement _page;
        private Menu _menu;
        private MenuItem _plain;
        private MenuItem _parent;
        private Menu _submenu;
        private MenuItem _deep;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _page = new VisualElement { Name = "page" };
            _page.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _menu = new Menu { Name = "menu" };

            _plain = Item("open", "Open");
            _parent = Item("recent", "Recent");
            _deep = Item("first", "First file");

            _submenu = new Menu { Name = "recent_menu" };
            _submenu.AddChild(_deep);

            _parent.AddChild(_submenu);

            _menu.AddChildren(_plain, _parent);
            _root.AddChildren(_page, _menu);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private static MenuItem Item(string name, string text)
        {
            var item = new MenuItem { Name = name, Text = text };

            item.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 120 };
            item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 24 };

            return item;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void AMenuChildIsFoundAndAnchoredToItsItem()
        {
            Assert.IsTrue(_parent.HasSubmenu);
            Assert.AreSame(_submenu, _parent.Submenu);
            Assert.AreSame(_parent, _submenu.AnchorElement,
                "AnchorElement is what makes this expressible at all - a submenu cannot name its "
                + "parent item, because the item may have no name");
            Assert.IsTrue(_parent.HasState(MenuItem.SUBMENU), "so #MenuItem:submenu can show it");
            Assert.IsFalse(_plain.HasSubmenu);
        }

        [TestMethod]
        public void ItIsFoundOnAttachRatherThanOnFirstHover()
        {
            var fresh = new MenuItem { Name = "late", Text = "Late" };
            fresh.AddChild(new Menu());

            Assert.IsFalse(fresh.HasState(MenuItem.SUBMENU), "nothing has looked yet");

            _menu.AddChild(fresh);

            Assert.IsTrue(fresh.HasState(MenuItem.SUBMENU),
                "OnHostChanged is the first moment the children are known to be there, and it "
                + "had to become reachable from outside Ixen.Core for a control to use it");
        }

        [TestMethod]
        public void ActivatingAParentOpensItInsteadOfClosingTheMenu()
        {
            int invoked = 0;
            _parent.Invoked += (sender, e) => invoked++;

            _menu.Open = true;
            Layout();

            _parent.PerformClick();

            Assert.IsTrue(_submenu.Open);
            Assert.IsTrue(_menu.Open, "the parent stays open under its own submenu");
            Assert.AreEqual(0, invoked, "a parent item is not a command");
        }

        [TestMethod]
        public void ItOpensToTheRightOfItsItem()
        {
            _menu.Open = true;
            Layout();

            _parent.OpenSubmenu();
            Layout();

            Assert.AreEqual(_parent.X + _parent.ActualWidth, _submenu.Panel.X);
            Assert.AreEqual(_parent.Y, _submenu.Panel.Y);
        }

        [TestMethod]
        public void HoveringASiblingClosesIt()
        {
            _menu.Open = true;
            Layout();

            _parent.OpenSubmenu();
            Assert.IsTrue(_submenu.Open);

            _surface.PointerMove(_plain.X + 2, _plain.Y + 2);

            Assert.IsFalse(_submenu.Open,
                "moving onto a plain sibling has to close the branch, or two levels stay open at "
                + "once and the pointer is under neither");
        }

        [TestMethod]
        public void ClosingTheParentClosesTheBranch()
        {
            _menu.Open = true;
            Layout();

            _parent.OpenSubmenu();
            Assert.IsTrue(_submenu.Open);

            _menu.Close();

            Assert.IsFalse(_submenu.Open, "a closed menu must not leave a layer behind it");
        }

        [TestMethod]
        public void ChoosingInASubmenuClosesTheWholeThing()
        {
            int invoked = 0;
            _deep.Invoked += (sender, e) => invoked++;

            _menu.Open = true;
            Layout();

            _parent.OpenSubmenu();
            Layout();

            _deep.PerformClick();

            Assert.AreEqual(1, invoked);
            Assert.IsFalse(_submenu.Open);
            Assert.IsFalse(_menu.Open,
                "the item closes its own menu, and closing that menu is what the parent's "
                + "click-outside walk then finishes");
        }

        [TestMethod]
        public void RightOpensAndLeftGoesBack()
        {
            _menu.Open = true;
            Layout();

            _surface.Focus(_parent);
            _surface.KeyDown(Key.Right, KeyModifiers.None);

            Assert.IsTrue(_submenu.Open);
            Assert.AreEqual(_deep, _surface.FocusedElement, "and the first item takes the focus");

            _surface.KeyDown(Key.Left, KeyModifiers.None);

            Assert.IsFalse(_submenu.Open);
            Assert.AreEqual(_parent, _surface.FocusedElement);
        }

        [TestMethod]
        public void ClickingTheParentItemDoesNotCloseTheSubmenu()
        {
            _menu.Open = true;
            Layout();

            _parent.OpenSubmenu();
            Layout();

            _surface.PointerDown(_parent.X + 2, _parent.Y + 2, PointerButton.Left);

            Assert.IsTrue(_submenu.Open,
                "the submenu is a CHILD of the item it hangs off, so walking up from the item "
                + "never reaches it - the anchor has to count as inside");
        }

        [TestMethod]
        public void ASubmenuTakesNoSpaceInItsItem()
        {
            _menu.Open = true;
            Layout();

            Assert.AreEqual(0f, _submenu.BoxWidth,
                "a layer is 0x0, so hanging one off an item costs the item nothing");
            Assert.AreEqual(_plain.ActualWidth, _parent.ActualWidth);
        }

        [TestMethod]
        public void OpenAtPutsAMenuWhereThePointerIs()
        {
            _menu.OpenAt(120, 90);
            Layout();

            Assert.IsTrue(_menu.Open);
            Assert.AreEqual(120f, _menu.Panel.X, "which is how a context menu is placed");
            Assert.AreEqual(90f, _menu.Panel.Y);
        }

        [TestMethod]
        public void OpenAtDropsAnyAnchor()
        {
            _menu.AnchorElement = _page;
            _menu.OpenAt(40, 40);
            Layout();

            Assert.IsNull(_menu.AnchorElement);
            Assert.AreEqual(40f, _menu.Panel.X,
                "an anchor would otherwise win and the menu would ignore the point entirely");
        }

        [TestMethod]
        public void AClickInASubmenuDoesNotReopenItThroughItsParent()
        {
            _menu.Open = true;
            Layout();

            _parent.OpenSubmenu();
            Layout();

            _deep.PerformClick();

            Assert.IsFalse(_submenu.Open,
                "PointerClick BUBBLES, so a click on a deep item reaches the parent item and "
                + "used to re-open the branch that had just closed. An item acts on its own "
                + "click only.");
        }
    }
}
