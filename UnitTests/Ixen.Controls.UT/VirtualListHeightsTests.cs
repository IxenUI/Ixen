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
    public class VirtualListHeightsTests
    {
        private const int VIEWPORT = 400;
        private const float TRACK = 20;
        private const float HEADER = 40;
        private const float WINDOW = 100;

        private VisualElement _root;
        private VirtualList _list;
        private List<string> _items;
        private float _headerHeight = HEADER;
        private IxenSurface _surface;
        private int _asked;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _items = Enumerable.Range(0, 200).Select(i => i % 10 == 0 ? "album " + i / 10 : "track " + i).ToList();

            _list = new VirtualList { Name = "library", ItemHeight = TRACK, Reorderable = true };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = WINDOW };
            _list.ItemHeights = index =>
            {
                _asked++;
                return _items[index].StartsWith("album") ? _headerHeight : TRACK;
            };
            _list.SetItems(_items, () => new VisualElement(), (row, index) => row.Text = _items[index]);
            _list.Reordered += (sender, e) => e.Apply(_items);

            _root.AddChild(_list);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };

            Layout();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Showing(int index)
            => _list.RealisedRows.FirstOrDefault(row => _list.IndexOfRow(row) == index);

        [TestMethod]
        public void EachItemTakesTheHeightItWasGiven()
        {
            VisualElement header = Showing(0);
            VisualElement track = Showing(1);

            Assert.AreEqual(HEADER, header.ActualHeight, 0.01f);
            Assert.AreEqual(TRACK, track.ActualHeight, 0.01f);
            Assert.AreEqual(_list.ContentY + HEADER, track.Y, 0.01f, "the first track sits below the header");
            Assert.AreEqual(20 * HEADER + 180 * TRACK, _list.ScrollExtentHeight, 0.01f);
        }

        [TestMethod]
        public void OnlyWhatTheWindowCoversIsRealised()
        {
            Assert.AreEqual(6, _list.RealisedCount,
                "the header and three tracks fill the window, one more is at its edge and one is overscan");
            Assert.AreEqual(0, _list.FirstRealised);
        }

        [TestMethod]
        public void ScrollingFindsTheItemsAtTheirRealPlace()
        {
            _list.ScrollY = 1000;
            Layout();

            float top = 0;
            int expected = 0;

            while (top + (_items[expected].StartsWith("album") ? HEADER : TRACK) <= 1000)
            {
                top += _items[expected].StartsWith("album") ? HEADER : TRACK;
                expected++;
            }

            Assert.AreEqual(expected - 1, _list.FirstRealised, "the item under the top edge, less the overscan");
            Assert.AreEqual(_items[expected], Showing(expected).Text);
            Assert.AreEqual(_list.ContentY + top, Showing(expected).Y, 0.01f);
        }

        [TestMethod]
        public void ScrollToAndScrollIntoViewUseTheRealTops()
        {
            _list.ScrollTo(55);
            Layout();

            float top = 6 * HEADER + 49 * TRACK;

            Assert.AreEqual(top, _list.ScrollY, 0.01f, "five albums and the sixth's header come before item 55");

            _list.ScrollTo(0);
            Layout();
            _list.ScrollIntoView(20);
            Layout();

            Assert.AreEqual(2 * HEADER + 18 * TRACK + HEADER - WINDOW, _list.ScrollY, 0.01f,
                "item 20 is a header: its bottom comes into view");
        }

        [TestMethod]
        public void TheHeightsAreAskedOnceAndAgainOnlyWhenTheyMayHaveChanged()
        {
            int asked = _asked;

            for (int step = 1; step <= 10; step++)
            {
                _list.ScrollY = step * 200;
                Layout();
            }

            Assert.AreEqual(asked, _asked, "scrolling reads the offsets it has");

            _headerHeight = 60;
            _list.Refresh();
            Layout();

            Assert.AreEqual(20 * 60 + 180 * TRACK, _list.ScrollExtentHeight, 0.01f, "a Refresh asks again");

            _items.Add("album 99");
            _root.InvalidateLayout();
            Layout();

            Assert.AreEqual(21 * 60 + 180 * TRACK, _list.ScrollExtentHeight, 0.01f, "and so does a new count");
        }

        [TestMethod]
        public void ADropLandsOnTheSideOfTheItemThePointerIsOn()
        {
            _list.ScrollY = 0;
            Layout();

            VisualElement track = Showing(1);
            float x = track.X + 10;
            float y = track.Y + 5;

            _surface.PointerDown(x, y, PointerButton.Left);
            _surface.PointerMove(x, y + 6);
            Layout();

            float below = _list.ContentY + HEADER + TRACK + 16;

            _surface.PointerMove(x, below);
            Layout();

            Assert.AreEqual(3, _list.DropIndex, "the lower half of track 2 is the gap below it");

            float lower = _list.ContentY + HEADER + 2 * TRACK + 2;

            _surface.PointerMove(x, lower);
            Layout();

            Assert.AreEqual(3, _list.DropIndex, "the upper half of track 3 is the gap above it");
            Assert.AreEqual(_list.ContentY + HEADER + 2 * TRACK - 1, _list.DropMarker.Y, 0.5f);

            _surface.PointerUp(x, lower, PointerButton.Left);
            Layout();

            Assert.AreEqual("album 0,track 2,track 1,track 3,track 4", string.Join(",", _items.Take(5)));
        }

        [TestMethod]
        public void AGridIgnoresTheHeights()
        {
            _list.ItemWidth = 50;
            Layout();

            Assert.AreEqual(3, _list.Columns);
            Assert.AreEqual(TRACK, Showing(0).ActualHeight, 0.01f, "tiles share one height");
        }
    }
}
