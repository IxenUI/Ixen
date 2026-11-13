using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class VirtualListFocusTests
    {
        private const int VIEWPORT = 800;
        private const float ITEM = 50;

        private VisualElement _root;
        private VirtualList _list;
        private Button _after;
        private List<string> _albums;
        private IxenSurface _surface;
        private int _opened = -1;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var holder = new VisualElement { Name = "holder" };
            holder.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 214 };
            holder.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _albums = Enumerable.Range(0, 100).Select(i => "album " + i).ToList();

            _list = new VirtualList { Name = "albums", ItemHeight = ITEM, ItemWidth = 60 };
            _list.SetItems(_albums, Tile, (cell, index) => cell.Text = _albums[index]);

            _after = new Button { Name = "after", Text = "After" };
            _after.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 30 };

            holder.AddChild(_list);
            _root.AddChildren(holder, _after);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };

            Layout();
        }

        private VisualElement Tile()
        {
            var tile = new VisualElement();
            tile.PointerClick += (sender, e) => _opened = _list.IndexOfRow(tile);
            return tile;
        }

        private void Layout()
        {
            for (int pass = 0; pass < 3; pass++)
            {
                _surface.ComputeLayout(VIEWPORT, VIEWPORT);
            }
        }

        private void Press(Key key, KeyModifiers modifiers = KeyModifiers.None)
        {
            _surface.KeyDown(key, modifiers);
            Layout();
        }

        private int Focused()
        {
            VisualElement focused = _surface.FocusedElement;

            return focused != null && focused.Parent == _list ? _list.IndexOfRow(focused) : -1;
        }

        [TestMethod]
        public void TheGridIsOneTabStop()
        {
            Press(Key.Tab);

            Assert.AreEqual(0, Focused(), "a clickable tile is a button, so Tab reaches the first one");

            Press(Key.Tab);

            Assert.AreSame(_after, _surface.FocusedElement,
                "and the next Tab leaves the grid: a hundred albums are not a hundred Tab presses");
        }

        [TestMethod]
        public void TheArrowsMoveByAnItemAndByARow()
        {
            Assert.AreEqual(3, _list.Columns);

            Press(Key.Tab);
            Press(Key.Right);

            Assert.AreEqual(1, Focused());

            Press(Key.Down);

            Assert.AreEqual(4, Focused(), "one row of three down");

            Press(Key.Left);
            Press(Key.Up);

            Assert.AreEqual(0, Focused());
        }

        [TestMethod]
        public void TheFocusFollowsTheItemNotTheRecycledElement()
        {
            Press(Key.Tab);

            for (int i = 0; i < 12; i++)
            {
                Press(Key.Down);
            }

            Assert.AreEqual(36, Focused(),
                "twelve rows down, far past the first window: the rows were recycled on the way, and "
                + "the element that held the focus came to show another album - the focus has to go with "
                + "the album, or Tab and the arrows wander");
            Assert.IsTrue(_list.ScrollY > 0, "and the list scrolled to show it");
        }

        [TestMethod]
        public void EndAndHomeReachBothEnds()
        {
            Press(Key.Tab);
            Press(Key.End);

            Assert.AreEqual(99, Focused(), "the last album, built only once the list scrolled to it");

            Press(Key.Home);

            Assert.AreEqual(0, Focused());
            Assert.AreEqual(0, _list.ScrollY, 0.01f);
        }

        [TestMethod]
        public void EnterOpensTheFocusedItem()
        {
            Press(Key.Tab);
            Press(Key.Down);
            Press(Key.Enter);

            Assert.AreEqual(3, _opened);
        }

        [TestMethod]
        public void TabComesBackToTheItemLeftLast()
        {
            Press(Key.Tab);
            Press(Key.Right);
            Press(Key.Right);
            Press(Key.Tab);
            Press(Key.Tab, KeyModifiers.Shift);

            Assert.AreEqual(2, Focused(), "the Tab stop moved with the arrows");
        }

        [TestMethod]
        public void ScrollingTheFocusedItemAwayDropsTheFocusRatherThanMovingIt()
        {
            Press(Key.Tab);
            Press(Key.Right);

            _list.ScrollY = 30 * ITEM;
            Layout();

            Assert.AreEqual(-1, Focused(),
                "the wheel took album 1 out of the window; its element now shows another album, which "
                + "the user never chose");
        }
    }
}
