using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class MenuBarTests
    {
        private const int VIEWPORT = 400;

        private VisualElement _root;
        private MenuBar _bar;
        private MenuItem _file;
        private MenuItem _edit;
        private Menu _fileMenu;
        private Menu _editMenu;
        private MenuItem _open;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _bar = new MenuBar { Name = "bar" };
            _bar.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 26 };

            _file = Item("file", "File");
            _edit = Item("edit", "Edit");

            _open = Item("open", "Open");
            _fileMenu = new Menu { Name = "file_menu" };
            _fileMenu.AddChild(_open);
            _file.AddChild(_fileMenu);

            _editMenu = new Menu { Name = "edit_menu" };
            _editMenu.AddChild(Item("copy", "Copy"));
            _edit.AddChild(_editMenu);

            _bar.AddChildren(_file, _edit);
            _root.AddChild(_bar);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };
            Layout();
        }

        private static MenuItem Item(string name, string text)
        {
            var item = new MenuItem { Name = name, Text = text };

            item.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 60 };
            item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 22 };

            return item;
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void HoverDoesNotOpenAnythingWhileTheBarIsCold()
        {
            _surface.PointerMove(_file.X + 2, _file.Y + 2);

            Assert.IsFalse(_fileMenu.Open,
                "a bar you cannot move the mouse across without menus flying open is the thing "
                + "every desktop got right in 1984 - hover only takes over once one is open");
            Assert.IsFalse(_bar.IsActive);
        }

        [TestMethod]
        public void ClickingOpensAndThenHoverSwitches()
        {
            _file.PerformClick();
            Layout();

            Assert.IsTrue(_fileMenu.Open);
            Assert.IsTrue(_bar.IsActive);
            Assert.IsTrue(_bar.HasState(MenuBar.ACTIVE), "so #MenuBar:active needs no C#");

            _surface.PointerMove(_edit.X + 2, _edit.Y + 2);

            Assert.IsTrue(_editMenu.Open, "now hover takes over");
            Assert.IsFalse(_fileMenu.Open, "and only one is open at a time");
        }

        [TestMethod]
        public void ClickingTheOpenItemAgainClosesIt()
        {
            _file.PerformClick();
            Assert.IsTrue(_fileMenu.Open);

            _file.PerformClick();

            Assert.IsFalse(_fileMenu.Open,
                "in a bar the top item is a toggle; inside a panel a parent item is not, which "
                + "is why the rule is asked of the owner rather than baked into the item");
            Assert.IsFalse(_bar.HasState(MenuBar.ACTIVE));
        }

        [TestMethod]
        public void ItOpensBelowItsItemRatherThanBesideIt()
        {
            _file.PerformClick();
            Layout();

            Assert.AreEqual(_file.X, _fileMenu.Panel.X);
            Assert.AreEqual(_file.Y + _file.ActualHeight, _fileMenu.Panel.Y,
                "a bar is horizontal, so its menus drop rather than fly out sideways");
        }

        [TestMethod]
        public void DownOpensAndRightMovesAlong()
        {
            _surface.Focus(_file);

            _surface.KeyDown(Key.Right, KeyModifiers.None);
            Assert.IsFalse(_fileMenu.Open, "Right walks the bar, it does not open anything");
            Assert.AreEqual(_edit, _surface.FocusedElement);

            _surface.KeyDown(Key.Down, KeyModifiers.None);
            Assert.IsTrue(_editMenu.Open,
                "Down is what opens a bar menu, where Right is what opens a nested one - the "
                + "item asks its owner which way it is laid out");
        }

        [TestMethod]
        public void RightSwitchesMenusOnceOneIsOpen()
        {
            _file.PerformClick();
            Layout();

            _surface.KeyDown(Key.Right, KeyModifiers.None);

            Assert.IsTrue(_editMenu.Open);
            Assert.IsFalse(_fileMenu.Open);
        }

        [TestMethod]
        public void EscapeClosesAndKeepsTheFocusOnTheBar()
        {
            _file.PerformClick();
            Layout();

            _surface.Focus(_file);
            _surface.KeyDown(Key.Escape, KeyModifiers.None);

            Assert.IsFalse(_fileMenu.Open);
            Assert.AreEqual(_file, _surface.FocusedElement);
        }

        [TestMethod]
        public void ChoosingACommandClosesTheBar()
        {
            int invoked = 0;
            _bar.ItemInvoked += (sender, e) => invoked++;

            _file.PerformClick();
            Layout();

            _open.PerformClick();

            Assert.IsFalse(_fileMenu.Open);
            Assert.IsFalse(_bar.IsActive);
            Assert.IsFalse(_bar.HasState(MenuBar.ACTIVE),
                "CloseChain stops at the first owner that is not a Menu, so the bar has to be "
                + "told rather than left showing an active look over a closed menu");
            Assert.AreEqual(0, invoked, "a bar item with a submenu is not a command");
        }

        [TestMethod]
        public void ABarItemWithoutASubmenuIsACommand()
        {
            MenuItem help = Item("help", "Help");
            _bar.AddChild(help);

            MenuItem seen = null;
            _bar.ItemInvoked += (sender, e) => seen = e.Item;

            help.PerformClick();

            Assert.AreSame(help, seen);
        }

        [TestMethod]
        public void TheBarIsNotALayerAndTakesItsSpace()
        {
            Assert.AreEqual(LayoutType.Row, _bar.Styles.Layout.Type);
            Assert.AreEqual(26f, _bar.ActualHeight,
                "unlike a Menu, a bar sits in the page - only the menus it opens are layers");
        }
    }
}
