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
    public class VirtualListGridTests
    {
        private const int VIEWPORT = 800;
        private const float ITEM = 50;

        private VisualElement _root;
        private VisualElement _holder;
        private VirtualList _list;
        private List<string> _albums;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _holder = new VisualElement { Name = "holder" };
            _holder.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 214 };
            _holder.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };

            _albums = Enumerable.Range(0, 100).Select(i => "album " + i).ToList();

            _list = new VirtualList { Name = "albums", ItemHeight = ITEM, ItemWidth = 60, Reorderable = true };
            _list.SetItems(_albums, () => new VisualElement(), (cell, index) => cell.Text = _albums[index]);
            _list.Reordered += (sender, e) => e.Apply(_albums);

            _holder.AddChild(_list);
            _root.AddChild(_holder);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Showing(int index)
            => _list.RealisedRows.FirstOrDefault(cell => _list.IndexOfRow(cell) == index);

        [TestMethod]
        public void ItemsFlowIntoAsManyColumnsAsTheWidthHolds()
        {
            float width = _list.ContentWidth;

            Assert.AreEqual(200, width, 0.01f, "214 less the scrollbar's gutter");
            Assert.AreEqual(3, _list.Columns, "200 holds three 60s");

            VisualElement fourth = Showing(4);

            Assert.AreEqual(_list.ContentX + width / 3, fourth.X, 0.5f, "item 4 is the middle of the second row");
            Assert.AreEqual(_list.ContentY + ITEM, fourth.Y, 0.5f);
            Assert.AreEqual(width / 3, fourth.ActualWidth, 0.5f, "and the columns share the width");
            Assert.AreEqual(34 * ITEM, _list.ScrollExtentHeight, 0.01f, "a hundred items make 34 rows");
        }

        [TestMethod]
        public void OnlyTheRowsInViewExist()
        {
            Assert.AreEqual(4 * 3 + 2 * 3, _list.RealisedCount,
                "four rows in view and one more on each side, three items each - eighteen of a hundred");
        }

        [TestMethod]
        public void AResizeKeepsTheItemAtTheTopInView()
        {
            _list.ScrollY = 10 * ITEM + 20;
            Layout();

            Assert.AreEqual(30, _list.FirstRealised + 3, "row 10 of three columns starts with item 30, one row of overscan above");

            _holder.Styles.Width.Value = 434;
            _holder.Invalidate();
            Layout();

            Assert.AreEqual(7, _list.Columns);
            Assert.AreEqual(4 * ITEM + 20, _list.ScrollY, 0.01f,
                "item 30 is on row 4 of seven columns, and the 20 units it was scrolled into its row stay: "
                + "the offset alone kept the place in pixels and showed items 70 and on");
        }

        [TestMethod]
        public void AGridThatComesBackStaysWhereItWasLeft()
        {
            _list.PersistScroll = true;
            _list.ScrollY = 10 * ITEM;
            Layout();

            _holder.RemoveChild(_list);
            Layout();

            var again = new VirtualList { Name = "albums", ItemHeight = ITEM, ItemWidth = 60, PersistScroll = true };
            again.SetItems(_albums, () => new VisualElement(), (cell, index) => cell.Text = _albums[index]);

            _holder.AddChild(again);
            Layout();

            Assert.AreEqual(10 * ITEM, again.ScrollY, 0.01f,
                "its first pass has no width, so one column; going to three is not a reflow anyone saw, "
                + "and keeping the top item across it would have undone the recall");
        }

        [TestMethod]
        public void AResizeReflowsWithinTheFrameThatMadeIt()
        {
            _holder.Styles.Width.Value = 434;
            _holder.Invalidate();
            Layout();

            Assert.AreEqual(7, _list.Columns);

            VisualElement eighth = Showing(8);

            Assert.AreEqual(_list.ContentX + 420f / 7, eighth.X, 0.5f, "item 8 moved to the second row's second place");
            Assert.AreEqual(_list.ContentY + ITEM, eighth.Y, 0.5f);
            Assert.AreEqual(15 * ITEM, _list.ScrollExtentHeight, 0.01f);
        }

        [TestMethod]
        public void ScrollingAndRevealingCountInRows()
        {
            _list.ScrollIntoView(50);
            Layout();

            Assert.AreEqual(16 * ITEM + ITEM - _list.ContentHeight, _list.ScrollY, 0.01f,
                "item 50 is on row 16");
            Assert.IsNotNull(Showing(50));

            _list.ScrollTo(30);
            Layout();

            Assert.AreEqual(10 * ITEM, _list.ScrollY, 0.01f);
            Assert.AreEqual("album 30", Showing(30).Text);
        }

        [TestMethod]
        public void BackToAWidthOfZeroIsASingleColumnAgain()
        {
            _list.ItemWidth = 0;
            Layout();

            Assert.AreEqual(1, _list.Columns);
            Assert.AreEqual(_list.ContentX, Showing(1).X, 0.5f);
            Assert.AreEqual(_list.ContentY + ITEM, Showing(1).Y, 0.5f);
            Assert.AreEqual(_list.ContentWidth, Showing(1).ActualWidth, 0.5f);
        }

        [TestMethod]
        public void AReorderLandsBetweenTheTwoItemsBesideThePointer()
        {
            VisualElement first = Showing(0);
            float x = first.X + 10;
            float y = first.Y + 10;
            float columnWidth = _list.ContentWidth / 3;
            float toX = _list.ContentX + columnWidth + columnWidth * 0.8f;
            float toY = _list.ContentY + ITEM + 10;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x + 6, y);
            Layout();
            _surface.PointerMove(toX, toY);
            Layout();

            Assert.AreEqual(5, _list.DropIndex, "past the middle of item 4, the gap is the one after it");

            VisualElement marker = _list.DropMarker;

            Assert.AreEqual(ITEM, marker.ActualHeight, 0.5f, "a grid's marker stands upright, a row tall");
            Assert.AreEqual(_list.ContentX + 2 * columnWidth - 1, marker.X, 0.5f);
            Assert.AreEqual(_list.ContentY + ITEM, marker.Y, 0.5f);

            _surface.PointerUp(toX, toY, PointerButton.Left);
            Layout();

            Assert.AreEqual("album 1,album 2,album 3,album 4,album 0,album 5", string.Join(",", _albums.Take(6)));
        }
    }
}
