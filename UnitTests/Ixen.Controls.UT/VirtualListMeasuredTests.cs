using Ixen.Core;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Controls.UT
{
    [TestClass]
    public class VirtualListMeasuredTests
    {
        private const int VIEWPORT = 400;
        private const float ESTIMATE = 40;
        private const float WINDOW = 200;

        private VisualElement _root;
        private VirtualList _list;
        private List<float> _heights;
        private IxenSurface _surface;
        private bool _quarter;

        private static float Real(int index) => index % 5 == 0 ? 60 : 20;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _heights = Enumerable.Range(0, 500).Select(Real).ToList();

            _list = new VirtualList { Name = "lyrics", ItemHeight = ESTIMATE, MeasureItems = true };
            _list.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 200 };
            _list.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = WINDOW };
            _list.SetItems(_heights, Row, Bind);

            _root.AddChild(_list);

            _surface = new IxenSurface(_root) { Styles = new StyleRegistry(), Scheduler = new FakeScheduler() };

            Layout();
        }

        private static VisualElement Row()
        {
            var row = new VisualElement();
            row.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var body = new VisualElement();
            body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };

            row.AddChild(body);

            return row;
        }

        private void Bind(VisualElement row, int index)
        {
            VisualElement body = row.ChildElements[0];

            if (index == 0 && _quarter)
            {
                body.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Percents, Value = 100 };
                body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Content };
                body.Styles.AspectRatio = new AspectRatioStyleDescriptor { Ratio = 4 };
            }
            else
            {
                body.Styles.Width = new WidthStyleDescriptor();
                body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = _heights[index] };
                body.Styles.AspectRatio = new AspectRatioStyleDescriptor();
            }

            body.Invalidate();
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private VisualElement Showing(int index)
            => _list.RealisedRows.FirstOrDefault(row => _list.IndexOfRow(row) == index);

        private float ScreenTop(VisualElement row) => row.Y - _list.ContentY - _list.ScrollY;

        [TestMethod]
        public void EachRowTakesTheHeightOfItsContentAndTheNextStartsBelowIt()
        {
            Assert.AreEqual(60, Showing(0).ActualHeight, 0.01f);
            Assert.AreEqual(20, Showing(1).ActualHeight, 0.01f);
            Assert.AreEqual(60, ScreenTop(Showing(1)), 0.01f, "the second row starts where the first one ends");
            Assert.AreEqual(80, ScreenTop(Showing(2)), 0.01f, "not at the 80 the estimate gave the second");
            Assert.AreEqual(60 + 20 * 4, ScreenTop(Showing(5)), 0.01f);
        }

        [TestMethod]
        public void TheWindowIsFilledByWhatTheRowsReallyMeasureInOneLayout()
        {
            VisualElement last = _list.RealisedRows.Last();

            Assert.IsTrue(ScreenTop(last) + last.ActualHeight >= WINDOW,
                "measured rows are shorter than the estimate, so the first guess left the bottom empty");
            Assert.IsTrue(_list.RealisedCount > (int)(WINDOW / ESTIMATE) + 2,
                "more rows than the estimate would have realised");
        }

        [TestMethod]
        public void TheExtentCountsWhatWasMeasuredAndEstimatesTheRest()
        {
            int measured = _list.FirstRealised + _list.RealisedCount;
            float known = Enumerable.Range(0, measured).Sum(Real);

            Assert.AreEqual(known + (_heights.Count - measured) * ESTIMATE, _list.ScrollExtentHeight, 0.01f);
        }

        [TestMethod]
        public void RowsMeasuredAboveTheTopDoNotMoveWhatIsOnScreen()
        {
            _list.ScrollY = 3000;
            Layout();

            VisualElement first = _list.RealisedRows.First(row => ScreenTop(row) >= 0);
            int index = _list.IndexOfRow(first);
            float before = ScreenTop(first);

            _list.Overscan = 6;
            _list.Refresh();
            Layout();

            Assert.AreEqual(before, ScreenTop(Showing(index)), 0.01f,
                "six rows above it were estimated at 40 and measured at 20 or 60: the scroll offset "
                + "absorbs the difference, or the page jumps under the reader");
        }

        [TestMethod]
        public void ARowWhoseContentGrowsPushesTheNextOnesDown()
        {
            _heights[1] = 50;

            VisualElement body = Showing(1).ChildElements[0];
            body.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 50 };
            body.Invalidate();

            Layout();

            Assert.AreEqual(110, ScreenTop(Showing(2)), 0.01f, "60 + 50: nothing told the list but the measure");
        }

        [TestMethod]
        public void ScrollToFindsAMeasuredRowAtItsRealTop()
        {
            _list.ScrollTo(3);
            Layout();

            Assert.AreEqual(100, _list.ScrollY, 0.01f, "60 + 20 + 20");
            Assert.AreEqual(0, ScreenTop(Showing(3)), 0.01f);
        }

        [TestMethod]
        public void AnotherWidthMeasuresAgain()
        {
            _quarter = true;
            _list.Refresh();

            Layout();

            float wide = _list.ContentWidth;

            Assert.AreEqual(wide / 4, ScreenTop(Showing(1)), 0.01f, "the first row is a quarter as tall as it is wide");

            _list.Styles.Width.Value = 120;
            _list.InvalidateLayout();

            Layout();

            Assert.IsTrue(_list.ContentWidth < wide);
            Assert.AreEqual(_list.ContentWidth / 4, ScreenTop(Showing(1)), 0.01f,
                "and re-measured once the list is narrower");
        }
    }
}
