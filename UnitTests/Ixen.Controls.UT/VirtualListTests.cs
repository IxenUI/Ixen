using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class VirtualListTests
    {
        private const int VIEWPORT = 300;
        private const int LIST_HEIGHT = 200;
        private const float ROW = 20;

        private VisualElement _root;
        private VirtualList _list;
        private List<string> _items;
        private IxenSurface _surface;
        private int _created;
        private int _bound;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _list = new VirtualList { Name = "list", ItemHeight = ROW };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = LIST_HEIGHT };

            _root.AddChild(_list);

            _items = new List<string>();

            for (int i = 0; i < 10000; i++)
            {
                _items.Add($"row {i}");
            }

            _created = 0;
            _bound = 0;

            _list.SetItems(_items, Create, Bind);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry() };

            Layout();
        }

        private VisualElement Create()
        {
            _created++;

            return new VisualElement();
        }

        private void Bind(VisualElement row, int index)
        {
            _bound++;

            row.Text = _items[index];
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        [TestMethod]
        public void OnlyTheVisibleWindowIsEverBuilt()
        {
            Assert.IsTrue(_created < 40,
                $"10 000 items and {_created} elements. A @foreach would have built all ten "
                + "thousand, styled and measured and arranged them every pass, for the twenty "
                + "or so anyone can see.");
            Assert.IsTrue(_created >= 10);
        }

        [TestMethod]
        public void TheExtentIsTheWholeListSoTheScrollbarIsHonest()
        {
            Assert.AreEqual(_items.Count * ROW, _list.ScrollExtentHeight,
                "the spacer carries the full height, so the thumb is the right size and the "
                + "list scrolls to the end even though the rows do not exist");
        }

        [TestMethod]
        public void ScrollingRebindsRatherThanRebuilds()
        {
            int createdBefore = _created;

            _list.ScrollY = 100 * ROW;
            Layout();

            Assert.AreEqual(createdBefore, _created, "no element is built to scroll");
            Assert.AreEqual(100 - _list.Overscan, _list.FirstRealised,
                "the window moved to the hundredth item, less the overscan - the row above the "
                + "fold is kept so a scroll of one pixel does not have to bind anything");
        }

        [TestMethod]
        public void TheRowsShowTheRightItemsAfterScrolling()
        {
            _list.ScrollY = 500 * ROW;
            Layout();

            var texts = new List<string>();

            foreach (VisualElement row in _list.RealisedRows)
            {
                texts.Add(row.Text);
            }

            Assert.IsTrue(texts.Contains("row 500"), "the item at the top of the window");
            Assert.IsFalse(texts.Contains("row 0"), "and nothing from where we came from");
        }

        [TestMethod]
        public void ARowIsPlacedAtItsOwnIndex()
        {
            _list.ScrollY = 300 * ROW;
            Layout();

            foreach (VisualElement row in _list.RealisedRows)
            {
                if (row.Text == "row 300")
                {
                    Assert.AreEqual(300 * ROW, row.Styles.Top.Value,
                        "positions are absolute in the list, not relative to the window - which "
                        + "is what lets the existing scroll offset move them with no extra work");

                    return;
                }
            }

            Assert.Fail("row 300 was not realised");
        }

        [TestMethod]
        public void ScrollingToTheEndStillFillsTheWindow()
        {
            _list.ScrollY = _list.MaxScrollY;
            Layout();

            Assert.IsTrue(_list.RealisedCount > 5,
                "the window is clamped against the end of the list, so the last screen is full "
                + "rather than showing one row and a lot of nothing");
        }

        [TestMethod]
        public void AnEmptyListRealisesNothingAndDoesNotThrow()
        {
            _list.SetItems(new List<string>(), Create, Bind);
            Layout();

            Assert.AreEqual(0, _list.RealisedCount);
            Assert.AreEqual(0f, _list.ScrollExtentHeight);
        }

        [TestMethod]
        public void AListShorterThanItsWindowRealisesOnlyWhatItHas()
        {
            _list.SetItems(new List<string> { "a", "b", "c" }, Create, Bind);
            Layout();

            Assert.AreEqual(3, _list.RealisedCount);
        }

        [TestMethod]
        public void AHundredThousandItemsCostNothingToLayOut()
        {
            var many = new List<string>();

            for (int i = 0; i < 100000; i++)
            {
                many.Add($"row {i}");
            }

            _list.SetItems(many, Create, Bind);
            Layout();

            var watch = Stopwatch.StartNew();

            for (int i = 0; i < 50; i++)
            {
                _list.ScrollY = i * ROW * 37;
                _root.InvalidateLayout();
                Layout();
            }

            watch.Stop();

            double perFrame = watch.Elapsed.TotalMilliseconds / 50;

            Assert.IsTrue(perFrame < 5,
                $"{perFrame:F2} ms a frame over a hundred thousand items. This is the whole "
                + "point: the cost follows the WINDOW, not the collection.");
        }


        [TestMethod]
        public void AFrameThatChangedNothingBindsNothing()
        {
            _root.InvalidateLayout();
            Layout();

            int boundBefore = _bound;

            _root.InvalidateLayout();
            Layout();

            Assert.AreEqual(boundBefore, _bound,
                "the window has not moved, so there is nothing to say to the rows. Without the "
                + "short circuit every layout pass rebinds every visible row, and a layout pass "
                + "happens for reasons that have nothing to do with this list.");
        }

        [TestMethod]
        public void GrowingTheCollectionMovesTheExtentOnItsOwn()
        {
            float before = _list.ScrollExtentHeight;

            for (int i = 0; i < 100; i++)
            {
                _items.Add($"extra {i}");
            }

            _root.InvalidateLayout();
            Layout();

            Assert.AreEqual(before + (100 * ROW), _list.ScrollExtentHeight,
                "the spacer is refreshed every pass, so a collection that grew without anyone "
                + "calling Refresh still scrolls to its real end");
        }
    }
}
